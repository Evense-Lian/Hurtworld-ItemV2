using Oxide.Core;
using Random = System.Random;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("GunGame", "Kaidoz", "1.4.0")]
    [Description("Арена dm с переходами на новое оружее")]

    class GunGame : HurtworldPlugin
    {
        #region Data

        private Random random = new Random();
        HashSet<GGPlayer> gplayers = new HashSet<GGPlayer>();
        List<ulong> deathsForSpawn = new List<ulong>();
        Dictionary<ulong, Configuration.Place> editSessions = new Dictionary<ulong, Configuration.Place>();

        #endregion

        #region Class

        public class InventItem
        {
            public int slot { get; set; }
            public ItemObject Item { get; set; }
        }

        public class GGPlayer
        {
            public PlayerSession session { get; set; }
            public ulong steamid { get; set; }
            public Vector3 homeposition { get; set; }
            public float infamy { get; set; }
            public int kills { get; set; }
            public List<InventItem> inventory { get; set; }

            public GGPlayer(PlayerSession session, bool saveinv)
            {
                this.session = session;
                this.steamid = (ulong)session.SteamId;
                this.homeposition = session.WorldPlayerEntity.transform.position;
                this.kills = 0;
                if (saveinv)
                    this.inventory = getInv(session);
                else
                    this.inventory = null;
            }

            List<InventItem> getInv(PlayerSession session)
            {
                var inv = session.WorldPlayerEntity.GetComponent<PlayerInventory>();
                List<InventItem> invents = new List<InventItem>();
                for (int d = 0; d < 50; d++)
                {
                    if (inv.GetSlot(d) != null)
                    {
                        invents.Add(new InventItem()
                        {
                            slot = d,
                            Item = inv.GetSlot(d)
                        });
                    }
                }
                return invents;
            }

            public int getStep(int count)
            {
                return this.kills / count;
            }
        }

        #endregion

        Timer ta = null;
        bool arenastart = false;
        Configuration _config;

        #region Configration

        public class Configuration
        {
            [JsonProperty("Settings Start")]
            public StartG startg { get; set; }

            public class StartG
            {
                [JsonProperty("Minimum online")]
                public int minimalonline { get; set; }

                [JsonProperty("Start GG at list time(false - every N min.)")]
                public bool timesti { get; set; }

                [JsonProperty("The time until the message about the future run(min.)")]
                public int beforetime { get; set; }

                [JsonProperty("Start GG every")]
                public int timeevery { get; set; }

                [JsonProperty("Time(HH:MM)")]
                public List<string> times { get; set; }

                [JsonProperty("Warring About start")]
                public bool aboutstart { get; set; }
            }

            [JsonProperty("Setting Prize")]
            public PrizeC prizeC { get; set; }

            public class PrizeC
            {
                [JsonProperty("Random prize")]
                public bool randprize { get; set; }

                [JsonProperty("Prizes")]
                public List<Prize> prizes { get; set; }

                public class Prize
                {
                    [JsonProperty("Item NameOrId")]
                    public string itemid { get; set; }

                    [JsonProperty("Item Count")]
                    public int itemcount { get; set; }
                }
            }


            [JsonProperty("Game")]
            public Game game { get; set; }

            public class Game
            {
                [JsonProperty("Weapon")]
                public List<Weapon> weapons { get; set; }

                public class Weapon
                {
                    [JsonProperty("ItemNameOrId")]
                    public string itemid { get; set; }

                    [JsonProperty("Item Name in chat")]
                    public string itemname { get; set; }

                    [JsonProperty("Ammo")]
                    public string ammo { get; set; }
                }

                [JsonProperty("Count kills")]
                public int kol { get; set; }

                [JsonProperty("Message about last step")]
                public bool warlast { get; set; }
            }

            [JsonProperty("Setting Game")]
            public SettingsGame settingsGame { get; set; }

            public class SettingsGame
            {
                [JsonProperty("Inventory save")]
                public bool inventorysave { get; set; }

                [JsonProperty("Notice kill")]
                public bool noticekill { get; set; }

                [JsonProperty("GG stop after(sec.)")]
                public int timerstop { get; set; }

                [JsonProperty("Get out from GG dead")]
                public bool alloweddeath { get; set; }

                [JsonProperty("No Effect")]
                public bool noEffect { get; set; }
            }

            [JsonProperty("Arenas")]
            public Dictionary<string, Place> places;

            public class Place
            {
                [JsonProperty("Spawn positions")]
                public List<Vector3> spawnpositions;

                [JsonProperty("MaxPlayer")]
                public int MaxPlayer;
            }
        }

        #endregion

        #region Config

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Create new config for GunGame...");
            _config = new Configuration()
            {
                places = new Dictionary<string, Configuration.Place>()
                {
                    {
                        "default",
                        new Configuration.Place()
                        {
                            spawnpositions = new List<Vector3>()
                            {
                                new Vector3(0,0,0)
                            },
                            MaxPlayer = 10
                        }
                    }
                },
                startg = new Configuration.StartG()
                {
                    minimalonline = 10,
                    aboutstart = true,
                    timesti = true,
                    timeevery = 180,
                    beforetime = 5,
                    times = new List<string>()
                    {
                        "12:00",
                    }
                },
                prizeC = new Configuration.PrizeC()
                {
                    randprize = true,
                    prizes = new List<Configuration.PrizeC.Prize>()
                    {
                        new Configuration.PrizeC.Prize()
                        {
                            itemid = "Amber",
                            itemcount = 50,
                        },
                        new Configuration.PrizeC.Prize()
                        {
                            itemid = "VehicleWrench",
                            itemcount = 1,
                        }
                    }
                },
                game = new Configuration.Game()
                {
                    weapons = new List<Configuration.Game.Weapon>()
                    {
                        new Configuration.Game.Weapon()
                        {
                            itemid = "Beretta M9",
                            itemname = "Beretta",
                            ammo = "9mm Bullet"
                        },
                        new Configuration.Game.Weapon()
                        {
                            itemid = "AWM",
                            itemname = "Bolt",
                            ammo = "308 Bullet"
                        },
                        new Configuration.Game.Weapon()
                        {
                            itemid = "AR15",
                            itemname = "AR15",
                            ammo = "556 Bullet"
                        },
                        new Configuration.Game.Weapon()
                        {
                            itemid = "Shotgun",
                            itemname = "Shotgun",
                            ammo = "Shotgun Shell"
                        },
                        new Configuration.Game.Weapon()
                        {
                            itemid = "Bow T1",
                            itemname = "Bow T1",
                            ammo = "Arrow"
                        },
                        new Configuration.Game.Weapon()
                        {
                            itemid = "Titranium Axe",
                            itemname = "Red Axe",
                            ammo = ""
                        }
                    },
                    kol = 3,
                    warlast = true
                },
                settingsGame = new Configuration.SettingsGame()
                {
                    inventorysave = false,
                    noticekill = true,
                    timerstop = 3600,
                    alloweddeath = true,
                    noEffect = true
                }
            };
            SaveConfig();
        }

        #endregion

        private new void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) throw new Exception();
            }
            catch
            {
                Config.WriteObject(_config, false, $"{Interface.Oxide.ConfigDirectory}/{Name}.jsonError");
                PrintError("The configuration file contains an error and has been replaced with a default config.\n" +
                           "The error configuration file was saved in the .jsonError extension");
                LoadDefaultConfig();
            }
            SaveConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        ////////////////////////////
        ///// Загрузка/Loading /////
        ////////////////////////////	

        void Loaded()
        {
            LoadPermissions();
            LoadDefaultMessages();
            LoadConfig();
            TimeStart();
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"GGSTART", "<color=red>[GunGame]</color>\n Arena launched!\n in the {0} stage(s) arena!\n to log in, type<i>/gg reg</i>"},
                {"GGBefore", "<color=red>[GunGame]</color> Arena will be launched in {0} minutes"},
                {"GGSTOPWP", "<color=red>[GunGame]</color> Arena completed. Winner<color=lime><b>{0}</b></color>"},
                {"GGSTOP", "<color=red>[GunGame]</color> Arena completed."},
                {"GGMaxPlayer", "<color=red>[GunGame]</color> Aarena is full."},
                {"GGDeath", "<color=red>[GunGame]</color> You're dead"},
                {"GGREGSendMSG","<color=red>[GunGame]</color> You went to the event"},
                {"GGHelp", "<color=red>[GunGame]</color> CMDS - /gg reg | Logout - /gg unreg"},
                {"GGREGBroadcast", "<size=14><color=red>[GunGame]</color> {0} joined the arena. Members {count}</size>"},
                {"GGUNREGSendMSG","<color=red>[GunGame]</color> You left off the event"},
                {"GGUNREGBroadcast", "<size=14><color=red> [GunGame]</color> {0} Exited the arena</size>"},
                {"GGDisable", "<color=red>[GunGame]</color> Arena not running"},
                {"GGOut", "<color=red>[GunGame]</color> You are not in the arena"},
                {"GGin", "<color=red>[GunGame]</color> You're already in the arena"},
                {"GGTryReg", "<color=red>[GunGame]</color> Enter the arena, you need to be naked!"},
                {"GG_A_NextWeapon", "<size=14><color=red> [GunGame]</color> {0} switched to weapons:<color=lime>{1}</color></size>"},
                {"GG_A_LastWeapon", "<size=15><color=red>[GunGame]</color> Player {0} has moved to the last stage!</size>"},
                {"GG_Notice_Death","You're dead"},
                {"GG_Notice_DisableCommands","Cmd disabled"},
                {"GG_Notice_Kills", "{0} of {1}"}
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"GGSTART","<color=red>[GunGame]</color>\n    Арена запущена!\n   На арене {0} этапа(ов)!\n  Чтобы зайти введите<i>/gg reg</i>"},
                {"GGBefore","<color=red>[GunGame]</color> Арена будет запущена через {0} минут"},
                {"GGSTOPWP","<color=red>[GunGame]</color> Арена завершена. Победитель<color=lime><b>{0}</b></color>"},
                {"GGSTOP","<color=red>[GunGame]</color> Арена завершена."},
                {"GGMaxPlayer","<color=red>[GunGame]</color> Арена заполнена."},
                {"GGDeath","<color=red>[GunGame]</color> Вы мертвы"},
                {"GGREGSendMSG","<color=red>[GunGame]</color> Вы зашли на ивент"},
                {"GGHelp","<color=red>[GunGame]</color> Зайти - /gg reg | Выйти - /gg unreg"},
                {"GGREGBroadcast","<size=14><color=red>[GunGame]</color> {0} присоеднился к арене. Участников {count}</size>"},
                {"GGUNREGSendMSG","<color=red>[GunGame]</color> Вы вышли с ивента"},
                {"GGUNREGBroadcast","<size=14><color=red>[GunGame]</color> {0} вышел с арены</size>"},
                {"GGDisable","<color=red>[GunGame]</color> Арена не запущена"},
                {"GGOut","<color=red>[GunGame]</color> Вы не находитесь на арене"},
                {"GGIn","<color=red>[GunGame]</color> Вы уже на арене"},
                {"GGTryReg","<color=red>[GunGame]</color> Чтобы попасть на арену,нужно быть голым!"},
                {"GG_A_NextWeapon","<size=14><color=red>[GunGame]</color> {0} перешел на оружие:<color=lime>{1}</color></size>"},
                {"GG_A_LastWeapon","<size=15><color=red>[GunGame]</color> Игрок {0} перешел на последний этап!</size>"},
                {"GG_Notice_Death"," Вы мертвы"},
                {"GG_Notice_DisableCommands"," Команда отключена"},
                {"GG_Notice_Kills"," {0} из {1}"}
            }, this, "ru");

            // Thanks for the translation PL ★☯Kub4☯★
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"GGSTART","<color=red>[Arena]</color>\n Arena Wystartowała!\n Etapy Areny {0} !\n Aby dołączyć do areny wpisz <color=red>/gg reg</color>"},
                {"GGBefore","<color=red>[Arena]</color> Arena zostanie uruchomiona za <color=red>{0}</color> minut"},
                {"GGSTOPWP","<color=red>[Arena]</color> Zwycięscą areny został: <color=red>{0}</color>"},
                {"GGSTOP","<color=red>[Arena]</color> Arena wyłączona."},
                {"GGMaxPlayer","<color=red>[Arena]</color> Arena jest pełna."},
                {"GGDeath","<color=red>[Arena]</color> Nie żyjesz"},
                {"GGREGSendMSG","<color=red>[Arena]</color> Dołączyłeś do areny"},
                {"GGHelp","<color=red>[Arena]</color> Zapisz się - <color=red>/gg reg</color>\n Wypisz się - <color=red>/gg unreg</color>"},
                {"GGREGBroadcast","<color=red>[Arena]</color> Gracz <color=red>{0}</color> Dołączył do areny. Członkowie areny : <color=red>{count}</color>"},
                {"GGUNREGSendMSG","<color=red>[Arena]</color> Wypisałęś się z areny"},
                {"GGUNREGBroadcast","<color=red>[Arena]</color> <color=red>{0}</color> Wypisał się z areny"},
                {"GGDisable","<color=red>[Arena]</color>  Arena jest Wyłączona"},
                {"GGOut","<color=red>[Arena]</color> Nie jesteś na arenie"},
                {"GGIn","<color=red>[Arena]</color> Jesteś już na arenie"},
                {"GGTryReg","<color=red>[Arena]</color> Aby dołączyć do areny nie możesz mieć na sobie itemków!"},
                {"GG_A_NextWeapon","<color=red>[Arena]</color> Gracz <color=red>{0}</color> przeszedł do następnego etapu areny <color=red>{1}</color> "},
                {"GG_A_LastWeapon","<color=red>[Arena]</color> Gracz <color=red>{0}</color> przeszedł do ostatniego etapu areny!"},
                {"GG_Notice_Death"," Nie żyjesz"},
                {"GG_Notice_Error"," Błąd: spróbuj ponownie"},
                {"GG_Notice_DisableCommands"," Komenda jest wyłączona"},
                {"GG_Notice_Kills"," {0} z {1}"}
            }, this, "pl");
        }

        void LoadPermissions()
        {
            if (!permission.PermissionExists("gungame.admin"))
                permission.RegisterPermission("gungame.admin", this);
        }

        // Проверка времени для запуска арены по расписанию
        void TimeStart()
        {
            timer.Repeat(_config.startg.timesti ? 60f : _config.startg.timeevery, 0, () =>
            {
                if (GameManager.Instance._activePlayerCount >= _config.startg.minimalonline)
                {
                    if (_config.startg.timesti)
                    {
                        try
                        {
                            foreach (string tim in _config.startg.times)
                            {
                                if (tim == DateTime.Now.ToString("HH:mm"))
                                {
                                    STARTGG();
                                }
                                if (_config.startg.aboutstart)
                                {
                                    DateTime t = DateTime.ParseExact(tim, "HH:mm", null).AddMinutes(-_config.startg.beforetime);

                                    if (t.ToString("HH:mm") == DateTime.Now.ToString("HH:mm"))
                                    {
                                        Server.Broadcast(Msg("GGBefore").Replace("{0}", Convert.ToString(_config.startg.beforetime)));
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                    else
                    {
                        STARTGG();
                    }
                }
            });
        }

        Configuration.Place place = null;
        void STARTGG()
        {
            place = _config.places.ElementAt(random.Next(0, _config.places.Count - 1)).Value;
            arenastart = true;
            Server.Broadcast(Msg("GGSTART").Replace("{0}", Convert.ToString(_config.game.weapons.Count())));

            ta = timer.Once(_config.settingsGame.timerstop, () =>
            {
                StopGGW();
            });
        }

        // Завершение арены с победителем
        void StopGGW(PlayerSession session = null)
        {
            try
            {
                if (!ta.Destroyed)
                    ta.Destroy();

                arenastart = false;
                if (session != null)
                    Server.Broadcast(Msg("GGSTOPWP").Replace("{0}", session.Identity.Name));
                else
                    Server.Broadcast(Msg("GGSTOP"));
                foreach (GGPlayer gp in gplayers)
                {
                    try
                    {
                        if (gp.session != null)
                            exitp(gp.session);
                    }
                    catch (Exception ex)
                    {
                        Puts("Error: ExitP, ex: " + ex);
                    }
                }
                if(session!=null)
                    GivePrice(session);
                gplayers.Clear();
            }
            catch (Exception ex)
            {
                Puts("Error: StopGGWPL, ex: " + ex);
            }
        }

        List<Timer> tmrs = new List<Timer>();

        // Выход игрока с арены
        void exitp(PlayerSession session)
        {
            switch (_config.settingsGame.alloweddeath)
            {
                case true:
                    if (session.WorldPlayerEntity == null)
                    {
                        deathsForSpawn.Add((ulong)session.SteamId);
                        return;
                    }
                    break;
                case false:
                    if (session.WorldPlayerEntity == null)
                    {
                        Send(session, Msg("GGDeath", session.SteamId.ToString()));
                        return;
                    }
                    break;
            }
            session.WorldPlayerEntity.gameObject.GetComponent<NoEffect>().TryDestroy();

            InvClear(session);
            GGPlayer par = FindGP(session).FirstOrDefault();
            Teleport(session, par.homeposition);
            stats(session, 10f);
            if (_config.settingsGame.inventorysave)
                refaundinventory(session);
        }

        // Завершение арены если плагин выключили
        void Unload()
        {
            if (arenastart)
                StopGGW();
        }

        /////////////////////////
        ///// Основа/Basic /////
        ///////////////////////			

        // Чтобы запретить использование других команды =>

        bool isplayergg(PlayerSession session)
        {
            int prov = FindGP(session).Count();

            if (prov == 1)
            {
                notice(session, Msg("GG_Notice_DisableCommands", session.SteamId.ToString()));
                return true;
            }
            else
                return false;
        }

        object OnPlayerSuicide(PlayerSession session)
        {
            if (isplayergg(session))
            {
                return true;
            }
            return null;
        }

        object canExtTeleport(PlayerSession session)
        {
            if (isplayergg(session))
            {
                return true;
            }
            return null;
        }

        object canRedeemKit(PlayerSession session)
        {
            if (isplayergg(session))
            {
                return true;
            }
            return null;
        }
        //<=

        [ChatCommand("gg")]
        void JoinEvent(PlayerSession session, string command, string[] args)
        {
            if (!arenastart)
            {
                Send(session, Msg("GGDisable", session.SteamId.ToString()));
                return;
            }
            if (args.Length == 0)
            {
                Send(session, Msg("GGHelp", session.SteamId.ToString()));
                return;
            }
            if (place.MaxPlayer != 0 && gplayers.Count >= place.MaxPlayer)
            {
                Send(session, Msg("GGMaxPlayer", session.SteamId.ToString()));
                return;
            }

            var prov = FindGP(session).Count();

            if (args[0].ToLower() == "reg")
            {
                if (prov == 0)
                {
                    if (!trygg(session, "reg"))
                        return;
                    gplayers.Add(new GGPlayer(session, _config.settingsGame.inventorysave));
                    if (_config.settingsGame.noEffect)
                        session.WorldPlayerEntity.gameObject.AddComponent<NoEffect>();
                    InvClear(session);
                    stats(session, 2f);
                    Heal(session);
                    Vector3 pos = place.spawnpositions[random.Next(0, place.spawnpositions.Count - 1)];
                    Teleport(session, pos);
                    givew(session, 0);
                    Send(session, Msg("GGREGSendMSG", session.SteamId.ToString()));
                    Server.Broadcast(Msg("GGREGBroadcast").Replace("{0}", session.Identity.Name).Replace("{count}", gplayers.Count.ToString()));
                    return;
                }
                else
                {
                    Send(session, Msg("GGIn", session.SteamId.ToString()));
                    return;
                }
            }
            if (args[0].ToLower() == "unreg")
            {
                if (prov == 1)
                {
                    if (!trygg(session, "unreg"))
                        return;
                    Send(session, Msg("GGUNREGSendMSG", session.SteamId.ToString()));
                    Server.Broadcast(Msg("GGUNREGBroadcast").Replace("{0}", session.Identity.Name));
                    GGPlayer par = FindGP(session).FirstOrDefault();
                    exitp(session);
                    gplayers.Remove(par);
                    return;
                }
                else
                {
                    Send(session, Msg("GGOut", session.SteamId.ToString()));
                    return;
                }
            }

            Send(session, Msg("GGHelp", session.SteamId.ToString()));
        }

        string prefix_adm = "<color=red>GG:</color> ";

        [ChatCommand("gg.admin")]
        void SpawnCommand(PlayerSession session, string command, string[] args)
        {
            if (!permission.UserHasPermission(session.SteamId.ToString(), "gungame.admin") && !session.IsAdmin)
            {
                Send(session, "Нет прав");
                return;
            }

            if (args.Length == 0)
            {
                AdminHelp(session);
            }
            if (args[0] == "start")
            {
                STARTGG();
                return;
            }
            if (args[0] == "stop")
            {
                StopGGW();
                return;
            }
            if (args[0] == "config")
            {
                Send(session, "<color=red>GG:</color> Конфиг сохранен");
                SaveConfig();
                return;
            }

            if (args[0].ToLower() == "create")
            {
                if (args.Length <= 1)
                {
                    AdminHelp(session);
                    return;
                }
                else
                {
                    if (_config.places.ContainsKey(args[1].ToLower()))
                    {
                        Send(session, prefix_adm + $"Arena {args[1]} exists");
                        return;
                    }
                    _config.places.Add(args[1].ToLower(), new Configuration.Place()
                    {
                        MaxPlayer = 10,
                        spawnpositions = new List<Vector3>()
                        {
                            new Vector3(0,0,0)
                        }
                    });
                    Send(session, prefix_adm + $"You created {args[1].ToLower()}");
                    SaveConfig();
                }
            }

            if (args[0].ToLower() == "place")
            {
                if (args.Length == 1)
                {
                    AdminHelp(session);
                    return;
                }
                if (args[1].ToLower() != "select")
                {
                    if (!editSessions.ContainsKey((ulong)session.SteamId))
                    {
                        Send(session, prefix_adm + "You need select arena. use: /gg.admin select <name>");
                        return;
                    }
                }

                switch (args[1].ToLower())
                {
                    case "select":
                        if (args.Length < 3)
                        {
                            Send(session, prefix_adm + "You need select arena. use: /gg.admin place select <name>");
                            return;
                        }
                        if (_config.places.ContainsKey(args[2]))
                        {
                            if (editSessions.ContainsKey((ulong)session.SteamId))
                                editSessions.Remove((ulong)session.SteamId);

                            editSessions.Add((ulong)session.SteamId, _config.places[args[2]]);
                            Send(session, prefix_adm + $"You select {args[2]}");
                        }
                        else
                            Send(session, prefix_adm + $"Arena {args[2]} not exists");
                        break;
                    case "set":
                        editSessions[(ulong)session.SteamId].spawnpositions.Add(session.WorldPlayerEntity.transform.position);
                        Send(session, prefix_adm + $"Created new SpawnPoint: " + session.WorldPlayerEntity.transform.position);
                        break;
                    case "list":
                        Send(session, prefix_adm + "List SpawnPoints:");
                        foreach (var point in editSessions[(ulong)session.SteamId].spawnpositions)
                            Send(session, prefix_adm + point);
                        break;
                    case "clear":
                        Send(session, prefix_adm + "SpawnPoints has been cleared");
                        editSessions[(ulong)session.SteamId].spawnpositions.Clear();
                        break;
                    default:
                        AdminHelp(session);
                        break;
                }
                SaveConfig();
                return;
            }
            AdminHelp(session);
        }

        void AdminHelp(PlayerSession session)
        {
            Send(session, "/gg.admin start/stop");
            Send(session, "/gg.admin create <name>");
            Send(session, "/gg.admin place select/set/list/clear");
            Send(session, "/gg.admin list");
        }

        void OnPlayerDisconnected(PlayerSession session)
        {
            if (deathsForSpawn.Contains((ulong)session.SteamId))
                deathsForSpawn.Remove((ulong)session.SteamId);

            var prov = FindGP(session).Count();

            if (prov == 0) return;

            InvClear(session);
            Server.Broadcast(Msg("GGUNREGBroadcast").Replace("{0}", session.Identity.Name));

            GGPlayer par = FindGP(session).FirstOrDefault();
            Teleport(session, par.homeposition);
            stats(session, 10f);
            gplayers.Remove(par);
        }

        void OnPlayerDeath(PlayerSession session, EntityEffectSourceData dataSource)
        {
            if (!arenastart)
                return;

            string KillerName = GetNameOfObject(dataSource.EntitySource);

            if (string.IsNullOrEmpty(KillerName))
                return;

            KillerName = KillerName.Remove(KillerName.Length - 3);

            PlayerSession killersession = getSession(KillerName);

            if (killersession == null)
                return;

            var prov = FindGP(killersession).Count();

            if (prov == 0) return;

            DoAfterDeath(session, killersession);
        }

        void DoAfterDeath(PlayerSession session, PlayerSession killersession)
        {
            GGPlayer par = FindGP(killersession).FirstOrDefault();
            par.kills++;
            stats(session, 2f);
            if (_config.settingsGame.noticekill)
                notice(killersession, string.Format(Msg("GG_Notice_Kills", killersession.SteamId.ToString()), par.kills, _config.game.kol));
            int step = par.getStep(_config.game.kol);
            if (step >= _config.game.weapons.Count())
            {
                StopGGW(killersession);
                return;
            }
            InvClear(killersession);
            givew(killersession, step);
            Broadcast(string.Format(Msg("GG_A_NextWeapon"), killersession.Identity.Name, _config.game.weapons[step].itemname));
            if (_config.game.warlast && step == _config.game.weapons.Count() - 1)
                Broadcast(Msg("GG_A_LastWeapon").Replace("{0}", killersession.Identity.Name));
            InvClear(session);
        }

        void OnTickGG(PlayerSession session)
        {
            if (session.WorldPlayerEntity != null)
                Heal(session, false);
        }

        public class NoEffect : MonoBehaviour
        {
            void Awake()
            {
                InvokeRepeating("HookOneSec", 1f, 1f);
            }

            public void HookOneSec()
            {
                Interface.GetMod().CallHook("OnTickGG", GetComponent<PlayerSession>());
            }
        }


        ///<summary>
        /// Установка время спавна
        ///</summary>
        ///<param name="session"></param>
        ///<param name="a"></param>
        void stats(PlayerSession session, float a)
        {
            var stats = session.WorldPlayerEntity.GetComponent<EntityStats>();
            stats.GetFluidEffect(effect().RespawnTime).SetValue(a);
            ((StandardEntityFluidEffect)stats.GetFluidEffect(effect().RespawnTime)).MaxValue = a + 0.1f;
            ((StandardEntityFluidEffect)stats.GetFluidEffect(effect().RespawnTime)).MinValue = a - 0.1f;
        }

        void refaundinventory(PlayerSession session)
        {
            PlayerInventory inv = session.WorldPlayerEntity.GetComponent<PlayerInventory>();
            GGPlayer par = FindGP(session).First();
            var itemmanager = Singleton<GlobalItemManager>.Instance;
            var amanager = Singleton<AlertManager>.Instance;
            foreach (var fd in par.inventory)
            {
                ItemObject item = itemmanager.CreateItem(fd.Item.Generator);
                inv.SetSlot(fd.slot, fd.Item);
                amanager.ItemReceivedServer(item, item.StackSize, session.Player);
            }
        }

        void OnPlayerRespawned(PlayerSession session, Vector3 pos)
        {
            if (deathsForSpawn.Contains((ulong)session.SteamId))
            {
                deathsForSpawn.Remove((ulong)session.SteamId);
                exitp(session);
            }

            if (!arenastart)
                return;

            var fnd = FindGP(session);

            if (fnd.Count() == 0) return;

            var itemmanager = Singleton<GlobalItemManager>.Instance;
            GGPlayer par = fnd.First();

            var ehs = session.WorldPlayerEntity.GetComponent<EquippedHandlerServer>();
            int step = par.getStep(_config.game.kol);
            givew(session, step);
            Teleport(session, GGSpawn());
        }

        Vector3 GGSpawn()
        {
            Vector3 pos = place.spawnpositions[random.Next(0, place.spawnpositions.Count - 1)];
            return pos;
        }

        void givew(PlayerSession session, int s)
        {
            var wep = _config.game.weapons[s];
            var itemmanager = Singleton<GlobalItemManager>.Instance;
            GiveItem(session, wep.itemid, 1);
            GiveItem(session, wep.ammo, 255);
        }

        void Heal(PlayerSession session, bool health = true)
        {
            EntityStats stats = session.WorldPlayerEntity.GetComponent<EntityStats>();
            stats.GetFluidEffect(effect().TemperatureDelta).Reset(true);
            stats.GetFluidEffect(effect().InternalTemperature).Reset(true);
            stats.GetFluidEffect(effect().ExternalTemperature).Reset(true);
            stats.GetFluidEffect(effect().Toxin).SetValue(0f);
            if (health)
                stats.GetFluidEffect(effect().Health).SetValue(100f);
            foreach (var d in stats.GetBinaryEffects())
            {
                d.Value.Remove();
            }
        }

        // Проверка
        bool trygg(PlayerSession session, string s)
        {
            var statsa = session.WorldPlayerEntity.GetComponent<EntityStats>();
            float a = statsa.GetFluidEffect(effect().Health).GetValue();

            if (a == 0)
            {
                notice(session, Msg("GG_Notice_Death", session.SteamId.ToString()));
                return false;
            }

            if (s == "reg")
            {
                if (!_config.settingsGame.inventorysave)
                {
                    int itemCount = session.WorldPlayerEntity.GetComponent<PlayerInventory>().GetTotalItemCount();
                    if (itemCount > 0)
                    {
                        Send(session, Msg("GGTryReg", session.SteamId.ToString()));
                        return false;
                    }
                }
            }

            return true;
        }

        // Выдать вещь
        void GiveItem(PlayerSession player, string nameOrFullName, int amount)
        {
            if (string.IsNullOrEmpty(nameOrFullName))
                return;

            PlayerInventory inventory = player.WorldPlayerEntity.GetComponent<PlayerInventory>();
            var item = getItemFromName(nameOrFullName);
            if (item == null)
                return;

            GlobalItemManager.Instance.GiveItem(player.Player, item, amount);
        }

        public ItemGeneratorAsset getItemFromName(string name)
        {
            foreach (var item in GlobalItemManager.Instance.GetGenerators())
            {
                if (item.Value.name == name)
                    return item.Value;
            }
            Puts("Error: Item not found!");
            return null;
        }


        ///<summary>
        /// Выдача приза
        ///</summary>
        ///<param name="session"></param>
        void GivePrice(PlayerSession session)
        {
            GGPlayer par = FindGP(session).FirstOrDefault();
            var itemmanager = Singleton<GlobalItemManager>.Instance;
            string x = string.Empty; int b = 1;

            if (!_config.prizeC.randprize)
            {
                foreach (var it in _config.prizeC.prizes)
                {
                    x = it.itemid;
                    b = it.itemcount;
                    GiveItem(session, x, b);
                }
            }
            else
            {
                var it = _config.prizeC.prizes[random.Next(0, _config.prizeC.prizes.Count - 1)];
                x = it.itemid;
                b = it.itemcount;
                GiveItem(session, x, b);
            }
        }

        /*  public static ExplosionConfiguration C4ExplosionConfig;

         Позже будет реализовано
        void ZaprArea(PlayerSession session)
        {
            Vector3 pos = session.WorldPlayerEntity.transform.position;
            GameObject explosionGO = Singleton<HNetworkManager>.Instance.NetInstantiate("ExplosionServer", pos, Quaternion.identity, GameManager.GetSceneTime());
                ExplosionServer explosion = explosionGO.GetComponent<ExplosionServer>();
                explosion.SetData((from o in Resources.FindObjectsOfTypeAll<ExplosiveDynamicServer>() where o.transform.name.Equals("LandCrabDynamicObject") select o).First().Configuration);

                explosion.Explode();
            hurt.SendChatMessage(session,null,"<color=red>[GunGame]</color> Вы не участник арены");	
        }*/

        // Очистить инвентарь
        void InvClear(PlayerSession session)
        {
            var pInventory = session.WorldPlayerEntity.GetComponent<PlayerInventory>();
            pInventory.ClearItems();
        }

        string Msg(string msg, string SteamId = null) => lang.GetMessage(msg, this, SteamId);

        IEnumerable<GGPlayer> FindGP(PlayerSession session)
        {
            return (from x in gplayers where x.steamid == (ulong)session.SteamId select x);
        }

        Vector3 GetPosition(PlayerSession session)
        {
            return session.WorldPlayerEntity.transform.position;
        }

        string GetNameOfObject(UnityEngine.GameObject obj)
        {
            var ManagerInstance = GameManager.Instance;
            return ManagerInstance.GetDescriptionKey(obj);
        }

        void Teleport(PlayerSession session, Vector3 location)
        {
            session.WorldPlayerEntity.transform.position = location;
        }

        private void GetConfig<T>(ref T variable, params string[] path)
        {
            if (path.Length == 0)
                return;

            if (Config.Get(path) == null)
            {
                Config.Set(path.Concat(new object[] { variable }).ToArray());
                PrintWarning($"Added field to config: {string.Join("/", path)}");
            }

            variable = (T)Convert.ChangeType(Config.Get(path), typeof(T));
        }

        void Broadcast(string msg)
        {
            foreach (GGPlayer gp in gplayers)
            {
                try
                {
                    hurt.SendChatMessage(gp.session, null, msg);
                }
                catch { }
            }
        }

        EntityFluidEffectKeyDatabase effect() => Singleton<RuntimeAssetManager>.Instance.RefList.EntityEffectDatabase;

        void Send(PlayerSession session, string msg) => hurt.SendChatMessage(session, null, msg);

        void notice(PlayerSession session, string s) => Singleton<AlertManager>.Instance.GenericTextNotificationServer(s, session.Player);

        private PlayerSession getSession(string identifier)
        {
            var sessions = GameManager.Instance.GetSessions();
            PlayerSession session = null;

            foreach (var i in sessions)
            {
                if (i.Value.Identity.Name.ToUpper().Contains(identifier.ToUpper()) || identifier.Equals(i.Value.SteamId.ToString()))
                {
                    session = i.Value;
                    break;
                }
            }

            return session;
        }
    }
}