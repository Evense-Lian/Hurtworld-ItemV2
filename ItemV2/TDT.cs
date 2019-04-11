using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Team Death Match", "Kaidoz | vk.com/kaidoz", "1.0.7")]
    [Description("Description")]
    class TDT : HurtworldPlugin
    {
        #region [List]

        private EntityFluidEffectKeyDatabase effects = null;

        List<_player> players = new List<_player>();
        Dictionary<PlayerSession, string> editplaces = new Dictionary<PlayerSession, string>();

        #endregion

        #region [Class]

        public class CurrentGame
        {
            public int redkills { get; set; } = 0;
            public int bluekills { get; set; } = 0;
            public int countred { get; set; } = 0;
            public int countblue { get; set; } = 0;
        }

        public class InventItem
        {
            public int slot { get; set; }
            public Item item;

            public class Item
            {
                public ItemGeneratorAsset generator;
                public int count;
            }

            public InventItem(int slot, ItemGeneratorAsset item, int count)
            {
                this.slot = slot;
                this.item = new Item()
                {
                    generator = item,
                    count = count
                };
            }
        }

        public class _player
        {
            public ulong steamid { get; set; }
            public string team { get; set; }
            public float timespawn { get; set; }
            public Vector3 home { get; set; }
            public List<InventItem> items { get; set; }

            public _player(PlayerSession session, string team)
            {
                this.steamid = (ulong)session.SteamId;
                this.timespawn = GetTimeSpawn(session);
                this.home = getPosition(session);
                this.items = getItems(session);
                this.team = team;
            }
        }


        public class Configuration
        {
            [JsonProperty("Places")]
            public Dictionary<string, SettingsPlace> places { get; set; }

            [JsonProperty("Items")]
            public Items items { get; set; }

            [JsonProperty("Settings Game")]
            public SettingsGame settingsGame { get; set; }

            [JsonProperty("Settings Prize")]
            public PrizeC prizeC { get; set; }

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

            public class PrizeC
            {
                [JsonProperty("Random prize")]
                public bool randprize { get; set; }

                [JsonProperty("Prizes")]
                public List<Prize> prizes { get; set; }

                public class Prize
                {
                    [JsonProperty("Item Name")]
                    public string itemid { get; set; }

                    [JsonProperty("Item Count")]
                    public int itemcount { get; set; }
                }
            }

            public class SettingsPlace
            {
                [JsonProperty("Spawn Position for RedTeam")]
                public Vector3 redteam = new Vector3(0, 0, 0);

                [JsonProperty("Spawn Position For BlueTeam")]
                public Vector3 blueteam = new Vector3(0, 0, 0);
            }

            public class Items
            {
                [JsonProperty("Weapons")]
                public List<Weapons> weapons { get; set; }

                [JsonProperty("Red Team")]
                public RedTeam redTeam { get; set; }

                [JsonProperty("Blue Team")]
                public BlueTeam blueTeam { get; set; }

                public class RedTeam
                {
                    [JsonProperty("Hat Name")]
                    public string maskid { get; set; }

                    [JsonProperty("Shirt Name")]
                    public string shirtid { get; set; }

                    [JsonProperty("Pants Name")]
                    public string pantsid { get; set; }

                    [JsonProperty("Boots Name")]
                    public string bootsid { get; set; }
                }

                public class BlueTeam
                {
                    [JsonProperty("Hat Name")]
                    public string maskid { get; set; }

                    [JsonProperty("Shirt Name")]
                    public string shirtid { get; set; }

                    [JsonProperty("Pants Name")]
                    public string pantsid { get; set; }

                    [JsonProperty("Boots Name")]
                    public string bootsid { get; set; }
                }

                public class Weapons
                {
                    [JsonProperty("Weapon ShortName")]
                    public string weaponid { get; set; }

                    [JsonProperty("Ammo ShortName")]
                    public string ammoid { get; set; }
                }
            }

            public class SettingsGame
            {
                [JsonProperty("Count kills for Win")]
                public int killsforwin { get; set; }

                [JsonProperty("Seconds for Spawn")]
                public int secondsforspawn { get; set; }

                [JsonProperty("Max players(10 - 5 vs 5)")]
                public int maxplayer { get; set; }

                [JsonProperty("Time to completion(min.)")]
                public int timecomplete { get; set; }
            }
        }

        #endregion

        #region Config

        Configuration _config;

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
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating config file...");
            _config = new Configuration()
            {
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
                            itemid = "Items/Raw Materials/Seeds (Pitcher)",
                            itemcount = 1,
                        },
                        new Configuration.PrizeC.Prize()
                        {
                            itemid = "Items/Raw Materials/Amber",
                            itemcount = 10,
                        }
                    }
                },
                places = new Dictionary<string, Configuration.SettingsPlace>()
                {
                    {"default",new Configuration.SettingsPlace()
                        {
                            blueteam = new Vector3(0,0,0),
                            redteam = new Vector3(0,0,0)
                        }
                    }
                },
                items = new Configuration.Items()
                {
                    blueTeam = new Configuration.Items.BlueTeam()
                    {
                        bootsid = "Items/Gear/Shoes/Hiking Boots",
                        pantsid = "Items/Gear/Pants/Chemsuit Pants",
                        shirtid = "Items/Gear/Chest/Jacket",
                        maskid = "Items/Gear/Head/Aussie Hat"
                    },
                    redTeam = new Configuration.Items.RedTeam()
                    {
                        bootsid = "Items/Gear/Shoes/Hiking Boots",
                        pantsid = "Items/Gear/Pants/Chemsuit Pants",
                        shirtid = "Items/Gear/Chest/Wool Jumper",
                        maskid = "Items/Gear/Head/Ushanka"
                    },
                    weapons = new List<Configuration.Items.Weapons>()
                    {
                        new Configuration.Items.Weapons()
                        {
                            weaponid = "Items/Weapons/Beretta M9",
                            ammoid = "Items/Ammo/9mm"
                        },
                        new Configuration.Items.Weapons()
                        {
                            weaponid = "Items/Weapons/Bow (Fibreflass)",
                            ammoid = "Items/Ammo/Arrow"
                        },
                        new Configuration.Items.Weapons()
                        {
                            weaponid = "Items/Weapons/AWM",
                            ammoid = "Items/Ammo/308 Bullet"
                        },
                        new Configuration.Items.Weapons()
                        {
                            weaponid = "Items/Weapons/Shotgun",
                            ammoid = "Items/Ammo/ShotgunShell"
                        },
                        new Configuration.Items.Weapons()
                        {
                            weaponid = "Items/Weapons/AR15",
                            ammoid = "Items/Ammo/5.56mm Bullet"
                        }
                    }
                },
                settingsGame = new Configuration.SettingsGame()
                {
                    killsforwin = 50,
                    maxplayer = 10,
                    secondsforspawn = 5,
                    timecomplete = 30
                }

            };
            SaveConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        #endregion

        #region Lang

        private void LoadDefaultMessages()
        {
            var messages = new Dictionary<string, string>
            {
                {"TDTPrefix", "<color=#FAB325>[TDT]</color>"},
                {"ArenaLaunch", "{prefix} Arena launched with map: <i>{0}</i>. Waiting for players. \n Command: /tdt join red/blue"},
                {"ArenaBefore","{prefix} Arena will launch after {0} minutes"},
                {"ArenaConnect", "{prefix} {0} connected to the arena. {1}"},
                {"ArenaStart", "{prefix} Arena started. FIGHT!"},
                {"ArenaNotEnabled", "{prefix} Arena not enabled"},
                {"ArenaJoinPlayer", "{prefix} You join for {0} team"},
                {"ArenaLeavePlayer", "{prefix} You have left the arena"},
                {"ArenaNoRights", "{prefix} No rights"},
                {"ArenaPlayerLeft", "{prefix} {0} left from the server"},
                {"ArenaYouOut", "{prefix} You are out of the arena"},
                {"ArenaYouDead", "{prefix} you're dead"},
                {"ArenaYouChooseTeam", "{prefix} Choose red or blue command"},
                {"ArenaYouAlready", "{prefix} You are already in the arena"},
                {"ArenaYouMany", "{prefix} Many players {0}"},
                {"ArenaYouFull", "{prefix} Arena full"},
                {"ArenaVictory", "{prefix} Victory of {0} team"},
                {"ArenaYouWin", "{prefix} You've won!"},
                {"ArenaYouLose", "{prefix} You lose!"},
                {"NoticeFF", "Is your friend!"},
                {"ArenaAdminList", "{prefix} List: "},
                {"ArenaAdminEdit", "{prefix} You edit a {0} arena"},
                {"ArenaAdminAdded", "{prefix} Create new arena {0}"},
                {"ArenaAdminSyntax", "{prefix} Syntax: {0}"},
                {"ArenaAdminInfo", "{prefix} Info:"},
                {"ArenaAdminNoResult", "{prefix} No results"},
                {"ArenaAdminNoFound", "{prefix} Not found"},
                {"ArenaAdminSetting", "{prefix} Setting a {0} set to {1}"},
                {"ArenaAdminHelp",  "{prefix} Cmds: /tdt admin list \n /tdt admin get \n /tdt admin corner 1/2 \n /td admin spawn red/blue" },
                {"ArenaPlayerHelp", "{prefix} Cmds: /tdt join red/blue /tdt leave"}
            };
            lang.RegisterMessages(messages, this);
        }

        #endregion

        #region Reference

        #endregion

        #region Init

        void OnServerInitialized()
        {
            effects = Singleton<RuntimeAssetManager>.Instance.RefList.EntityEffectDatabase;
            LoadConfig();
            if (!permission.PermissionExists(this.Name.ToLower() + ".admin", this))
                permission.RegisterPermission(this.Name.ToLower() + ".admin", this);
            TimeStart();
        }

        void TimeStart()
        {


            timer.Repeat(_config.startg.timesti ? 60f : _config.startg.timeevery, 0, () =>
            {
                if (_config.startg.timesti)
                {
                    try
                    {
                        foreach (string tim in _config.startg.times)
                        {
                            if (tim == DateTime.Now.ToString("HH:mm"))
                            {
                                gameStart();
                            }
                            if (_config.startg.aboutstart)
                            {
                                DateTime t = DateTime.ParseExact(tim, "HH:mm", null).AddMinutes(-_config.startg.beforetime);

                                if (t.ToString("HH:mm") == DateTime.Now.ToString("HH:mm"))
                                {
                                    Server.Broadcast(Msg("ArenaBefore").Replace("{0}", Convert.ToString(_config.startg.beforetime)));
                                }
                            }
                        }
                    }
                    catch { }
                }
                else
                {
                    gameStart();
                }
            });
        }

        #endregion

        #region Main

        System.Random rnd = new System.Random();

        #region Arena

        private CurrentGame currentGame;
        private KeyValuePair<string, Configuration.SettingsPlace> arenaCurrent;
        private Configuration.Items.Weapons weapon;

        void gameStart()
        {
            arenaCurrent = _config.places.ElementAt(rnd.Next(0, _config.places.Count() - 1));
            Broadcast(string.Format(Msg("ArenaLaunch"), arenaCurrent.Key));
            currentGame = new CurrentGame();
            weapon = _config.items.weapons[rnd.Next(0, _config.items.weapons.Count() - 1)];
        }

        void arenaStart()
        {
            foreach (var d in players)
            {
                SetMove(getSession(d.steamid), true);
                GiveItem(getSession(d.steamid), weapon.weaponid, 1);
                GiveItem(getSession(d.steamid), weapon.ammoid, 255);
            }
            SendChatAll(Msg("ArenaStart"));
        }

        void arenaAddPlayer(PlayerSession session, string team)
        {
            players.Add(new _player(session, team));
            Clearinventory(session);
            SetTimeSpawn(session, _config.settingsGame.secondsforspawn);
            SetItem(session, team);
            TeleportTeam(session, team);
            SetMove(session, false);
            Broadcast(string.Format(Msg("ArenaConnect", string.Empty), session.Identity.Name, $"{players.Count()} of {_config.settingsGame.maxplayer}"));
            if (currentGame.countblue + currentGame.countred >= _config.settingsGame.maxplayer)
            {
                arenaStart();
            }
        }


        #endregion


        [ChatCommand("tdt")]
        void JoinGame(PlayerSession session, string command, string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    GetHelp(session);
                    return;
                }

                if (currentGame == null && args.Length > 1 && args[0].ToLower() != "admin")
                {
                    SendChat(session, Msg("ArenaNotEnabled", session));
                    return;
                }
                if (args.Length > 2 && args[0].ToLower() != "admin")
                {
                    GetHelp(session);
                }
                switch (args[0].ToLower())
                {
                    case "join":
                        if (args.Length < 2)
                        {
                            SendChat(session, Msg("ArenaYouChooseTeam", session));
                            return;
                        }
                        string team = args[1].ToLower();
                        if (!checkPlayerForArena(session, team))
                            return;

                        switch (team)
                        {
                            case "blue":
                                arenaAddPlayer(session, team);
                                SendChat(session, string.Format(Msg("ArenaJoinPlayer", session), team));
                                break;
                            case "red":
                                arenaAddPlayer(session, team);
                                SendChat(session, string.Format(Msg("ArenaJoinPlayer", session), team));
                                break;
                            default:
                                SendChat(session, Msg("ArenaYouChooseTeam", session));
                                break;

                        }
                        return;
                    case "leave":
                        if (!checkExitFromArena(session))
                            return;
                        ExitPlayer(session);
                        SendChat(session, Msg("ArenaLeavePlayer", session));
                        return;
                    default:
                        if (args[0].ToLower() == "admin")
                            break;
                        GetHelp(session);
                        return;
                }
                if (!GetPerm(session))
                {
                    SendChat(session, Msg("ArenaNoRights", session));
                    return;
                }
                if (args.Length < 2)
                {
                    GetHelpAdmin(session);
                    return;
                }
                string valFind;
                string fi;
                Configuration.SettingsPlace place;
                switch (args[1].ToLower())
                {
                    case "start":
                        gameStart();
                        break;
                    case "gamestart":
                        arenaStart();
                        break;
                    case "list":
                        SendChat(session, Msg("ArenaAdminList", session));
                        for (int a = 0; a < _config.places.Count; a++)
                            SendChat(session, a + ". " + _config.places.Keys.ToList()[a]);
                        break;
                    case "get":
                        valFind = args[2].ToLower();
                        if (!_config.places.ContainsKey(valFind))
                        {
                            SendChat(session, Msg("ArenaAdminNoResult", session));
                            return;
                        }
                        var result = _config.places[args[2]];
                        SendChat(session, "red: " + result.redteam + "\n" + "blue: " + result.blueteam);
                        break;
                    case "edit":
                        if (args.Length == 2)
                            break;
                        valFind = args[2].ToLower();
                        if (_config.places.ContainsKey(valFind))
                        {
                            if (editplaces.ContainsKey(session))
                            {
                                editplaces.Remove(session);
                            }
                            editplaces.Add(session, valFind);
                            SendChat(session, string.Format(Msg("ArenaAdminEdit", session.SteamId.ToString()), valFind));
                        }
                        else
                            SendChat(session, Msg("ArenaAdminNoFound", session));
                        break;
                    case "spawn":
                        fi = getEditSession(session);
                        if (string.IsNullOrEmpty(fi))
                            break;
                        place = GetPlace(fi);
                        switch (args[2].ToLower())
                        {
                            case "blue":
                                SendChat(session, string.Format(Msg("ArenaAdminSetting", session.SteamId.ToString()), "spawn blue", getPosition(session).ToString()));
                                place.blueteam = getPosition(session);
                                break;
                            case "red":
                                SendChat(session, string.Format(Msg("ArenaAdminSetting", session.SteamId.ToString()), "spawn red", getPosition(session).ToString()));
                                place.redteam = getPosition(session);
                                break;
                            default:
                                SendChat(session, string.Format(Msg("ArenaAdminSyntax", session.SteamId.ToString()), "tdt admin spawn blue/red"));
                                break;
                        }
                        return;
                    case "add":
                        fi = getEditSession(session);
                        if (string.IsNullOrEmpty(fi))
                            break;
                        string nameAdd = args[2].ToLower();
                        SendChat(session, string.Format(Msg("ArenaAdminAdded", session.SteamId.ToString()), nameAdd));
                        _config.places.Add(nameAdd, new Configuration.SettingsPlace()
                        {
                            blueteam = new Vector3(0, 0, 0),
                            redteam = new Vector3(0, 0, 0)
                        });
                        break;
                }
                SaveConfig();
            }
            catch (Exception ex)
            {
                Puts("Error: {prefix}{1}{2}", ex.Message, Environment.NewLine, ex.StackTrace);
                SendChat(session, "[Error] Something went wrong!");
                //Log("Errors", string.Format("Error: {prefix}{1}{2}", ex.Message, Environment.NewLine, ex.StackTrace));
            }
        }

        object OnServerCommand(string cmd, string[] args)
        {
            if (cmd == "tdt")
            {
                switch (args[0])
                {
                    case "add":
                        if (args.Length > 2 || args.Length == 1)
                        {
                            Puts("syntax: tdt add 'name'");
                            break;
                        }
                        if (_config.places.ContainsKey(args[1]))
                        {
                            Puts("Arena exists: 'name'");
                            break;
                        }
                        Puts("Successful added arena: " + args[1]);
                        _config.places.Add(args[1], new Configuration.SettingsPlace());
                        SaveConfig();
                        break;

                }
                return true;
            }
            return null;
        }

        #region Hooks

        object canExtTeleport(PlayerSession session)
        {
            if (FindGP(session) != null)
            {
                return true;
            }
            return null;
        }

        object canRedeemKit(PlayerSession session)
        {
            if (FindGP(session) != null)
            {
                return true;
            }
            return null;
        }

        object OnPlayerSuicide(PlayerSession session)
        {
            if (FindGP(session) != null && !session.IsAdmin)
            {
                return true;
            }
            return null;
        }

        // disable message from AdvancedDeathMessages
        bool OnDeathMessageADV(PlayerSession session)
        {
            if (FindGP(session) != null)
                return true;

            return false;
        }

        void OnPlayerRespawned(PlayerSession session, Vector3 pos)
        {
            if (currentGame == null)
                return;

            var prov = FindGP(session);

            if (prov.Count() == 0) return;

            var par = prov.FirstOrDefault();
            Clearinventory(session);
            TeleportTeam(session, par.team);
            SetItem(session, par.team);
            GiveItem(session, weapon.weaponid, 1);
            GiveItem(session, weapon.ammoid, 255);
            SetTimeSpawn(session, _config.settingsGame.secondsforspawn);
        }

        /*[HookMethod("IOnTakeDamage")]
        private void IOnTakeDamage(EntityEffectFluid effect, EntityStats target, EntityEffectSourceData source)
        {
            HNetworkView networkView = target.networkView;
            if (networkView != null)
            {
                PlayerSession session = GameManager.Instance.GetSession(networkView.owner);
                if (session != null)
                {
                    OnPlayerTakeDamage(session, source);
                }
            }
        }*/

        void OnPlayerTakeDamage(PlayerSession session, EntityEffectSourceData dataSource)
        {
            string name = session.Identity.Name;
            string tmpName = GetNameOfObject(dataSource.EntitySource);
            if (!string.IsNullOrEmpty(tmpName) && Player.Find(tmpName.Remove(tmpName.Length - 3)) != null)
            {
                string KillerName = tmpName.Remove(tmpName.Length - 3);
                var sesskiller = getSession(KillerName);
                var _plk = FindGP(sesskiller);
                if (_plk == null)
                    return;

                var _pld = FindGP(session).FirstOrDefault();
                if (_plk.FirstOrDefault().team == _pld.team)
                {
                    Notice(sesskiller, Msg("NoticeFF", session));
                    //dataSource.Value = 0;
                    dataSource.LastEffectSource = null;
                }
            }
        }

        void OnPlayerDeath(PlayerSession session, EntityEffectSourceData dataSource)
        {
            if (dataSource.EntitySource == null)
                return;

            string KillerName = GetNameOfObject(dataSource.EntitySource);

            if (string.IsNullOrEmpty(KillerName))
                return;

            string name = session.Identity.Name;
            KillerName = KillerName.Remove(KillerName.Length - 3);
            var killer = getSession(KillerName);

            if (killer == null)
                return;

            var fnd = FindGP(killer).FirstOrDefault();
            switch (fnd.team)
            {
                case "blue":
                    currentGame.bluekills++;
                    NoticeAll(currentGame.bluekills + " of " + _config.settingsGame.killsforwin, "blue");
                    if (currentGame.bluekills >= _config.settingsGame.killsforwin)
                    {
                        SendChatAll(string.Format(Msg("ArenaVictory", string.Empty), "<color=blue>blue</color>"));
                        ExitVictory("blue");
                    }
                    break;
                case "red":
                    currentGame.redkills++;
                    NoticeAll(currentGame.redkills + " of " + _config.settingsGame.killsforwin, "red");
                    if (currentGame.bluekills >= _config.settingsGame.killsforwin)
                    {
                        SendChatAll(string.Format(Msg("ArenaVictory", string.Empty), "<color=red>red</color>"));
                        ExitVictory("red");
                    }
                    break;
            }
        }


        void OnPlayerDisconnected(PlayerSession session)
        {
            if (editplaces.ContainsKey(session))
            {
                editplaces.Remove(session);
            }
            if (FindGP(session) != null)
            {
                try
                {
                    ExitPlayer(session);
                }
                catch { }
                SendChatAll(string.Format(Msg("ArenaPlayerLeft", string.Empty), session.Identity.Name));
            }
        }

        #endregion

        #endregion

        #region Helper

        void TeleportTeam(PlayerSession session, string team)
        {
            var blue = new Vector3(arenaCurrent.Value.blueteam.x + rnd.Next(-1, 1), arenaCurrent.Value.blueteam.y, arenaCurrent.Value.blueteam.z + rnd.Next(-1, 1));
            var red = new Vector3(arenaCurrent.Value.redteam.x + rnd.Next(-1, 1), arenaCurrent.Value.redteam.y, arenaCurrent.Value.redteam.z + rnd.Next(-1, 1));
            switch (team)
            {
                case "blue":
                    Teleport(session, blue);
                    break;
                case "red":
                    Teleport(session, red);
                    break;
            }
        }

        void SetItem(PlayerSession session, string team)
        {
            try
            {
                var inv = session.WorldPlayerEntity.GetComponent<PlayerInventory>();
                var blue = _config.items.blueTeam;
                var red = _config.items.redTeam;
                switch (team)
                {
                    case "blue":
                        inv.SetSlot(11, GetItem(blue.bootsid, 1));
                        inv.SetSlot(10, GetItem(blue.pantsid, 1));
                        inv.SetSlot(9, GetItem(blue.shirtid, 1));
                        inv.SetSlot(8, GetItem(blue.maskid, 1));
                        break;
                    case "red":
                        inv.SetSlot(11, GetItem(red.bootsid, 1));
                        inv.SetSlot(10, GetItem(red.pantsid, 1));
                        inv.SetSlot(9, GetItem(red.shirtid, 1));
                        inv.SetSlot(8, GetItem(red.maskid, 1));
                        break;
                }
                inv.Invalidate(false);
            }
            catch
            {
                Puts("ERROR: SetItem");
            }
        }

        IEnumerable<_player> FindGP(PlayerSession session)
        {
            var result = (from x in players where x.steamid == (ulong)session.SteamId select x);
            return result.Count() == 0 ? null : result;
        }

        void RefaundInv(PlayerSession session)
        {
            PlayerInventory inv = session.WorldPlayerEntity.GetComponent<PlayerInventory>();
            var itemmanager = Singleton<GlobalItemManager>.Instance;
            var amanager = Singleton<AlertManager>.Instance;
            var par = FindGP(session).FirstOrDefault();
            foreach (var fd in par.items)
            {
                ItemObject item = itemmanager.CreateItem(fd.item.generator, fd.item.count);
                inv.SetSlot(fd.slot, item);
                amanager.ItemReceivedServer(item, item.StackSize, session.Player);
                inv.Invalidate(false);
            }
        }

        ItemObject GetItem(string id, int count)
        {
            var imng = Singleton<GlobalItemManager>.Instance;
            var item = getItemFromName(id);
            var obj = imng.CreateItem(item, count);
            return obj;
        }

        public ItemGeneratorAsset getItemFromName(string name)
        {
            foreach (var it in GlobalItemManager.Instance.GetGenerators())
                if (it.Value.GetNameKey() == name)
                    return GlobalItemManager.Instance.GetGenerators()[it.Value.GeneratorId];

            return GlobalItemManager.Instance.GetGenerators()[1];
        }

        void Clearinventory(PlayerSession s)
        {
            var targetInventory = s.WorldPlayerEntity.GetComponent<PlayerInventory>();
            targetInventory.ClearItems();
        }

        bool checkent(string name)
        {
            if (name.Contains("SleeperLootCrate") || name.Contains("Cache"))
                return true;
            return false;
        }

        void SetMove(PlayerSession session, bool set)
        {
            CharacterMotorSimple motor = session.WorldPlayerEntity.GetComponent<CharacterMotorSimple>();
            motor.CanMove = set;
        }

        void DebugPuts(string message)
        {
            if (1 == 1)
                Puts("DEBUG: " + message);
        }

        bool GetMove(PlayerSession session) => session.WorldPlayerEntity.GetComponent<CharacterMotorSimple>().CanMove;

        void Heal(PlayerSession session)
        {
            EntityStats stats = session.WorldPlayerEntity.GetComponent<EntityStats>();
            Dictionary<EntityFluidEffectKey, IEntityFluidEffect> effects = stats.GetFluidEffects();

            foreach (KeyValuePair<EntityFluidEffectKey, IEntityFluidEffect> effect in effects)
            {
                effect.Value.Reset(true);
            }
        }

        void SetTimeSpawn(PlayerSession session, float set)
        {
            var stats = session.WorldPlayerEntity.GetComponent<EntityStats>();
            stats.GetFluidEffect(effects.RespawnTime).SetValue(set);
            ((StandardEntityFluidEffect)stats.GetFluidEffect(effects.RespawnTime)).MinValue = set + 0.1f;
            ((StandardEntityFluidEffect)stats.GetFluidEffect(effects.RespawnTime)).MaxValue = set - 0.1f;
        }

        static float GetTimeSpawn(PlayerSession session)
        {
            var stats = session.WorldPlayerEntity.GetComponent<EntityStats>();
            return stats.GetFluidEffect(RuntimeAssetManager.Instance.RefList.EntityEffectDatabase.RespawnTime).GetValue();
        }

        void Teleport(PlayerSession session, Vector3 position)
        {
            session.WorldPlayerEntity.transform.position = position;
        }

        bool checkExitFromArena(PlayerSession session)
        {
            if (FindGP(session) == null)
            {
                SendChat(session, Msg("ArenaYouOut", session));
                return false;
            }
            if (session.WorldPlayerEntity == null)
            {
                SendChat(session, Msg("ArenaYouDead", session));
                return false;
            }
            return true;
        }

        bool checkPlayerForArena(PlayerSession session, string team)
        {
            if (FindGP(session) != null)
            {
                SendChat(session, Msg("ArenaYouAlready", session));
                return false;
            }
            if (session.WorldPlayerEntity == null)
            {
                SendChat(session, Msg("ArenaYouDead", session));
                return false;
            }
            if ((from x in players where x.team == team select x).Count() == _config.settingsGame.maxplayer / 2)
            {
                SendChat(session, string.Format(Msg("ArenaYouMany", session.SteamId.ToString()), team));
                return false;
            }
            if (players.Count() == _config.settingsGame.maxplayer)
            {
                SendChat(session, Msg("ArenaYouFull", session));
                return false;
            }
            return true;
        }

        string Msg(string msg, string steamid) => lang.GetMessage(msg, this, steamid).Replace("{prefix}", lang.GetMessage("TDTPrefix", this, steamid));

        string Msg(string msg) => lang.GetMessage(msg, this, null).Replace("{prefix}", lang.GetMessage("TDTPrefix", this, null));

        string Msg(string msg, PlayerSession session) => lang.GetMessage(msg, this, session.SteamId.ToString()).Replace("{prefix}", lang.GetMessage("TDTPrefix", this, session.SteamId.ToString()));

        void Broadcast(string text) => Server.Broadcast(text);

        static Vector3 getPosition(PlayerSession session) => session.WorldPlayerEntity.transform.position;

        Configuration.SettingsPlace GetPlace(string text)
        {
            foreach (var ad in _config.places)
                if (ad.Key.ToLower() == text)
                    return ad.Value;

            return null;
        }

        string getEditSession(PlayerSession session)
        {
            foreach (var da in editplaces)
                if (da.Key == session)
                    return da.Value;

            return string.Empty;
        }

        void Teleport(PlayerSession session, Vector3 position, int at = 0, int to = 0)
        {
            session.WorldPlayerEntity.transform.position = new Vector3(position.x + rnd.Next(at, to), position.y, position.x + rnd.Next(at, to));
        }

        /// <summary>
        /// Get acces for use admin commands
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        bool GetPerm(PlayerSession session)
        {
            return session.IsAdmin || permission.UserHasPermission(session.SteamId.ToString(), this.Name.ToLower() + ".admin");
        }

        /// <summary>
        /// Send Help message to player
        /// </summary>
        /// <param name="session"></param>
        void GetHelp(PlayerSession session)
        {
            SendChat(session, Msg("ArenaPlayerHelp", session));
        }


        /// <summary>
        /// Send Help Message to admin
        /// </summary>
        /// <param name="session"></param>
        void GetHelpAdmin(PlayerSession session)
        {
            SendChat(session, Msg("ArenaAdminHelp", session));
        }

        void ExitPlayer(PlayerSession session)
        {
            var pl = FindGP(session).FirstOrDefault();
            Clearinventory(session);
            RefaundInv(session);
            Teleport(session, pl.home);
            players.Remove(pl);
            SetMove(session, true);
        }

        void ExitVictory(string team)
        {
            Configuration.PrizeC.Prize item = null;
            if (_config.prizeC.randprize)
                item = _config.prizeC.prizes[rnd.Next(0, _config.prizeC.prizes.Count - 1)];

            foreach (var pl in players)
            {
                PlayerSession session = getSession(pl.steamid);
                Clearinventory(session);
                RefaundInv(session);
                Teleport(session, pl.home);
                if (pl.team == team)
                {
                    SendChat(session, Msg("ArenaYouWin", session));
                    if (_config.prizeC.randprize)
                    {
                        GiveItem(session, item.itemid, item.itemcount);
                    }
                    else
                    {
                        foreach (var ite in _config.prizeC.prizes)
                            GiveItem(session, ite.itemid, ite.itemcount);
                    }
                }
                else
                    SendChat(session, Msg("ArenaYouLose", session));
            }
            currentGame = null;
            players.Clear();
        }

        void GiveItem(PlayerSession player, string shortname, int amount)
        {
            var ItemMgr = Singleton<GlobalItemManager>.Instance;
            PlayerInventory inventory = player.WorldPlayerEntity.GetComponent<PlayerInventory>();
            ItemGeneratorAsset item = GetItem(shortname, amount).Generator;
            ItemMgr.GiveItem(player.Player, item, amount);
        }

        /// <summary>
        /// Send message to player
        /// </summary>
        /// <param name="session"></param>
        /// <param name="message"></param>
        void SendChat(PlayerSession session, string message) => hurt.SendChatMessage(session, null, message);

        void SendChatAll(string message)
        {
            foreach (var pl in players)
            {
                SendChat(getSession(pl.steamid), message);
            }
        }

        void Notice(PlayerSession session, string message) => Singleton<AlertManager>.Instance.GenericTextNotificationServer(message, session.Player);

        void NoticeAll(string message, string team)
        {
            foreach (var pl in players)
            {
                if (pl.team == team)
                {
                    Notice(getSession(pl.steamid), message);
                }
            }
        }

        PlayerSession getSession(ulong steam) => Player.FindById(steam.ToString());

        string GetNameOfObject(UnityEngine.GameObject obj)
        {
            var ManagerInstance = GameManager.Instance;
            return ManagerInstance.GetDescriptionKey(obj);
        }

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


        /// <summary>
        /// Get items at Player
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        static public List<InventItem> getItems(PlayerSession session)
        {
            var inv = session.WorldPlayerEntity.GetComponent<PlayerInventory>();
            List<InventItem> items = new List<InventItem>();

            for (int a = 0; a < inv.Capacity; a++)
            {
                if (inv.GetSlot(a) != null)
                {
                    var it = inv.GetSlot(a);
                    items.Add(new InventItem(a, it.Generator, it.StackSize));
                }
            }
            return items;
        }


        #endregion
    }
}
