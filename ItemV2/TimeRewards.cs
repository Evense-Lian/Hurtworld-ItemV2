using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide;
using UnityEngine;
using System.Text;
using Oxide.Core.Plugins;
using Newtonsoft.Json;
using Oxide.Core.Libraries;
using System.Text.RegularExpressions;

namespace Oxide.Plugins
{
    [Info("TimeRewards", "Kaidoz", "1.0.1")]
    class TimeRewards : HurtworldPlugin
    {
        System.Random rnd = new System.Random();

        #region Config

        public CFG cfg;

        public class CFG
        {
            [JsonProperty("Command")]
            public string cmd = "bonus";

            [JsonProperty("Block without prefix")]
            public bool EnabledPrefix = false;

            [JsonProperty("Prefix for bonus")]
            public string Prefix = "[DP]";

            [JsonProperty("If player is AFK to timer stop")]
            public bool AfkBlocked = true;

            [JsonProperty("Min. for block timer if player is AFK")]
            public int AfkMin = 5;

            [JsonProperty("List bonus")]
            public List<TimeBonus> timeBonus = new List<TimeBonus>();

            public class TimeBonus
            {
                [JsonProperty("Name bonus")]
                public string NameBonus;

                [JsonProperty("Receive again")]
                public bool ReceiveAgain;

                [JsonProperty("Min(s)")]
                public int minutGame;

                [JsonProperty("Hour(s)")]
                public int hourGame;

                public string Command;

                [JsonProperty("Item")]
                public GiveItem item;

                public class GiveItem
                {
                    [JsonProperty("Item Name")]
                    public string name;

                    [JsonProperty("Min. Count")]
                    public int minCount;

                    [JsonProperty("Max Count")]
                    public int maxCount;
                }
            }
        }

        public class Player_
        {
            public int Time = 0;

            public string lastMessage;

            public List<CFG.TimeBonus> listBonus = new List<CFG.TimeBonus>();
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"Prefix","<color=#30B4FF>[🎁]</color>"},
                {"GetBonus","You get bonus {namebonus}!\nEnter command to get: /bonus"},
                {"TakeBonus","You taked all bonuses!"},
                {"Closestbonus","Closest bonus with name: {namebonus}\n Need remains to play: {time}"},
                {"NoPrefix", "You need to set the prefix to the nickname to get the bonuses!\n Prefix: {prefix}" },
                {"NoBonus", "You have no bonuses:c" }
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"Prefix","<color=#30B4FF>[🎁]</color>"},
                {"GetBonus","Вы получили бонус {namebonus}!\nВведите команду для получения: /bonus"},
                {"TakeBonus","Вы забрали все бонусы!"},
                {"Closestbonus","Ближайший бонус под названием: {namebonus}\n Осталось наиграть часов: {time}"},
                {"NoPrefix", "Вам нужно установить префикс для получения бонусов!\n Префикс: {prefix}" },
                {"NoBonus", "У вас нет бонусов:c" }
            }, this, "ru");
        }

        #endregion

        private new void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                cfg = Config.ReadObject<CFG>();
                if (cfg == null) throw new Exception();
            }
            catch
            {
                Config.WriteObject(cfg, false, $"{Interface.Oxide.ConfigDirectory}/{Name}.jsonError");
                PrintError("The configuration file contains an error and has been replaced with a default config.\n" +
                           "The error configuration file was saved in the .jsonError extension");
                LoadDefaultConfig();
            }
            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating new config file for TimeRewards...");
            cfg = new CFG()
            {
                timeBonus = new List<CFG.TimeBonus>()
                {
                    new CFG.TimeBonus()
                    {
                        NameBonus = "One-time weapons kit",
                        ReceiveAgain = false,
                        minutGame = 0,
                        hourGame = 3,
                        Command = "ExampleCommand %steamid%",
                        item = null
                    },
                    new CFG.TimeBonus()
                    {
                        NameBonus = "Ambers",
                        ReceiveAgain = false,
                        minutGame = 0,
                        hourGame = 1,
                        Command = "",
                        item = new CFG.TimeBonus.GiveItem()
                        {
                            name = "Amber",
                            minCount = 5,
                            maxCount = 10
                        }
                    }
                }
            };
            SaveConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(cfg);

        void Unload()
        {
            SaveData();
            foreach (PlayerSession session in GameManager.Instance._playerSessions.Values)
            {
                session.WorldPlayerEntity.gameObject.GetComponent<TimePlayed>().TryDestroy();
            }
        }

        void Loaded()
        {
            LoadConfig();
            LoadData();
            CheckAllPlayer();
        }

        #region Data

        Dictionary<ulong, Player_> playerPlayed = new Dictionary<ulong, Player_>();

        void LoadData()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("TimeRewards"))
                playerPlayed = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, Player_>>("TimeRewards");
        }

        void SaveData() => Interface.Oxide.DataFileSystem.WriteObject("TimeRewards", playerPlayed);

        void OnServerSave()
        {
            SaveData();
        }

        #endregion

        [ChatCommand("bonus")]
        void BonusCMD(PlayerSession session, string cmd, string[] args)
        {
            if (cfg.EnabledPrefix && !session.Identity.Name.Contains(cfg.Prefix))
            {
                SendChat(session, Msg("NoPrefix").Replace("{prefix}", cfg.Prefix));
                return;
            }
            var Player = playerPlayed[getUlong(session)];
            if (session.IsAdmin)
                SendChat(session, "[Видит только админ] " + Player.Time.ToString());
            if (Player.listBonus.Count == 0)
            {
                int played = Player.Time;
                var find = cfg.timeBonus.Where(x => x.hourGame * 60 + x.minutGame > played).OrderBy(x => Math.Abs(played - x.hourGame * 60 + x.minutGame));
                if (find.Count() != 0)
                {
                    var bonus = find.First();
                    int h = bonus.hourGame != 0 ? bonus.hourGame - played / 60 : 0;
                    int m = bonus.minutGame != 0 ? bonus.minutGame - played % 60 : 0;
                    var td = DateTime.Now.AddHours(bonus.hourGame).AddMinutes(bonus.minutGame) - DateTime.Now.AddMinutes(Player.Time);
                    SendChat(session, Msg("Closestbonus", session.SteamId.ToString()).Replace("{namebonus}", bonus.NameBonus).Replace("{time}", td.ToString()));
                }
                SendChat(session, Msg("NoBonus"));
                return;
            }
            foreach (var bonus in Player.listBonus)
            {
                if (!string.IsNullOrEmpty(bonus.Command))
                    ConsoleManager.Instance.ExecuteCommand((bonus.Command));
                var it = bonus.item;
                if (it != null)
                {
                    GiveItem(session, bonus.item.name, rnd.Next(it.minCount, it.maxCount));
                }
            }
            Player.listBonus.Clear();
            SaveData();
            SendChat(session, Msg("TakeBonus"));
        }

        void CheckAllPlayer()
        {
            int err = 0;
            foreach (PlayerSession session in GameManager.Instance._playerSessions.Values)
            {
                if (session.IsLoaded)
                {
                    try
                    {
                        if (!playerPlayed.ContainsKey(getUlong(session)))
                        {
                            playerPlayed.Add(getUlong(session), new Player_());
                        }
                        session.WorldPlayerEntity.gameObject.GetComponent<TimePlayed>().TryDestroy();
                        session.WorldPlayerEntity.gameObject.AddComponent<TimePlayed>();
                        session.WorldPlayerEntity.gameObject.GetComponent<TimePlayed>().SetSession(session);
                    }
                    catch
                    {
                        err++;
                    }
                }
            }
            Puts("[CheckAllPlayer] Err: " + err);
            SaveData();
        }

        void OnPlayerConnected(PlayerSession session)
        {
            if (!playerPlayed.ContainsKey(getUlong(session)))
            {
                playerPlayed.Add(getUlong(session), new Player_());
                SaveData();
            }
            session.WorldPlayerEntity.gameObject.GetComponent<TimePlayed>().TryDestroy();
            session.WorldPlayerEntity.gameObject.AddComponent<TimePlayed>();
            session.WorldPlayerEntity.gameObject.GetComponent<TimePlayed>().SetSession(session);
        }

        void OnPlayerDisconnected(PlayerSession session)
        {
            session.WorldPlayerEntity.gameObject.GetComponent<TimePlayed>().TryDestroy();
        }

        void OnMinutGame(PlayerSession session, int afkTime)
        {
            if (cfg.EnabledPrefix && !session.Identity.Name.Contains(cfg.Prefix))
                return;
            if (cfg.AfkBlocked && afkTime >= cfg.AfkMin)
                return;
            if (session == null)
                return;
            var Player = playerPlayed[getUlong(session)];
            Player.Time++;
            bool msg = false;
            int played = Player.Time;
            foreach (var bonus_ in cfg.timeBonus)
            {
                var _bonus = bonus_;
                int needPlay = bonus_.hourGame * 60 + bonus_.minutGame;
                if (played == bonus_.hourGame * 60 + bonus_.minutGame)
                {
                    if (!string.IsNullOrEmpty(bonus_.Command))
                    {
                        string cmd = Regex.Replace(bonus_.Command, "%steamid%", session.SteamId.ToString(), RegexOptions.IgnoreCase);
                        _bonus.Command = cmd;
                    }
                    Player.listBonus.Add(_bonus);
                    SaveData();
                    msg = true;
                    SendChat(session, Msg("GetBonus", session.SteamId.ToString()).Replace("{namebonus}", bonus_.NameBonus));
                }
                else
                {
                    if (bonus_.ReceiveAgain && played % needPlay == 0)
                    {
                        if (!string.IsNullOrEmpty(bonus_.Command))
                        {
                            string cmd = Regex.Replace(bonus_.Command, "%steamid%", session.SteamId.ToString(), RegexOptions.IgnoreCase);
                            _bonus.Command = cmd;
                        }
                        Player.listBonus.Add(_bonus);
                        SaveData();
                        msg = true;
                        SendChat(session, Msg("GetBonus", session.SteamId.ToString()).Replace("{namebonus}", bonus_.NameBonus));
                    }
                }
            }
            var find = cfg.timeBonus.Where(x => x.hourGame * 60 + x.minutGame > played).OrderBy(x => Math.Abs(played - x.hourGame * 60 + x.minutGame));
            if (find.Count() == 0)
                return;
            var bonus = find.First();
            int h = bonus.hourGame != 0 ? bonus.hourGame - played / 60 : 0;
            int m = bonus.minutGame != 0 ? bonus.minutGame - played % 60 : 0;
            var td = DateTime.Now.AddHours(bonus.hourGame).AddMinutes(bonus.minutGame) - DateTime.Now.AddMinutes(Player.Time);


            if (string.IsNullOrEmpty(Player.lastMessage) || (DateTime.Now - DateTime.Parse(Player.lastMessage)).TotalHours >= 1 || msg)
            {
                Player.lastMessage = DateTime.Now.ToString();
                SendChat(session, Msg("Closestbonus", session.SteamId.ToString()).Replace("{namebonus}", bonus.NameBonus).Replace("{time}", td.ToString()));
            }
        }

        #region GameStores

        #endregion

        #region Mono

        public class TimePlayed : MonoBehaviour
        {
            PlayerSession session = null;

            string timePlayed = "{DarkPluginsId}";

            Vector3 oldPosition;

            int afkTime = 0;

            void Awake()
            {
                InvokeRepeating("OnTickMin", 1f, 60f);
            }

            void OnTickMin()
            {
                if (oldPosition == transform.position)
                    afkTime++;
                else
                    afkTime = 0;

                oldPosition = transform.position;
                if (session == null)
                {
                    string name = GameManager.Instance.GetDescriptionKey(gameObject);
                    name = name.Remove(name.Length - 3);
                    session = GetSession(name);
                    if (session != null)
                        Debug.Log(name);
                }
                else
                    Interface.CallHook("OnMinutGame", session, afkTime);

            }

            public void SetSession(PlayerSession session_)
            {
                session = session_;
            }
        }

        #endregion

        #region Helper

        public static PlayerSession GetSession(string identifier)
        {
            var sessions = GameManager.Instance.GetSessions();
            PlayerSession session = null;

            foreach (var i in sessions)
            {
                if (i.Value.Identity.Name.ToLower().Contains(identifier.ToLower()) || identifier.Equals(i.Value.SteamId.ToString()))
                {
                    session = i.Value;
                    break;
                }
            }

            return session;
        }

        void GiveItem(PlayerSession player, string shortname, int amount)
        {
            var ItemMgr = Singleton<GlobalItemManager>.Instance;
            PlayerInventory inventory = player.WorldPlayerEntity.GetComponent<PlayerInventory>();
            ItemGeneratorAsset item = GetItem(shortname, amount).Generator;
            ItemMgr.GiveItem(player.Player, item, amount);
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
                if (it.Value.name == name)
                    return GlobalItemManager.Instance.GetGenerators()[it.Value.GeneratorId];

            return GlobalItemManager.Instance.GetGenerators()[1];
        }

        string Msg(string msg, string SteamId = null)
        {
            return lang.GetMessage("Prefix", this, SteamId) + " " + lang.GetMessage(msg, this, SteamId);
        }

        void SendChat(PlayerSession session, string message) => hurt.SendChatMessage(session, null, message);

        ulong getUlong(PlayerSession session) => (ulong)session.SteamId;

        #endregion
    }
}
