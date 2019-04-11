using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("NoLagg", "Kaidoz | vk.com/kaidoz", "1.2.1")]
    [Description("Система отслежки лагов сервера и защита от спаммеров")]

    class NoLagg : HurtworldPlugin
    {
        #region Lang

        private void LoadDefaultMessages()
        {
            var messages = new Dictionary<string, string>
            {
                {"Prefix", "<color=#21CEFA>[NoLagg]</color>"},
                {"MessageWarring", "{prefix} Player {0} spamming items"},
                {"MessageKick","You has been kicked for spamming items"},
                {"GetFPS","{prefix} FPS server is {0}"}
            };

            lang.RegisterMessages(messages, this);
        }

        #endregion


        #region Config

        public class Configuration
        {
            [JsonProperty("Optimization")]
            public Optimization optimization = new Optimization();

            public class Optimization
            {
                [JsonProperty("Count items")]
                public int countitems { get; set; } = 60;

                [JsonProperty("Disable physics for items then items world more than 'count items'")]
                public bool disablephysics { get; set; } = true;
            }

            [JsonProperty("Protector")]
            public Protector protector = new Protector();

            public class Protector
            {
                [JsonProperty("Detect spammer items")]
                public bool detectspammer { get; set; } = true;

                [JsonProperty("Count items for detect spammer")]
                public int detectcount { get; set; } = 40;

                [JsonProperty("Kick spammer items")]
                public bool kickspammer { get; set; } = true;

                [JsonProperty("Send warning all admins and user exists permission")]
                public bool msgmod { get; set; } = true;
            }

            [JsonProperty("Critical Optimization")]
            public CriticalOptmization criticalOptmization = new CriticalOptmization();

            public class CriticalOptmization
            {
                [JsonProperty("Clear all items in world if items more than critical count")]
                public bool clearitems { get; set; } = true;

                [JsonProperty("Critical count")]
                public int criticalcount { get; set; } = 200;
            }

        }

        Configuration _config;

        protected override void SaveConfig() => Config.WriteObject(_config);

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating config file for NoLagg...");
            _config = new Configuration();
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


        #region Hooks

        void Loaded()
        {
            LoadConfig();
            if (!permission.PermissionExists(this.Name.ToLower() + ".moder"))
                permission.RegisterPermission(this.Name.ToLower() + ".moder", this);
        }

        void OnEntitySpawned(HNetworkManager data)
        {
            if (trywordlitem(data.gameObject))
            {
                List<GameObject> objs = null;
                if (_config.optimization.disablephysics || _config.protector.detectspammer || _config.criticalOptmization.clearitems)
                    objs = getWorldItems();
                GameObject obj = data.gameObject;
                if (_config.optimization.disablephysics)
                {
                    if (objs.Count() >= _config.optimization.countitems)
                    {
                        OpmizationObject(obj);
                    }
                }
                else
                    OpmizationObject(obj);

                if (_config.criticalOptmization.clearitems)
                {
                    if (objs.Count() >= _config.criticalOptmization.criticalcount)
                    {
                        clearWorldItems(objs);
                    }
                }
                if (objs.Count() >= _config.protector.detectcount)
                {
                    CheckAllPlayer(objs);
                }
            }
        }

        #endregion

        #region Main

        [ChatCommand("fps")]
        void checkfps(PlayerSession session)
        {
            if (!(session.IsAdmin || permission.UserHasPermission(session.SteamId.ToString(), this.Name.ToLower() + ".moder")))
            {
                hurt.SendChatMessage(session, null, "У вас нет прав");
                return;
            }

            float d = getFPS();
            hurt.SendChatMessage(session, null, string.Format(Msg("GetFPS", session.SteamId.ToString()), d));
        }

        void CheckAllPlayer(List<GameObject> gmj)
        {
            List<PlayerSession> players = Singleton<GameManager>.Instance.GetSessions()?.Values.Where(IsValidSession).ToList();
            foreach (var ses in players)
            {
                CheckPlayer(ses, gmj);
            }
        }

        void CheckPlayer(PlayerSession session, List<GameObject> gmj)
        {
            List<GameObject> ad = new List<GameObject>();

            if (gmj.Count == 0)
                return;
            for (int a = 0; a < gmj.Count; a++)
            {
                float d = Vector3.Distance(gmj[a].transform.position, session.WorldPlayerEntity.transform.position);
                if (d <= 30)
                {
                    ad.Add(gmj[a]);
                }
            }
            if (ad.Count >= _config.protector.detectcount)
            {
                if (_config.protector.msgmod)
                    SendWarring(string.Format(Msg("MessageWarring", null), session.Identity.Name));
                if (_config.protector.kickspammer)
                {
                    Log("Protector", $"[{session.SteamId.ToString()}] " + Msg("MessageKick", session.SteamId.ToString()));
                    Singleton<GameManager>.Instance.KickPlayer(session.SteamId.ToString(), Msg("MessageKick", session.SteamId.ToString()));
                }
            }
        }

        #endregion

        #region MonoBehavior

        public class Optimizator : MonoBehaviour
        {
            List<string> exclude = new List<string>()
            {
                "WorldItem"
            };

            bool check(string name)
            {
                foreach (string d in exclude)
                    if (name.Contains(d))
                        return false;
                return true;
            }

            private void OnCollisionEnter(Collision other)
            {
                if (getDistance() > 0.2f && check(other.gameObject.name))
                    return;

                var rigidbody = gameObject.GetComponent<Rigidbody>();
                rigidbody.useGravity = false;
                rigidbody.isKinematic = true;
                rigidbody.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionZ;
                Destroy(this);
            }

            float getDistance()
            {
                Vector3 position = gameObject.transform.position;
                RaycastHit hitInfo;

                if (Physics.Raycast(position, Vector3.down, out hitInfo))
                {
                    float dist = Vector3.Distance(position, hitInfo.point);
                    return dist;
                }
                return 0f;
            }

            public void Destroy() => Destroy(this);
        }

        #endregion


        #region Helper

        string Msg(string msg, string steamid) => lang.GetMessage(msg, this, steamid).Replace("{prefix}", lang.GetMessage("Prefix", this, steamid));

        void SendWarring(string c)
        {
            foreach (var tplayer in getPlayerList())
            {
                if (tplayer.IsAdmin || permission.UserHasPermission(tplayer.SteamId.ToString(), this.Name.ToLower() + ".moder"))
                    hurt.SendChatMessage(tplayer, null, c);
            }
        }

        void OpmizationObject(GameObject obj)
        {
            // try
            // {
            if (obj.GetComponent<Optimizator>() != null)
                obj.GetComponent<Optimizator>().Destroy();

            obj.AddComponent<Optimizator>();
            // }
            //catch { }

        }


        /// <summary>
        /// Get a fps server
        /// </summary>
        /// <returns></returns>
        int getFPS() => Convert.ToInt32(DumpFPS.Instance.GetFPS());

        /// <summary>
        /// Clear world items,but no around players
        /// </summary>
        /// <param name="objects"></param>
        void clearWorldItems(List<GameObject> objects)
        {
            int count = objects.Count();
            int counta = 0;
            foreach (GameObject obj in objects)
            {
                if (trywordlitem(obj))
                {
                    if (CheckDistance(obj, 10))
                    {
                        DestroyObject(obj);
                        counta++;
                    }

                }
            }
            Log("Critical Optimization", $"Clear world items! Before: {count}. After: {counta}.");
        }

        /// <summary>
        /// Get a count all WorldItems in world
        /// </summary>
        /// <returns></returns>
        List<GameObject> getWorldItems()
        {
            List<GameObject> objs = new List<GameObject>();

            foreach (GameObject obj in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (trywordlitem(obj))
                {
                    objs.Add(obj);
                }
            }

            return objs;
        }

        bool trywordlitem(GameObject obj)
        {
            if (obj.name.Contains("WorldItem(Clone)") || obj.name.Contains("WorldItemOre"))
                return true;

            return false;
        }

        bool CheckDistance(GameObject obj, float range)
        {
            foreach (var session in getPlayerList())
            {
                if (Vector3.Distance(obj.transform.position, session.WorldPlayerEntity.transform.position) <= range)
                    return false;
            }

            return true;
        }

        void DestroyObject(GameObject gameObject)
        {
            var nView = uLink.NetworkView.Get(gameObject);

            if (nView != null && nView.isActiveAndEnabled)
            {
                Singleton<HNetworkManager>.Instance.NetDestroy(nView.HNetworkView());
            }
        }

        List<PlayerSession> getPlayerList() => GameManager.Instance.GetSessions().Values.ToList();

        private void Log(string filename, string text)
        {
            string time = DateTime.Now.ToString("hh:mm");
            LogToFile(filename, $"[{time}]" + " " + text, this);
        }

        public bool IsValidSession(PlayerSession session)
        {
            return session?.SteamId != null && session.IsLoaded && session.Identity.Name != null && session.Identity != null &&
            session.WorldPlayerEntity?.transform?.position != null;
        }

        #endregion
    }
}
