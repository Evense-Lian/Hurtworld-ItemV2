using Newtonsoft.Json;
using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using uLink;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("NoEscape ItemV2", "Kaidoz", "1.3.1")]
    [Description("Blocks system for Hurtworld ItemV2.")]
    public class NoEscapeV2 : HurtworldPlugin
    {
        /* SUPPORT 
         *  Discord: Kaidoz#3059
         *  Steam: https://steamcommunity.com/id/ka1doz/
         * */

        #region Data

        Dictionary<ulong, string> OnRaids = new Dictionary<ulong, string>();

        #endregion

        #region Lang

        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"RaidBlock Started", "<color=red><b>[NoEscape]</b></color> Начался рейд! У вас блок {0} минут"},
                {"Message about Timer", "<color=red><b>[NoEscape]</b></color> У вас блок на {0} секунд."},
                {"Notice about Timer","У вас блок на {0} секунд."}
            }, this, "ru");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"RaidBlock Started", "<color=red><b>[NoEscape]</b></color> Raid started! You have a block for {0} minutes."},
                {"Message about Timer", "<color=red><b>[NoEscape]</b></color> You have a block {0} seconds."},
                {"Notice about Timer","You have a block for {0} seconds."}
            }, this);
        }

        #endregion

        #region Config

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

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating new config file for NoEscapeV2...");
            _config = new Configuration()
            {
                main = new Configuration.Main()
                {
                    clanmembers = true,
                    exployactiveblock = false,
                    timec4 = 5,
                    timedrill = 7,
                    radius = 150,
                    blockIsHammer = true,
                    buildObjects = new List<string>()
                    {
                        "BedBuilder",
                        "Workbench",
                        "Titranium Workbench",
                        "Torch",
                        "Fridge",
                        "Recycler",
                        "Sign",
                        "OwnershipStake",
                        "Blast Furnace",
                        "AutomaticDrillBlue",
                        "AutomaticDrillGreen",
                        "AutomaticDrillRed"
                    }
                },
                logs = true,
                runforadmin = true,
            };
            SaveConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        Configuration _config;

        public class Configuration
        {
            [JsonProperty("Main")]
            public Main main;

            public class Main
            {
                [JsonProperty("RaidBlock time of C4")]
                public int timec4;

                [JsonProperty("RaidBlock time of RaidDrill")]
                public int timedrill;

                [JsonProperty("Radius of RaidBlock")]
                public int radius;

                [JsonProperty("Blocked build objects")]
                public List<string> buildObjects;

                [JsonProperty("Block construction hammer")]
                public bool blockIsHammer;

                [JsonProperty("RaidBlock for clan members")]
                public bool clanmembers;

                [JsonProperty("RaidBlock at Explosixe(not enabled)")]
                public bool exployactiveblock;
            }

            [JsonProperty("RaidBlock for Admins")]
            public bool runforadmin;

            [JsonProperty("Logs")]
            public bool logs;
        }

        #endregion

        #region Work Data

        void SaveData() => Interface.Oxide.DataFileSystem.WriteObject("NoEscape/players", OnRaids);

        void LoadData()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("NoEscape/players"))
                OnRaids = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, string>>("NoEscape/players");

            if (OnRaids.Count() == 0)
                return;
            for (int o = 0; o < OnRaids.Count(); o++)
            {
                try
                {
                    if (DateTime.Compare(DateTime.Parse(OnRaids.Values.ToList()[o]), DateTime.Now) < 0)
                        OnRaids.Remove(OnRaids.Keys.ToList()[o]);
                }
                catch { }
            }
            SaveData();

        }

        #endregion

        #region Hooks

        void Loaded()
        {
            LoadData();
            LoadConfig();
        }

        void OnEntitySpawned(HNetworkView data)
        {
            if (data.gameObject.name.Contains("C4"))
            {
                var sessions = GameManager.Instance?.GetSessions()?.Values.Where(IsValidSession).ToList();
                var playersNear =
                  sessions.Where(s =>
                          Vector3.Distance(s.WorldPlayerEntity.transform.position,
                              data.gameObject.transform.position) <= _config.main.radius)
                      .ToList();
                blockplayers(playersNear, _config.main.timec4);
            }

            if (data.gameObject.name.Contains("RaidDrill") && !data.gameObject.name.Contains("Item"))
            {
                var sessions = GameManager.Instance?.GetSessions()?.Values.Where(IsValidSession).ToList();
                var playersNear =
                  sessions.Where(s =>
                          Vector3.Distance(s.WorldPlayerEntity.transform.position,
                              data.gameObject.transform.position) <= _config.main.radius)
                      .ToList();
                blockplayers(playersNear, _config.main.timedrill);
            }
        }

        object OnEntityDeploy(EquipEventData entityEvent)
        {
            var session = GameManager.Instance.GetSession(entityEvent.Session.Handler.networkView.owner);

            if (!isRaid(session))
                return null;

            var data = entityEvent.Session.Handler.RefCache.PlayerConstructionManager;
            var result = (from x in _config.main.buildObjects where entityEvent.Session.RootItem.Generator.name.Contains(x) select x).Count() > 0;
            if (result || (data.IsHammer && _config.main.blockIsHammer))
            {
                Alert(string.Format(Msg("Notice about Timer", session.SteamId.ToString()), getTimeBlock(session)), session);
                return true;
            }

            return null;
        }

        #endregion

        #region Methods

        void addraid(ulong steamid, int time, bool clan = false)
        {
            DateTime blocktime = DateTime.Now.AddMinutes(time);
            if (OnRaids.ContainsKey(steamid))
                OnRaids[steamid] = blocktime.ToString();
            else
                OnRaids.Add(steamid, blocktime.ToString());
            var session = Player.FindById(steamid.ToString());
            if (session != null)
                reply(session, string.Format(Msg("RaidBlock Started", steamid.ToString()), time));
            if (_config.main.clanmembers && clan)
            {
                if (session.Identity.Clan != null)
                {
                    var clan_Members = session.Identity.Clan.GetMemebers();
                    foreach (var id in clan_Members)
                    {
                        addraid(id, time);
                    }
                }
            }
            SaveData();
        }

        void blockplayers(List<PlayerSession> playersNear, int time)
        {
            foreach (PlayerSession session in playersNear)
            {
                if (session.IsAdmin && _config.runforadmin)
                    addraid((ulong)session.SteamId, time, true);
                else
                    addraid((ulong)session.SteamId, time, true);
            }
            if (_config.logs)
                Logs(playersNear);
        }

        #endregion

        #region Mono

        #endregion

        /// <summary>
        /// Check player for raidblock
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        bool isRaid(PlayerSession session)
        {
        A:
            if (OnRaids.ContainsKey((ulong)session.SteamId))
            {
                var res = (from x in OnRaids where x.Key == (ulong)session.SteamId select x).LastOrDefault();
                var countraid = DateTime.Parse(res.Value).Subtract(DateTime.Now).TotalSeconds;
                if (countraid > 0)
                {
                    Alert(string.Format(Msg("Notice about Timer", session.SteamId.ToString()), getTimeBlock(session)), session);
                    return true;
                }
                else
                {
                    OnRaids.Remove(res.Key);
                    SaveData();
                    goto A;
                }
            }

            return false;
        }

        #region Block Kits and TP

        /// <summary>
        /// Block for ExtTeleport
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        object canExtTeleport(PlayerSession session)
        {
            if (isRaid(session))
            {
                return true;
            }
            return null;
        }


        /// <summary>
        /// Block for Kits
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        object canRedeemKit(PlayerSession session)
        {
            if (isRaid(session))
            {
                return true;
            }
            return null;
        }

        #endregion

        #region Helpers

        int getTimeBlock(PlayerSession session)
        {
            var res = (from x in OnRaids where x.Key == (ulong)session.SteamId select x).LastOrDefault();
            var countraid = DateTime.Parse(res.Value).Subtract(DateTime.Now).TotalSeconds;
            return Convert.ToInt32(countraid);
        }

        void Alert(string msg, PlayerSession session) => AlertManager.Instance.GenericTextNotificationServer(msg, session.Player);

        string Msg(string msg, string SteamId = null) => lang.GetMessage(msg, this, SteamId);

        void reply(PlayerSession session, string msg) => hurt.SendChatMessage(session, null, msg);

        /// <summary>
        /// Logs
        /// </summary>
        /// <param name="sess"></param>
        void Logs(List<PlayerSession> sess)
        {
            string prefix = "{DarkPluginsID}";
            string msg = "[" + DateTime.Now + "]" + "Players are Raid Blocked:\n";
            foreach (var s in sess)
            {
                msg += s.Identity.Name + $" ({s.SteamId})" + "\n";
            }
            LogToFile("NoEscape", msg, this);
        }

        public bool IsValidSession(PlayerSession session)
        {
            return session?.SteamId != null && session.IsLoaded && session.Identity.Name != null && session.Identity != null &&
                   session.WorldPlayerEntity?.transform?.position != null;
        }

        #endregion
    }
}
