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

namespace Oxide.Plugins
{
    [Info("ClansRewards", "Kaidoz", "1.0.3")]
    class ClansRewards : HurtworldPlugin
    {

        [PluginReference]
        private Plugin EconPlus;

        public class Configuration
        {
            [JsonProperty("Clans Wars")]
            public ClansWars clansWars;

            public class ClansWars
            {
                [JsonProperty("Take balance for enemy clan member")]
                public bool takeBalance;

                [JsonProperty("Count balance")]
                public float count;

                [JsonProperty("For killing a member default multiple")]
                public float defaultm;

                [JsonProperty("For killing a member officer multiple")]
                public float officerm;

                [JsonProperty("For killing a member creator multiple")]
                public float creatorm;
            }

            [JsonProperty("Clans Capture")]
            public ClansCapture clansT;
            public class ClansCapture
            {
                [JsonProperty("Totem after reduced money")]
                public int maxTotems;

                [JsonProperty("TimeRewards")]
                public List<TimeReward> timerewards;

                public class TimeReward
                {
                    [JsonProperty("Time")]
                    public string time;

                    [JsonProperty("Reward")]
                    public float reward;

                    public TimeReward(string time, float reward)
                    {
                        this.time = time;
                        this.reward = reward;
                    }
                }
            }
        }

        #region Config
        Configuration _config;

        protected override void SaveConfig() => Config.WriteObject(_config);

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating new config file for ClansRewards...");
            _config = new Configuration()
            {
                clansT = new Configuration.ClansCapture()
                {
                    maxTotems = 3,
                    timerewards = new List<Configuration.ClansCapture.TimeReward>()
                    {
                        new Configuration.ClansCapture.TimeReward("00", 1f),
                        new Configuration.ClansCapture.TimeReward("01", 0.5f),
                        new Configuration.ClansCapture.TimeReward("02", 0.3f),
                        new Configuration.ClansCapture.TimeReward("03", 0.4f),
                        new Configuration.ClansCapture.TimeReward("04", 0.5f),
                        new Configuration.ClansCapture.TimeReward("05", 0.5f),
                        new Configuration.ClansCapture.TimeReward("06", 0.5f),
                        new Configuration.ClansCapture.TimeReward("07", 0.6f),
                        new Configuration.ClansCapture.TimeReward("08", 0.6f),
                        new Configuration.ClansCapture.TimeReward("09", 0.6f),
                        new Configuration.ClansCapture.TimeReward("10", 0.7f),
                        new Configuration.ClansCapture.TimeReward("11", 0.75f),
                        new Configuration.ClansCapture.TimeReward("12", 0.8f),
                        new Configuration.ClansCapture.TimeReward("13", 0.9f),
                        new Configuration.ClansCapture.TimeReward("14", 0.95f),
                        new Configuration.ClansCapture.TimeReward("15", 1f),
                        new Configuration.ClansCapture.TimeReward("16", 1.15f),
                        new Configuration.ClansCapture.TimeReward("17", 1.2f),
                        new Configuration.ClansCapture.TimeReward("18", 1.4f),
                        new Configuration.ClansCapture.TimeReward("19", 1.4f),
                        new Configuration.ClansCapture.TimeReward("20", 1.5f),
                        new Configuration.ClansCapture.TimeReward("21", 1.5f),
                        new Configuration.ClansCapture.TimeReward("22", 1.5f),
                        new Configuration.ClansCapture.TimeReward("23", 1.45f),
                        new Configuration.ClansCapture.TimeReward("24", 1.40f),
                    }
                }

            };
            SaveConfig();
        }

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

        #endregion

        [HookMethod("Unload")]
        void Unload()
        {
            UnDoTerritoryMarkers();
        }

        [HookMethod("Loaded")]
        void Loaded()
        {
            EconPlus = plugins.Find("EconPlus");
            LoadConfig();
            DoTerritoryMarkers();
        }

        Dictionary<Clan, float> objStakes = new Dictionary<Clan, float>();

        void UnDoTerritoryMarkers()
        {
            var listTerritotyMarkers = Resources.FindObjectsOfTypeAll<OwnershipStakeServer>()?
            .Where(
                e =>
                    e.IsClanTotem)
            .ToList() ?? new List<OwnershipStakeServer>();
            foreach (var obj in listTerritotyMarkers)
            {
                if (obj.gameObject.GetComponent<ClanReward>() != null)
                    obj.gameObject.GetComponent<ClanReward>().TryDestroy();
            }
        }

        void DoTerritoryMarkers()
        {
            var listTerritotyMarkers = Resources.FindObjectsOfTypeAll<OwnershipStakeServer>()?
            .Where(
                e =>
                    e.IsClanTotem)
            .ToList() ?? new List<OwnershipStakeServer>();
            foreach (var obj in listTerritotyMarkers)
            {
                if (obj.gameObject.GetComponent<ClanReward>() == null)
                    obj.gameObject.AddComponent<ClanReward>();
                if (obj.AuthorizedClans.Count() != 0)
                {
                    if (!objStakes.ContainsKey(obj.AuthorizedClans.First()))
                        objStakes.Add(obj.AuthorizedClans.First(), 1f);
                    else
                        objStakes[obj.AuthorizedClans.First()]++;
                }
            }
        }

        [HookMethod("OnPlayerAuthorize")]
        void OnPlayerAuthorize(PlayerSession session, GameObject obj, OwnershipStakeServer stake)
        {
            if (!stake.IsClanTotem)
                return;

            if (stake.AuthorizedClans.Count() != 0)
            {
                var oldClan = stake.AuthorizedClans.First();
                if (objStakes.ContainsKey(oldClan))
                    objStakes[oldClan]--;
            }

            var newClan = session.Identity.Clan;
            if (objStakes.ContainsKey(newClan))
                objStakes[newClan]++;
            else
                objStakes.Add(newClan, 1);
        }

        [HookMethod("OnPlayerDeath")]
        private void OnPlayerDeath(PlayerSession session, EntityEffectSourceData source)
        {
            if (!_config.clansWars.takeBalance)
                return;

            string name = session.Identity.Name;
            string tmpName = GetNameOfObject(source.EntitySource);
            if (!tmpName.EndsWith("(P)"))
                return;

            var sesskiller = getSession(tmpName.Remove(tmpName.Length - 3));
            if (sesskiller == null)
                return;

            if (session.Identity.Clan == null || sesskiller.Identity.Clan == null)
                return;


        }

        [HookMethod("OnClanReward")]
        void OnClanReward(Clan clan)
        {
            SendClanBalance(clan, GetAmountReward() *
                                                                objStakes[clan] > _config.clansT.maxTotems ?
                                                                                _config.clansT.maxTotems / (objStakes[clan] - 0.5f)
                                                                                : 1);
        }

        void SendClanBalance(Clan clan, float amount)
        {
            EconPlus.Call("GiveReward", clan, amount);
        }

        public float GetAmountReward()
        {
            string hour = string.Empty;
            hour = DateTime.Now.ToString("HH");
            try
            {
                foreach (var st in _config.clansT.timerewards)
                {
                    if (st.time == hour)
                        return st.reward;
                }
            }
            catch (Exception ex)
            {
                Puts(ex.ToString());
            }
            return 0.7f;
        }

        static System.Random rnd = new System.Random();

        #region MonoBehaviur

        public class ClanReward : MonoBehaviour
        {
            string nameClan = "{DarkPluginsId}";
            int time = 0;
            string controlClan = string.Empty;

            void Awake()
            {
                InvokeRepeating("GiveRewad", 1f, 1f);
            }

            void GiveRewad()
            {
                OwnershipStakeServer stake = GetComponent<OwnershipStakeServer>();
                if (stake.AuthorizedClans == null || stake.AuthorizedClans.Count() == 0)
                    return;

                Clan clan = gameObject.GetComponent<OwnershipStakeServer>().AuthorizedClans.FirstOrDefault();
                time++;
                if (controlClan != clan.ClanGuid)
                {
                    controlClan = clan.ClanGuid;
                    time = 0;
                }

                if (time >= 60)
                {
                    time = 0;
                    Interface.CallHook("OnClanReward", clan);
                }
            }
        }

        #endregion

        private PlayerSession getSession(string identifier)
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

        string GetNameOfObject(UnityEngine.GameObject obj)
        {
            var ManagerInstance = GameManager.Instance;
            return ManagerInstance.GetDescriptionKey(obj);
        }

        void Notice(PlayerSession session, string message) => Singleton<AlertManager>.Instance.GenericTextNotificationServer(message, session.Player);

        #region API

        private int getMaxTotem()
        {
            return _config.clansT.maxTotems;
        }

        #endregion

    }
}
