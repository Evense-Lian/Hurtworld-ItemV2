using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using Newtonsoft.Json;
using Oxide.Core;
using System.Text.RegularExpressions;

namespace Oxide.Plugins
{
    [Info("Running Man", "Kaidoz", "1.3.0")]
    [Description("Running Man for HurtWorld")]

    class RunningMan : HurtworldPlugin
    {

        private PlayerSession runningman = null;
        private Timer ts;
        Dictionary<ulong, Timer> notice_r = new Dictionary<ulong, Timer>();
        
        GameObject markObj;
        MapMarkerData mark;

        Configuration _config;

        public class Configuration
        {
            [JsonProperty("List rewards")]
            public List<Item> items;

            public class Item
            {
                public string ItemName;

                public int Count;
            }

            [JsonProperty("Random reward(1 item) or all rewards")]
            public bool random = true;

            [JsonProperty("Event time(min.)")]
            public int timeRun = 10;

            [JsonProperty("Repeat event(min.)")]
            public int timeRepeat = 60;

            [JsonProperty("Minimum online")]
            public int minOnline = 6;

            [JsonProperty("Noticer")]
            public bool runNotice = true;

            [JsonProperty("Marker")]
            public Marker marker = new Marker();

            public class Marker
            {
                [JsonProperty("Enabled")]
                public bool enabled = true;

                [JsonProperty("Show in compass")]
                public bool compas = true;

                [JsonProperty("Scale")]
                public float scale = 100;

                [JsonProperty("Color")]
                public string color = "#EF4265";

                [JsonProperty("Label for Marker")]
                public string label = "Runner";
            }
        }

        protected override void LoadDefaultConfig()
        {
            _config = new Configuration()
            {
                items = new List<Configuration.Item>()
                {
                    new Configuration.Item()
                    {
                       ItemName = "Amber",
                       Count = 10
                    }
                }
            };
        }

        void Unload()
        {
            RunnerStop(null, true);
        }

        private void Loaded()
        {
            LoadConfig();
            LoadMessages();
            Timer();
            markObj = Resources.FindObjectsOfTypeAll<GameObject>().Where(x=> x.name.Contains("CircleMarker")).First();

            if (!permission.PermissionExists(this.Name.ToLower() + ".admin"))
                permission.RegisterPermission(this.Name.ToLower() + ".admin", this);
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

        protected override void SaveConfig() => Config.WriteObject(_config);

        private void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
                {
                    {"msg_stayrunner","<color=#95E447>[RUN]</color> Player {0} has become a running man, you have {1} minutes to find and kill him \nFind out where running /run"},
                    {"msg_newrunner","<color=#95E447>[RUN]</color> {newrunner} stole status runner {oldrunner}!"},
                    {"msg_killrunner","<color=#95E447>[RUN]</color> {killer} killed {runner} and received a bonus!"},
                    {"msg_stoprunner","<color=#95E447>[RUN]</color> Player {runner} survived and take prize"},
                    {"msg_dissrunner","<color=#95E447>[RUN]</color> Runner {name} disconnected,event stopped"},
                    {"msg_irunner","<color=#95E447>[RUN]</color> You became runner! Survive and take prize!"},
                    {"msg_dstnrunner","<color=#95E447>[RUN]</color> You are {0} m. from runner. !"},
                    {"notice_dstnrunner","You {0} m. from runner"},
                    {"msg_notrunner","<color=#95E447>[RUN]</color> Runner not exists!"},
                    {"msg_lgrunner","<color=#95E447>[RUN]</color> Player {0} survived and take prize"},
                    {"msg_loserunner","<color=#95E447>[RUN]</color> Runner is gone!"},
                    {"msg_deathhrunner","<color=#95E447>[RUN]</color> Runner killed!"},
                    {"msg_nrrunner","<color=#95E447>[RUN]</color> You're close with the Runner!"},
                    {"msg_stopnotice","<color=#95E447>[RUN]</color> Noticer stopped!"},
                }, this);

            lang.RegisterMessages(new Dictionary<string, string>
                {
                    {"msg_stayrunner","<color=#95E447>[RUN]</color> Игрок {0} стал бегущим человеком, у вас есть {1} минут, найти и убить его \nУзнать где бегущий /run"},
                    {"msg_newrunner","<color=#95E447>[RUN]</color> {newrunner} забрал статус бегущего у {oldrunner}!"},
                    {"msg_killrunner","<color=#95E447>[RUN]</color> {killer} убил бегущего {runner} и получил бонус!"},
                    {"msg_stoprunner","<color=#95E447>[RUN]</color> Игрок {0} продержался и получил бонус"},
                    {"msg_dissrunner","<color=#95E447>[RUN]</color> Бегущий игрок {name} вышел,ивент окончен"},
                    {"msg_irunner","<color=#95E447>[RUN]</color> Ты стал бегущим человеком! Продержись и получи бонус!"},
                    {"msg_dstnrunner","<color=#95E447>[RUN]</color> До бегущего {0}м. !"},
                    {"notice_dstnrunner","До бегущего {0}м. "},
                    {"msg_notrunner","<color=#95E447>[RUN]</color> Бегущего человека нет!"},
                    {"msg_lgrunner","<color=#95E447>[RUN]</color> Игрок {0} продержался и получил бонус"},
                    {"msg_loserunner","<color=#95E447>[RUN]</color> Бегущий пропал!"},
                    {"msg_deathhrunner","<color=#95E447>[RUN]</color> Бегущий погиб!"},
                    {"msg_nrrunner","<color=#95E447>[RUN]</color> Вы сблизились с бегущим!"},
                    {"msg_stopnotice","<color=#95E447>[RUN]</color> Отображение отключено!"},
                }, this, "ru");
        }

        object OnPlayerSuicide(PlayerSession session)
        {
            if (session == runningman)
            {
                return true;
            }
            return null;
        }

        object canExtTeleport(PlayerSession session)
        {
            if (session == runningman)
            {
                return true;
            }
            return null;
        }

        void Timer()
        {
            timer.Repeat(_config.timeRepeat * 60f, 0, () =>
            {
                var allPlayers = GameManager.Instance.GetSessions().Values.ToList();

                if (allPlayers.Count() >= _config.minOnline)
                    RunnerStart();
            });
        }

        private void OnPlayerDisconnected(PlayerSession session)
        {
            if (notice_r.ContainsKey((ulong)session.SteamId))
            {
                notice_r[(ulong)session.SteamId].Destroy();
                notice_r.Remove((ulong)session.SteamId);
            }

            if (runningman == null)
                return;

            string runname = string.Empty;
            runname = runningman.Identity.Name;
            if (runningman == session)
            {
                Server.Broadcast((lang.GetMessage("msg_dissrunner", this).Replace("{name}", runningman.Identity.Name)));
                runningman = null;
            }
        }

        void RunnerStart(PlayerSession target = null)
        {
            if (target == null)
            {
                var allPlayers = GameManager.Instance.GetSessions().Values.ToList();
                int randomInt = UnityEngine.Random.Range(0, allPlayers.Count() - 1);
                target = allPlayers[randomInt];
            }

            if (runningman != null)
                Server.Broadcast(lang.GetMessage("msg_newrunner", this).Replace("{oldrunner}", runningman.Identity.Name).Replace("{newrunner}", target.Identity.Name));

            runningman = target;
            runningman.WorldPlayerEntity.gameObject.GetComponent<RunnerMan>().TryDestroy();
            runningman.WorldPlayerEntity.gameObject.AddComponent<RunnerMan>();
        }

        [ChatCommand("run.admin")]
        void cmdadmin(PlayerSession session, string command, string[] args)
        {
            if (!perm(session))
            {
                Reply(session, "You have no rights");
                return;
            }
            if (args.Length == 0)
            {
                Reply(session, "          <color=#95E447>[RUN]</color> ADMIN\n/run.admin start - start event\n/run.admin stop - stop event\n/run.admin target nickname - start event with target player");
                return;
            }
            switch (args[0].ToLower())
            {
                case "start":
                    RunnerStart();
                    break;
                case "stop":
                    RunnerStop(null, true);
                    break;
                case "target":
                    if (args.Length == 2)
                    {
                        string name = find(null, args[1]);
                        if (name != null)
                            RunnerStart(getSession(name));
                        else
                            Reply(session, "<color=#95E447>[RUN]</color> No found");
                        return;
                    }
                    Reply(session, "<color=#95E447>[RUN]</color> Syntax: /run.admin target name");
                    break;
                default:
                    Reply(session, "          <color=#95E447>[RUN]</color> ADMIN\n/run.admin start - start event\n/run.admin stop - stop event\n/run.admin target nickname - start event with target player");
                    break;
            }
        }

        void RunnerStop(PlayerSession session, bool admin = false)
        {
            if (session != null)
                give(session);
            if (admin)
                Server.Broadcast("<color=#95E447>[RUN]</color> Event stopped!");
            if (!ts.Destroyed)
                ts.Destroy();
            if(mark!=null)
            {
                try
                {
                    MapManagerServer.Instance.DeregisterMarker(mark);
                }catch{}
            }
                

            if(runningman.WorldPlayerEntity!=null)
            {
                var rn = runningman.WorldPlayerEntity.GetComponent<RunnerMan>();
                OnUnRegisterRunner();
                rn.TryDestroy();
            }
            runningman = null;
            cTimer = 0;
        }

        [ChatCommand("run")]
        void dsnrun(PlayerSession session)
        {
            if (runningman == null)
            {
                Reply(session, (lang.GetMessage("msg_notrunner", this)));
                return;
            }
            else
            {
                Vector3 xr = runningman.WorldPlayerEntity.transform.position;
                Vector3 xk = session.WorldPlayerEntity.transform.position;
                float gr = Vector3.Distance(xk, xr);
                Reply(session, (lang.GetMessage("msg_dstnrunner", this).Replace("{0}", Convert.ToString(Math.Round(gr)))));
                if (_config.runNotice)
                {
                    if (notice_r.ContainsKey((ulong)session.SteamId) && notice_r[(ulong)session.SteamId] != null && !notice_r[(ulong)session.SteamId].Destroyed)
                    {
                        notice_r[(ulong)session.SteamId].Destroy();
                        Reply(session, lang.GetMessage("msg_stopnotice", this));
                        return;
                    }
                    if (!notice_r.ContainsKey((ulong)session.SteamId))
                        notice_r.Add((ulong)session.SteamId, null);

                    notice_r[(ulong)session.SteamId] = timer.Repeat(1f, 0, () =>
                    {
                        if (runningman == null)
                        {
                            Reply(session, lang.GetMessage("msg_loserunner", this));
                            notice_r[(ulong)session.SteamId].Destroy();
                            return;
                        }
                        xr = runningman.WorldPlayerEntity.transform.position;
                        xk = session.WorldPlayerEntity.transform.position;
                        gr = Vector3.Distance(xk, xr);
                        if (gr <= 2)
                        {
                            Reply(session, lang.GetMessage("msg_nrrunner", this));
                            notice_r[(ulong)session.SteamId].Destroy();
                            return;
                        }

                        Singleton<AlertManager>.Instance.GenericTextNotificationServer(((lang.GetMessage("notice_dstnrunner", this)).Replace("{0}", Convert.ToString(Math.Round(gr)))), session.Player);
                    });
                }
                else
                {
                    Reply(session, (lang.GetMessage("msg_dstnrunner", this).Replace("{0}", Convert.ToString(Math.Round(gr)))));
                }
            }
        }

        private void OnPlayerDeath(PlayerSession session, EntityEffectSourceData dataSource)
        {
            string name = string.Empty;
            name = session.Identity.Name;
            string tmpName = GetNameOfObject(dataSource.EntitySource);

            if (string.IsNullOrEmpty(tmpName))
                return;

            var KillerName = tmpName.Remove(tmpName.Length - 3);

            if (runningman == getSession(name))
            {
                var sessionKiller = getSession(KillerName);
                if (sessionKiller != null)
                {
                    Server.Broadcast(lang.GetMessage("msg_killrunner", this)
                        .Replace("{killer}", sessionKiller.Identity.Name)
                        .Replace("{runner}", session.Identity.Name));
                    RunnerStop(sessionKiller);
                }else
                Server.Broadcast(lang.GetMessage("msg_deathhrunner", this));
            }
        }

        [ConsoleCommand("rman")]
        private void ConsoleCmd(string command, string[] args)
        {
            if (args.Length == 0)
            {
                Puts("[RUN] Commands: ");
                Puts("rman stop");
                Puts("rman target");
                Puts("rman start");
                return;
            }

            switch (args[0].ToLower())
            {
                case "stop":
                    RunnerStop(null);
                    Puts("[RUN] Event stopped");
                    break;
                case "target":
                    string name = find(null, args[1]);
                    if (name != null)
                        RunnerStart(getSession(name));
                    else
                        Puts("[RUN] No found");
                    break;
                case "start":
                    RunnerStart();
                    break;
            }
        }

        int cTimer = 0;

        void OnUnRegisterRunner()
        {
            RunnerStop(null, true);
        }

        void OnRegisterRunner()
        {
            string name = runningman.Identity.Name;
            Server.Broadcast((lang.GetMessage("msg_stayrunner", this).Replace("{0}", name).Replace("{1}", Convert.ToString(_config.timeRun))));
            hurt.SendChatMessage(runningman, null, lang.GetMessage("msg_irunner", this));
            if(_config.marker.enabled)
                mark = CreateMarker(getPosition(runningman));
        }

        void OnTickRunner(GameObject obj)
        {
            if(runningman==null)
            {
                obj.GetComponent<RunnerMan>().TryDestroy();
                return;
            }
            cTimer++;
            if(_config.marker.enabled)
                RefreshMarker(mark, getPosition(runningman));
            if (cTimer >= _config.timeRun * 60)
            {
                Server.Broadcast((lang.GetMessage("msg_lgrunner", this).Replace("{0}", runningman.Identity.Name)));
                RunnerStop(runningman);
                runningman.WorldPlayerEntity.gameObject.GetComponent<RunnerMan>().TryDestroy();
                runningman = null;
            }
        }

        void RefreshMarker(MapMarkerData mark, Vector3 position)
        {
            if (Vector3.Distance(mark.Position, position) >= 10)
            {
                MapManagerServer.Instance.DeregisterMarker(mark);
                mark.Position = position;
                MapManagerServer.Instance.RegisterMarker(mark);
            }
        }

        #region Mono

        public class RunnerMan : MonoBehaviour
        {
            void Awake()
            {
                Interface.CallHook("OnRegisterRunner");
                InvokeRepeating("Repeat", 1f, 1f);
            }

            void Repeat()
            {
                Interface.CallHook("OnTickRunner", gameObject);
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

        Vector3 getPosition(PlayerSession session) => session.WorldPlayerEntity.transform.position;

        MapMarkerData CreateMarker(Vector3 position)
        {
            MapMarkerData marker = new MapMarkerData();
            marker.Prefab = markObj;
            marker.Label = _config.marker.label + "\n" + runningman.Identity.Name;
            marker.Position = position;
            marker.Scale = new Vector3(_config.marker.scale, _config.marker.scale, 0f);
            marker.Color = HexStringToColor(_config.marker.color.Replace("#", ""));
            marker.ShowInCompass = _config.marker.compas;
            marker.Global = true;
            MapManagerServer.Instance.RegisterMarker(marker);
            return marker;
        }

        public Color HexStringToColor(string hexColor)
        {
            string hc = ExtractHexDigits(hexColor);
            if (hc.Length != 6)
            {
                return Color.white;
            }
            string r = hc.Substring(0, 2);
            string g = hc.Substring(2, 2);
            string b = hc.Substring(4, 2);
            Color color = Color.white;
            try
            {
                int ri
                   = Int32.Parse(r, System.Globalization.NumberStyles.HexNumber);
                int gi
                   = Int32.Parse(g, System.Globalization.NumberStyles.HexNumber);
                int bi
                   = Int32.Parse(b, System.Globalization.NumberStyles.HexNumber);
                color = Color.HSVToRGB(ri, gi, bi);
            }
            catch
            {
                return Color.black;
            }
            return color;
        }

        public string ExtractHexDigits(string input)
        {
            // remove any characters that are not digits (like #)
            Regex isHexDigit
               = new Regex("[abcdefABCDEF\\d]+", RegexOptions.Compiled);
            string newnum = "";
            foreach (char c in input)
            {
                if (isHexDigit.IsMatch(c.ToString()))
                    newnum += c.ToString();
            }
            return newnum;
        }

        string find(PlayerSession session, string name)
        {
            foreach (var target in GameManager.Instance.GetSessions().Values)
            {
                if (name.Contains(target.Identity.Name))
                {
                    return name;
                }
                else if (name.Contains(target.SteamId.ToString()))
                {
                    return target.Identity.Name.ToString();
                }

                if (session == null)
                    Puts("[Run] Player not exists");
                else
                    Reply(session, $"[Run] Player not exists");
            }
            return null;
        }

        public bool perm(PlayerSession session) => session.IsAdmin || permission.UserHasPermission(session.SteamId.ToString(), this.Name.ToLower() + ".admin");

        void give(PlayerSession session)
        {
            string[] a;
            var ItemMgr = Singleton<GlobalItemManager>.Instance;
            string it = string.Empty;
            int cnt = 0;

            if (!_config.random)
            {
                foreach (var item in _config.items)
                {
                    it = item.ItemName;
                    cnt = item.Count;
                    ItemMgr.GiveItem(session.Player, getItemFromName(it), cnt);
                }
            }
            else
            {
                int rand = UnityEngine.Random.Range(0, _config.items.Count - 1);
                it = _config.items[rand].ItemName;
                cnt = _config.items[rand].Count;
                ItemMgr.GiveItem(session.Player, getItemFromName(it), cnt);
            }
        }

        public ItemGeneratorAsset getItemFromName(string name)
        {
            foreach (var it in GlobalItemManager.Instance.GetGenerators())
                if (name==it.Value.name)
                    return GlobalItemManager.Instance.GetGenerators()[it.Value.GeneratorId];

            return GlobalItemManager.Instance.GetGenerators()[1];
        }

        void Reply(PlayerSession session, string msg) => hurt.SendChatMessage(session, null, msg);

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
                if (i.Value.Identity.Name.ToLower().Contains(identifier.ToLower()) || identifier.Equals(i.Value.SteamId.ToString()))
                {
                    session = i.Value;
                    break;
                }
            }

            return session;
        }

        #endregion
    }
}