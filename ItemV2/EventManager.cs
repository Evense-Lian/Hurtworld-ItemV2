using Oxide.Core;
using Random = System.Random;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using Oxide.Core.Configuration;
using Newtonsoft.Json.Linq;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("EventManager", "Masno_Fest", "1.0.0")]
    [Description("EventManager")]
    class EventManager : HurtworldPlugin
    {
        static Plugin NoEscapeV2 => _plugin.plugins.Find("NoEscapeV2");

        static void Heal(PlayerSession session)
        {
            EntityStats stats = session.WorldPlayerEntity.GetComponent<EntityStats>();
            stats.GetFluidEffect(effect().TemperatureDelta).Reset(true);
            stats.GetFluidEffect(effect().InternalTemperature).Reset(true);
            stats.GetFluidEffect(effect().ExternalTemperature).Reset(true);
            stats.GetFluidEffect(effect().Toxin).SetValue(0f);
            stats.GetFluidEffect(effect().Health).SetValue(100f);
            foreach (var d in stats.GetBinaryEffects())
            {
                d.Value.Remove();
            }
        }

        static EntityFluidEffectKeyDatabase effect() => Singleton<RuntimeAssetManager>.Instance.RefList.EntityEffectDatabase;

        static List<T> Shuffle<T>(List<T> list)
        {
            List<T> ret = list.ToList();
            int n = ret.Count();
            while (n > 1)
            {
                n--;
                int k = Core.Random.Range(0, n);
                T value = ret[k];
                ret[k] = ret[n];
                ret[n] = value;
            }
            return ret;
        }

        static string MSG_REG_Open = "<color=lime>[EVENT]</color> Event <color=cyan>{event}</color> otwarty! Aby się zapisać napisz: /event reg <color=yellow>[0/{max}]</color>";
        static string MSG_REG_All = "<color=lime>[EVENT]</color> Gracz {name} zapisał się na event {event} <color=yellow>[{players}/{max}]</color>";
        static string MSG_REG_One = "<color=lime>[EVENT]</color> Zapisałeś się na event <color=cyan>{event}</color>!";
        static string MSG_Cant_REG_One = "<color=red>[EVENT]</color> Już się zapisałeś na event <color=cyan>{event}</color>!";
        static string MSG_Cant_REG_DIE = "<color=red>[EVENT]</color> Nie możesz się zapisać na event <color=cyan>{event}</color>, ponieważ zginąłeś!";
        static string MSG_Cant_REG_RaidBlock = "<color=red>[EVENT]</color> Nie możesz się zapisać na event <color=cyan>{event}</color>, RaidBlock!";

        static string MSG_UNREG_All = "<color=red>[EVENT]</color> Gracz <color=#FF6347>{name}</color> wypisał się z eventu {event} <color=yellow>[{players}/{max}]</color>";
        static string MSG_UNREG_One = "<color=red>[EVENT]</color> Wypisałeś się z eventu {event}!";
        static string MSG_UNREG_DIE = "<color=red>[EVENT]</color> Wypisano ciebie z eventu {event}, ponieważ zginąłeś!";
        static string MSG_UNREG_RaidBlock = "<color=red>[EVENT]</color> RaidBlock!";
        static string MSG_Cant_UNREG_One = "<color=red>[EVENT]</color> Nie możesz wypisać się z eventu {event}!";

        static string MSG_START = "<color=red>[EVENT]</color> Event {event} wystartował!";
        static string MSG_START_PLAYERS = "<color=red>!!!START!!!</color>";
        static string MSG_WON_PLAYERS = "<color=red>!!!WYGRAŁEŚ!!!\nOTRZYMUJESZ: {items}</color>";
        static string MSG_DIE = "<color=lime>[EVENT]</color> Gracz {name} zginął! <color=yellow>[{player}/{max}]</color>";
        static string MSG_WIN = "<color=lime>[EVENT] W evencie <color=cyan>{event}</color> wygrał/li: {players}</color>";
        static string MSG_STOP = "<color=red>[EVENT]</color> Event <color=cyan>{event}</color> zakończony!";

        static string MSG_INFO = "<color=lime>[EVENT]</color> Event <color=cyan>{event}</color> rozpocznie się za <color=yellow>{time} minut</color>\nKomenda na zapisanie się: <color=orange>/event reg</color>";
        static string MSG_INFO_CURR = "<color=red>[EVENT]</color> Event aktualnie trwa, czas będzie odliczany po zakończeniu!";
        static string MSG_INFO_MIN = "<color=lime>[EVENT]</color> Event <color=cyan>{event}</color> rozpocznie się za <color=yellow>{time} minut</color>\nKomenda na zapisanie się: <color=orange>/event reg</color>";

        static string PermAutoreg = "autoreg.event";
        static string PermReg = "reg.event";

        static EventManager _plugin = null;

        public class ItemInstance
        {
            public int slot;
            public ItemObject item;
        }

        class ConfigData
        {
            //SETTINGS
            public static float EVENT_CD { get { return GetConfig("SETTINGS", "CoolDown AutoEvent (min)", 30); } set { SetConfig(true, "SETTINGS", "CoolDown AutoEvent (min)", value); } }
            public static List<int> EVENT_TIME { get { return GetConfig("SETTINGS", "After event message time (sec)", new List<int>() { 20 * 60, 10 * 60, 5 * 60, 2 * 60, 60, 30, 15 }); } set { SetConfig(true, "SETTINGS", "After event message time (sec)", value); } }

            //PUBG
            public static int PUBG_Players { get { return GetConfig("PUBG", "Players", 10); } set { SetConfig(true, "PUBG", "Players", value); } }
            public static List<Vector3> PUBG_Positions { get { return (GetConfig("PUBG", "Positions", new List<string>()) ?? new List<string>()).Select(str => StringToVector3(str)).ToList(); } set { SetConfig(true, "PUBG", "Positions", value.Select(v3 => Vector3ToString(v3)).ToList()); } }
            public static List<ItemInfo> PUBG_Prize
            {
                get
                {
                    return GetConfig("PUBG", "Prize", new Dictionary<string, int>()
                    {
                        ["Detonator Cap"] = 1
                    }).Select(kv => new ItemInfo()
                    {
                        ID = kv.Key,
                        Count = kv.Value
                    }).ToList();
                }
                set
                {
                    SetConfig(true, "PUBG", "Prize", value.ToDictionary(i => i.ID, i => i.Count));
                }
            }
            public static Dictionary<string, Dictionary<int, ItemInfo>> PUBG_Weapons
            {
                get
                {
                    int err = 0;
                    try
                    {
                        if (_plugin.Config.Get("PUBG", "Weapons") == null)
                        {
                            var dict = new Dictionary<string, Dictionary<int, ItemInfo>>()
                            {
                                ["Bow"] = new Dictionary<int, ItemInfo>()
                                {
                                    [0] = new ItemInfo("Bow T1", 1),
                                    [1] = new ItemInfo("Arrow", 50),
                                },
                                ["AR"] = new Dictionary<int, ItemInfo>()
                                {
                                    [0] = new ItemInfo("AR15", 1),
                                    [1] = new ItemInfo("556 Bullet", 50),
                                },
                                ["Pistol"] = new Dictionary<int, ItemInfo>()
                                {
                                    [0] = new ItemInfo("Beretta M9", 1),
                                    [1] = new ItemInfo("9mm Bullet", 50),
                                }
                            };
                            err++;
                            SetConfig(false, "PUBG", "Weapons", dict);
                            return _plugin.Config.Get("PUBG", "Weapons") as Dictionary<string, Dictionary<int, ItemInfo>>;
                        }
                        try
                        {
                            err++;
                            return _plugin.Config.Get("PUBG", "Weapons") as Dictionary<string, Dictionary<int, ItemInfo>>;
                        }
                        catch
                        {
                            err++;
                            Dictionary<string, Dictionary<int, ItemInfo>> ret = new Dictionary<string, Dictionary<int, ItemInfo>>();
                            foreach (var kv in _plugin.Config.Get("PUBG", "Weapons") as Dictionary<string, Dictionary<int, ItemInfo>>)
                            {
                                Dictionary<int, ItemInfo> items = new Dictionary<int, ItemInfo>();
                                foreach (var slot in kv.Value)
                                {
                                    string ItemID = slot.Value.ID;
                                    int Count = slot.Value.Count;
                                    items.Add(slot.Key, new ItemInfo(ItemID, Count));
                                }
                                ret.Add(kv.Key, items);
                            }
                            err++;
                            return ret;
                            
                        }
                    }
                    catch(Exception e)
                    {
                        Interface.GetMod().LogDebug("err: {0}{1}{2}{3}", e.Message, Environment.NewLine, e.StackTrace,err);
                    }
                    return null;
                }
                set { SetConfig(false, "PUBG", "Weapons", value); }
            }
            public static Vector3 PUBG_Pos1 { get { return StringToVector3(GetConfig("PUBG", "Pos1", "0 0 0")); } set { SetConfig(true, "PUBG", "Pos1", Vector3ToString(value)); } }
            public static Vector3 PUBG_Pos2 { get { return StringToVector3(GetConfig("PUBG", "Pos2", "0 0 0")); } set { SetConfig(true, "PUBG", "Pos2", Vector3ToString(value)); } }

            //CARS
            public static int CARS_Players { get { return GetConfig("CARS", "Players", 10); } set { SetConfig(true, "CARS", "Players", value); } }
            public static Dictionary<Vector3, CAR_PosData> CARS_Positions
            {
                get
                {
                    if (_plugin.Config.Get("CARS", "Positions") == null)
                    {
                        SetConfig(false, "CARS", "Positions", new Dictionary<string, CAR_PosData>());
                        return new Dictionary<Vector3, CAR_PosData>();
                    }
                    try
                    {
                        return _plugin.Config.Get<Dictionary<string, CAR_PosData>>("CARS", "Positions").ToDictionary(kv => StringToVector3(kv.Key), kv => kv.Value);
                    }
                    catch
                    {
                        return (_plugin.Config.Get("CARS", "Positions") as Dictionary<string, object>).ToDictionary(kv => StringToVector3(kv.Key), kv => CAR_PosData.Create(kv.Value as Dictionary<string, object>));
                    }
                    //return (GetConfig("CARS", "Positions", (object)new Dictionary<string, CAR_PosData>()) as Dictionary<string, CAR_PosData>).ToDictionary(kv => StringToVector3(kv.Key), kv => kv.Value);
                }
                set
                {
                    SetConfig(true, "CARS", "Positions", value.ToDictionary(kv => Vector3ToString(kv.Key), kv => kv.Value));
                }
            }
            public static List<ItemInfo> CARS_Prize
            {
                get
                {
                    return GetConfig("CARS", "Prize", new Dictionary<string, int>() { ["Detonator Cap"] = 1 }).Select(kv => new ItemInfo()
                    {
                        ID = kv.Key,
                        Count = kv.Value
                    }).ToList();
                }
                set
                {
                    SetConfig(true, "CARS", "Prize", value.ToDictionary(i => i.ID, i => i.Count));
                }
            }
            /*public static Dictionary<string, Dictionary<int, ItemInfo>> CARS_Weapons
            {
                get
                {
                    if (_plugin.Config.Get("CARS", "Weapons") == null)
                    {
                        SetConfig(false, "CARS", "Weapons", new Dictionary<string, Dictionary<int, ItemInfo>>() { ["Bow"] = new Dictionary<int, ItemInfo>() { [0] = new ItemInfo(47, 1), [1] = new ItemInfo(48, 50), }, ["AR"] = new Dictionary<int, ItemInfo>() { [0] = new ItemInfo(98, 1), [1] = new ItemInfo(278, 50), }, ["Pistol"] = new Dictionary<int, ItemInfo>() { [0] = new ItemInfo(279, 1), [1] = new ItemInfo(280, 50), } });
                        return new Dictionary<string, Dictionary<int, ItemInfo>>() { ["Bow"] = new Dictionary<int, ItemInfo>() { [0] = new ItemInfo(47, 1), [1] = new ItemInfo(48, 50), }, ["AR"] = new Dictionary<int, ItemInfo>() { [0] = new ItemInfo(98, 1), [1] = new ItemInfo(278, 50), }, ["Pistol"] = new Dictionary<int, ItemInfo>() { [0] = new ItemInfo(279, 1), [1] = new ItemInfo(280, 50), } };
                    }
                    try
                    {
                        return _plugin.Config.Get<Dictionary<string, Dictionary<int, ItemInfo>>>("CARS", "Weapons");
                    }
                    catch
                    {
                        Dictionary<string, Dictionary<int, ItemInfo>> ret = new Dictionary<string, Dictionary<int, ItemInfo>>();
                        foreach (var kv in _plugin.Config.Get("CARS", "Weapons") as Dictionary<string, object>)
                        {
                            Dictionary<int, ItemInfo> items = new Dictionary<int, ItemInfo>();
                            foreach (var slot in kv.Value as Dictionary<string, object>)
                            {
                                int ItemID = int.Parse((slot.Value as Dictionary<string, object>)["ItemID"].ToString());
                                int Count = int.Parse((slot.Value as Dictionary<string, object>)["Count"].ToString());
                                items.Add(int.Parse(slot.Key), new ItemInfo(ItemID, Count));
                            }
                            ret.Add(kv.Key, items);
                        }
                        return ret;
                    }
                }
                set { SetConfig(true, "CARS", "Weapons", value); }
            }*/
            public static Vector3 CARS_Pos1 { get { return StringToVector3(GetConfig("CARS", "Pos1", "0 0 0")); } set { SetConfig(true, "CARS", "Pos1", Vector3ToString(value)); } }
            public static Vector3 CARS_Pos2 { get { return StringToVector3(GetConfig("CARS", "Pos2", "0 0 0")); } set { SetConfig(true, "CARS", "Pos2", Vector3ToString(value)); } }

            //PVPS
            public static int PVPS_Players { get { return GetConfig("PVPS", "Players", 4); } set { SetConfig(true, "PVPS", "Players", value); } }
            public static Dictionary<string, Dictionary<Vector3, PVP_PosData>> PVPS_Positions
            {
                get
                {
                    if (_plugin.Config.Get("PVPS", "Positions") == null)
                    {
                        SetConfig(false, "PVPS", "Positions", new Dictionary<string, Dictionary<Vector3, PVP_PosData>>());
                        return new Dictionary<string, Dictionary<Vector3, PVP_PosData>>();
                    }
                    try
                    {
                        return _plugin.Config.Get<Dictionary<string, Dictionary<string, PVP_PosData>>>("PVPS", "Positions").ToDictionary(kv2 => kv2.Key, kv2 => kv2.Value.ToDictionary(kv => StringToVector3(kv.Key), kv => kv.Value));
                    }
                    catch
                    {
                        return (_plugin.Config.Get("PVPS", "Positions") as Dictionary<string, object>).ToDictionary(kv2 => kv2.Key, kv2 => (kv2.Value as Dictionary<string, object>).ToDictionary(kv => StringToVector3(kv.Key), kv => PVP_PosData.Create(kv.Value as Dictionary<string, object>)));
                    }
                    //return (GetConfig("PVPS", "Positions", (object)new Dictionary<string, PVP_PosData>()) as Dictionary<string, PVP_PosData>).ToDictionary(kv => StringToVector3(kv.Key), kv => kv.Value);
                }
                set
                {
                    SetConfig(true, "PVPS", "Positions", value.ToDictionary(kv2 => kv2.Key, kv2 => kv2.Value.ToDictionary(kv => Vector3ToString(kv.Key), kv => kv.Value)));
                }
            }
            public static List<ItemInfo> PVPS_Prize
            {
                get
                {
                    return
                        GetConfig("PVPS", "Prize", new Dictionary<string, int>() { ["Detonator Cap"] = 1 }).Select(kv => new ItemInfo()
                        {
                            ID = kv.Key,
                            Count = kv.Value
                        }).ToList();
                }
                set
                {
                    SetConfig(true, "PVPS", "Prize", value.ToDictionary(i => i.ID, i => i.Count));
                }
            }
            public static Vector3 PVPS_Pos1 { get { return StringToVector3(GetConfig("PVPS", "Pos1", "0 0 0")); } set { SetConfig(true, "PVPS", "Pos1", Vector3ToString(value)); } }
            public static Vector3 PVPS_Pos2 { get { return StringToVector3(GetConfig("PVPS", "Pos2", "0 0 0")); } set { SetConfig(true, "PVPS", "Pos2", Vector3ToString(value)); } }

            public static void LoadAndSaveAll()
            {
                List<string> GetSetNames = typeof(ConfigData).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public).Where(v => v.Name.Remove(4).Contains("get_")).Select(v => v.Name.Remove(0, 4)).Distinct().ToList();
                foreach (var name in GetSetNames)
                {
                    typeof(ConfigData).GetMethod("set_" + name, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public).Invoke(null, new object[] { typeof(ConfigData).GetMethod("get_" + name, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public).Invoke(null, null) });
                }
            }
            public static void SetConfig(bool replace, string group, string key, object value)
            {
                if (replace || _plugin.Config.Get(group, key) == null) _plugin.Config.Set(group, key, value);
                _plugin.Config.Save();
            }
            public static T GetConfig<T>(string group, string key, T defaultVal)
            {
                if (_plugin.Config.Get(group, key) == null)
                {
                    SetConfig(false, group, key, defaultVal);
                    return defaultVal;
                }
                try { return _plugin.Config.Get<T>(group, key); }
                catch { return (T)(object)(_plugin.Config.Get(group, key) as List<object>); }
            }
            static string Vector3ToString(Vector3 pos) => $"{pos.x} {pos.y} {pos.z}";
            static Vector3 StringToVector3(string pos) => new Vector3(Convert.ToSingle(pos.Split(' ')[0]), Convert.ToSingle(pos.Split(' ')[1]), Convert.ToSingle(pos.Split(' ')[2]));

            public struct CAR_PosData
            {
                public string Rotation;
                public float RadiusPlayers;
                public Dictionary<int, ItemInfo> Weapons;
                public Dictionary<int, ItemInfo> CarItems;
                public Vector3 rotation { get { return StringToVector3(Rotation); } set { Rotation = Vector3ToString(new Vector3(float.Parse(Math.Round(value.x, 2).ToString()), float.Parse(Math.Round(value.y, 2).ToString()), float.Parse(Math.Round(value.z, 2).ToString()))); } }
                public static CAR_PosData Create(Dictionary<string, object> dict)
                {
                    CAR_PosData data = new CAR_PosData();
                    data.Rotation = dict["Rotation"].ToString();
                    data.RadiusPlayers = float.Parse(dict["RadiusPlayers"].ToString());
                    data.Weapons = (dict["Weapons"] as Dictionary<string, object>).ToDictionary(kv => int.Parse(kv.Key), kv => ItemInfo.Create(kv.Value as Dictionary<string, object>));
                    data.CarItems = (dict["CarItems"] as Dictionary<string, object>).ToDictionary(kv => int.Parse(kv.Key), kv => ItemInfo.Create(kv.Value as Dictionary<string, object>));
                    return data;
                }
                public CAR_PosData(Vector3 Rotation, Dictionary<int, ItemInfo> Weapons, Dictionary<int, ItemInfo> CarItems, float RadiusPlayers)
                {
                    this.Rotation = Vector3ToString(Rotation);
                    this.Weapons = Weapons;
                    this.CarItems = CarItems;
                    this.RadiusPlayers = RadiusPlayers;
                }
            }
            public struct PVP_PosData
            {
                public float RadiusPlayers;
                public Dictionary<int, ItemInfo> Weapons;
                public static PVP_PosData Create(Dictionary<string, object> dict)
                {
                    PVP_PosData data = new PVP_PosData();
                    data.RadiusPlayers = float.Parse(dict["RadiusPlayers"].ToString());
                    data.Weapons = (dict["Weapons"] as Dictionary<string, object>).ToDictionary(kv => int.Parse(kv.Key), kv => ItemInfo.Create(kv.Value as Dictionary<string, object>));
                    return data;
                }
                public PVP_PosData(Dictionary<int, ItemInfo> Weapons, float RadiusPlayers)
                {
                    this.Weapons = Weapons;
                    this.RadiusPlayers = RadiusPlayers;
                }
            }
        }

        void OnPlayerTakeDamage(PlayerSession session, EntityEffectSourceData source) => source.Value = (source?.EntitySource?.GetComponent<EventPlayer>()?.team?.Contains(session) ?? false) ? 0 : source.Value;

        new void LoadDefaultConfig() { PrintError("No config file found, generating a new one."); }
        new void LoadConfig()
        {
            ConfigData.LoadAndSaveAll();
            SaveConfig();
        }
        object canExtTeleport(PlayerSession session) => session?.WorldPlayerEntity?.GetComponent<EventPlayer>() == null ? (object)null : false;
        void OnServerInitialized()
        {
            List<string> comps = new List<string>()
            {
                "EventPlayer"
            };
            foreach (var edits in Resources.FindObjectsOfTypeAll<GameObject>().Where(ent => (ent?.GetComponents<Component>()?.Where(comp => comps.Contains(comp?.GetType()?.Name))?.Count() ?? 0) > 0).Select(ent => ent.GetComponents<Component>().Where(comp => comps.Contains(comp.GetType().Name))))
                try
                {
                    foreach (var edit in edits)
                        try { UnityEngine.Object.Destroy(edit); } catch { }
                }
                catch { }
            permission.RegisterPermission(PermReg, this);
            permission.RegisterPermission(PermAutoreg, this);
            AutoEvent.Init();
        }
        void Loaded()
        {
            _plugin = this;
            LoadConfig();
        }
        static void OutVehicle(PlayerSession session) => session?.WorldPlayerEntity?.GetComponent<CharacterMotorSimple>()?.InsideVehicle?.ExitVehicle(session?.WorldPlayerEntity?.GetComponent<CharacterMotorSimple>(), session.WorldPlayerEntity.GetComponent<CharacterMotorSimple>().InsideVehicle.GetVelocity(), session.WorldPlayerEntity.GetComponent<CharacterMotorSimple>().InsideVehicle.transform.position);
        void OnPlayerDisconnected(PlayerSession session)
        {
            EventPlayer player = session?.WorldPlayerEntity?.GetComponent<EventPlayer>();
            if (player == null) return;
            player?.exit?.Invoke(player);
            try { player?.Event.Unreg(session, null); } catch (Exception e) { Debug.LogError(e); }
            UnityEngine.Object.Destroy(player);
        }
        static EventPUBG eventPUBG = null;
        static EventCars eventCars = null;
        static EventPVPS eventPVPS = null;

        static void PutsOns(string message)
        {
            PlayerSession session = _plugin.Player.Find("TESTPLAYEROMXA835");
            if (session == null) return;
            _plugin.Player.Message(session, message);
        }
        static class AutoEvent
        {
            static Dictionary<string, Func<Action, Event>> eventCreators = new Dictionary<string, Func<Action, Event>>();
            static int num = -1;
            public static Dictionary<int, Event> Event = new Dictionary<int, Event>();
            static DateTime time = DateTime.MinValue;
            public static void Init()
            {
                eventCreators.Clear();
                eventCreators.Add("PUBG", (action) => new EventPUBG(ConfigData.PUBG_Players, ConfigData.PUBG_Weapons, ConfigData.PUBG_Positions, ConfigData.PUBG_Prize, ConfigData.PUBG_Pos1, ConfigData.PUBG_Pos2, action));
                eventCreators.Add("Cars", (action) => new EventCars(ConfigData.CARS_Players, ConfigData.CARS_Positions, ConfigData.CARS_Prize, ConfigData.CARS_Pos1, ConfigData.CARS_Pos2, action));
                eventCreators.Add("PVPS", (action) => new EventPVPS(ConfigData.PVPS_Players, ConfigData.PVPS_Positions, ConfigData.PVPS_Prize, ConfigData.PVPS_Pos1, ConfigData.PVPS_Pos2, action));
                Event.Clear();
                for (int i = 0; i < eventCreators.Count(); i++) Event.Add(i, null);
                StartNextTime();
            }
            public static void Next()
            {
                Event.Clear();
                for (int i = 0; i < eventCreators.Count(); i++) Event.Add(i, null);
                PutsOns($"NEXT: [{string.Join("|", Event.Select(e => e.Value == null ? "0" : "1").ToArray())}]");
                num = ((num + 1) < eventCreators.Count()) ? (num + 1) : 0;
                Event[num] = eventCreators.ElementAt(num).Value.Invoke(() => { PutsOns($"END_START: [{string.Join("|", Event.Select(e => e.Value == null ? "0" : "1").ToArray())}]"); StartNextTime(); Event.Clear(); for (int i = 0; i < eventCreators.Count(); i++) Event.Add(i, null); PutsOns($"END_END: [{string.Join("|", Event.Select(e => e.Value == null ? "0" : "1").ToArray())}]"); });
            }
            public static void StartNextTime()
            {
                time = DateTime.Now.AddMinutes(ConfigData.EVENT_CD);
                int n = num == -1 ? 0 : num;
                WriteTime(Mathf.RoundToInt(ConfigData.EVENT_CD * 60), ConfigData.EVENT_TIME,
                    (sec) =>
                    {
                        PutsOns($"N = {n},\n Event = [{string.Join("|", Event.Select(e => e.Value == null ? "0" : "1").ToArray())}],\nN + 1 = {(((num + 1) < eventCreators.Count()) ? (num + 1) : 0)},\n ECreat = [{string.Join("|", eventCreators.Select(e => e.Value == null ? "0" : "1").ToArray())}]");
                        if (NEXT != null) _plugin.Server.Broadcast(MSG_INFO_MIN.Replace("{event}", eventCreators.ElementAt(((num + 1) < eventCreators.Count()) ? (num + 1) : 0).Key).Replace("{time}", CurrTime(new TimeSpan(0, 0, sec))));
                    });
                _plugin.timer.Once(ConfigData.EVENT_CD * 60, Next);
            }

            public static void WriteTime(int max, List<int> times, Action<int> action)
            {
                _plugin.timer.Repeat(1, max, () =>
                {
                    max--;
                    PutsOns($"{max}: [{string.Join("|", Event.Select(e => e.Value == null ? "0" : "1").ToArray())}]");
                    if (times.Contains(max)) action.Invoke(max);
                });
            }

            static string CurrTime(DateTime value, DateTime next)
            {
                if (next.Ticks - value.Ticks <= 0) return "00:00";
                TimeSpan timeSpan = new TimeSpan(next.Ticks - value.Ticks);
                return $"{curr(Math.Floor(timeSpan.TotalMinutes).ToString(), '0', 2)}:{curr(timeSpan.Seconds.ToString(), '0', 2)}";
            }
            static string CurrTime(TimeSpan tSpan) => (tSpan.Ticks <= 0) ? "00:00" : $"{curr(Math.Floor(tSpan.TotalMinutes).ToString(), '0', 2)}:{curr(tSpan.Seconds.ToString(), '0', 2)}";
            static string curr(string value, char add, int count) => count > value.Count() ? $"{new string(add, count - value.Count())}{value}" : value;
            static void AddTimerMore(float time, Action action)
            {
                if (time <= 0) return;
                _plugin.timer.Once(time, action);
            }
            public static Event CURRENT => Event.Where(e => e.Value != null).Count() > 0 ? Event.Where(e => e.Value != null).FirstOrDefault().Value : null;
            public static string NEXT => eventCreators.ElementAt(((num + 1) < eventCreators.Count()) ? (num + 1) : 0).Key;

            public static void SendInfo(PlayerSession session)
            {
                if (CURRENT == null) _plugin.Player.Message(session, MSG_INFO.Replace("{event}", NEXT).Replace("{time}", CurrTime(DateTime.Now, time)));
                else _plugin.Player.Message(session, MSG_INFO_CURR);
            }
        }
        static class AutoReg
        {
            static List<string> AutoRegData = new List<string>();
            static void Save() => Interface.GetMod().DataFileSystem.WriteObject("AutoRegData", AutoRegData);
            static void Load()
            {
                if (!Interface.GetMod().DataFileSystem.ExistsDatafile("AutoRegData")) Interface.GetMod().DataFileSystem.WriteObject("AutoRegData", AutoRegData);
                AutoRegData = Interface.GetMod().DataFileSystem.ReadObject<List<string>>("AutoRegData");
            }
            public static List<PlayerSession> sessions = Contains(_plugin.Player.Sessions.Values);
            public static bool AddDel(string SteamID)
            {
                Load();
                if (AutoRegData.Contains(SteamID))
                {
                    AutoRegData.Remove(SteamID);
                    Save();
                    return false;
                }
                else
                {
                    AutoRegData.Add(SteamID);
                    Save();
                    return true;
                }
            }
            public static List<PlayerSession> Contains(IEnumerable<PlayerSession> sessions)
            {
                Load();
                return sessions.Where(session => AutoRegData.Contains(session.SteamId.ToString())).ToList();
            }
        }
        static class Rank
        {
            public static Dictionary<string, Dictionary<string, int>> RankData = new Dictionary<string, Dictionary<string, int>>();
            static void Save() => Interface.GetMod().DataFileSystem.WriteObject("RankData", RankData);
            public static void Load()
            {
                if (!Interface.GetMod().DataFileSystem.ExistsDatafile("RankData")) Interface.GetMod().DataFileSystem.WriteObject("RankData", RankData);
                RankData = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<string, Dictionary<string, int>>>("RankData");
            }
            public static void Add(string EventName, string SteamID)
            {
                Load();
                if (!RankData.ContainsKey(SteamID)) RankData.Add(SteamID, new Dictionary<string, int>() { ["PUBG"] = 0, ["Cars"] = 0, ["PVPS"] = 0 });
                if (!RankData[SteamID].ContainsKey(EventName)) RankData[SteamID].Add(EventName, 0);
                RankData[SteamID][EventName]++;
                Save();
            }
            public static int? GetValue(string SteamID)
            {
                Load();
                if (!RankData.ContainsKey(SteamID)) return null;
                Dictionary<string, int> data = RankData.OrderBy(kv => -kv.Value.Sum(d => d.Value)).Select((kv, num) => new KeyValuePair<string, int>(kv.Key, num)).ToDictionary(kv => kv.Key, kv => kv.Value);
                return data[SteamID] + 1;
            }
            public static Dictionary<string, int> GetData(string SteamID)
            {
                Load();
                if (!RankData.ContainsKey(SteamID)) return new Dictionary<string, int>() { ["PUBG"] = 0, ["Cars"] = 0, ["PVPS"] = 0 };
                return RankData[SteamID];
            }
            public static Dictionary<string, Dictionary<string, int>> GetTops(int count) => RankData.OrderBy(kv => -kv.Value.Sum(d => d.Value)).Where((kv, num) => num < count).ToDictionary(kv => kv.Key, kv => kv.Value);
            public static string GetName(string SteamID) => GetAllPlayers().Where(kv => kv.Value == SteamID).FirstOrDefault().Key ?? SteamID;
            public static List<KeyValuePair<string, string>> GetAllPlayers() => _plugin.permission.GetUsersInGroup("default").Select(val => new KeyValuePair<string, string>(val.Remove(val.Count() - 1).Remove(0, val.IndexOf(' ') + 2), val.Remove(val.IndexOf(' ')))).ToList();
        }

        [ChatCommand("event")]
        void ChatCommand_Event(PlayerSession session, string command, string[] args)
        {
            switch (args.FirstOrDefault())
            {
                case "close": if (!session.IsAdmin) break; if (AutoEvent.CURRENT == null) { Player.Message(session, "Already closed!"); return; } else { AutoEvent.CURRENT.StopEvent(); Player.Message(session, "You close event!"); } return;
                case "reg":
                    if (!permission.UserHasPermission(session.SteamId.ToString(), PermReg))
                    {
                        Player.Message(session, "<color=red>[EVENT]</color> Nie posiadasz SVIPa lub wyżej, aby się zarejestrować na event!");
                        return;
                    }
                    if (AutoEvent.CURRENT?.started != false)
                        Player.Message(session, "<color=red>[EVENT]</color> Brak eventu w tym momencie!\nSprawdź ile czasu pozostało do eventu: <color=orange>/event</color>");
                    else
                        AutoEvent.CURRENT.Reg(session);
                    return;
                case "unreg": if (AutoEvent.CURRENT?.started != false) { Player.Message(session, "<color=red>[EVENT]</color> Event jest zamknięty"); return; } else { AutoEvent.CURRENT.Unreg(session, MSG_UNREG_One); } return;
                case "pos":
                    if (!session.IsAdmin) break;
                    if (args.Count() < 2) { Player.Message(session, "/event pos <EventName>"); return; }
                    switch (string.Join(" ", args.Where((arg, num) => num != 1).ToArray()).ToLower())
                    {
                        case "pubg":
                            {
                                if (!session.IsAdmin) break;
                                List<Vector3> vectors = ConfigData.PUBG_Positions;
                                vectors.Add(session.WorldPlayerEntity.transform.position);
                                ConfigData.PUBG_Positions = vectors;
                                Player.Message(session, "You add pos!");
                            }
                            return;
                        case "cars":
                            {
                                if (!session.IsAdmin) break;
                                Dictionary<Vector3, ConfigData.CAR_PosData> vectors = ConfigData.CARS_Positions;
                                vectors.Add(session.WorldPlayerEntity.transform.position, new ConfigData.CAR_PosData(session.WorldPlayerEntity.GetComponent<CharacterMotorSimple>().AimRotationEulerCache, new Dictionary<int, ItemInfo>() { [0] = new ItemInfo("Bow T1", 1), [1] = new ItemInfo("Arrow", 50) }, new Dictionary<int, ItemInfo>(), 3));
                                ConfigData.CARS_Positions = vectors;
                                Player.Message(session, "You add pos!");
                            }
                            return;
                        case "pvps":
                            {
                                if (!session.IsAdmin) break;
                                Dictionary<string, Dictionary<Vector3, ConfigData.PVP_PosData>> vectors = ConfigData.PVPS_Positions;
                                if (!vectors.ContainsKey("DEFAULT")) vectors.Add("DEFAULT", new Dictionary<Vector3, ConfigData.PVP_PosData>());
                                vectors["DEFAULT"].Add(session.WorldPlayerEntity.transform.position, new ConfigData.PVP_PosData(new Dictionary<int, ItemInfo>() { [0] = new ItemInfo("Bow T1", 1), [1] = new ItemInfo("Arrow", 50) }, 3));
                                ConfigData.PVPS_Positions = vectors;
                                Player.Message(session, "You add pos!");
                            }
                            return;
                        default: Player.Message(session, $"Event '{string.Join(" ", args.Where((arg, num) => num != 1).ToArray()).ToLower()}' not found!"); return;
                    }
                    return;
                case "pos1":
                    if (!session.IsAdmin) break;
                    if (args.Count() < 2) { Player.Message(session, "/event pos1 <EventName>"); return; }
                    switch (string.Join(" ", args.Where((arg, num) => num != 1).ToArray()).ToLower())
                    {
                        case "pubg": ConfigData.PUBG_Pos1 = session.WorldPlayerEntity.transform.position; break;
                        case "cars": ConfigData.CARS_Pos1 = session.WorldPlayerEntity.transform.position; break;
                        case "pvps": ConfigData.PVPS_Pos1 = session.WorldPlayerEntity.transform.position; break;
                        default: Player.Message(session, $"Event '{string.Join(" ", args.Where((arg, num) => num != 1).ToArray()).ToLower()}' not found!"); return;
                    }
                    Player.Message(session, "You set pos1!");
                    return;
                case "pos2":
                    if (!session.IsAdmin) break;
                    if (args.Count() < 2) { Player.Message(session, "/event pos2 <EventName>"); return; }
                    switch (string.Join(" ", args.Where((arg, num) => num != 1).ToArray()).ToLower())
                    {
                        case "pubg": ConfigData.PUBG_Pos2 = session.WorldPlayerEntity.transform.position; break;
                        case "cars": ConfigData.CARS_Pos2 = session.WorldPlayerEntity.transform.position; break;
                        case "pvps": ConfigData.PVPS_Pos2 = session.WorldPlayerEntity.transform.position; break;
                        default: Player.Message(session, $"Event '{string.Join(" ", args.Where((arg, num) => num != 1).ToArray()).ToLower()}' not found!"); return;
                    }
                    Player.Message(session, "You set pos2!");
                    return;
                case "autoreg":
                    if (!permission.UserHasPermission(session.SteamId.ToString(), PermAutoreg))
                    {
                        Player.Message(session, "<color=lime>[EVENT AUTOREG]</color> Nie masz uprawnień, aby włączyć opcję AUTOREG!");
                        return;
                    }
                    Player.Message(session, $"{(AutoReg.AddDel(session.SteamId.ToString()) ? "<color=lime>[ON]</color> Włączyłeś AUTOREG" : "<color=red>[OFF]</color> Wyłączyłeś AUTOREG")}");
                    return;
                case "rank": Rank.Load(); Player.Message(session, $"<color=lime>[RANK]</color> Twoja pozycja {Rank.GetValue(session.SteamId.ToString())?.ToString() ?? "-"}, Wygrałeś: {string.Join(" | ", Rank.GetData(session.SteamId.ToString()).Select(kv => $"PVP {kv.Key}: {kv.Value}").ToArray())} = {Rank.GetData(session.SteamId.ToString()).Values.Sum()}"); return;
                case "top": Rank.Load(); Player.Message(session, "<color=lime>[TOPka Evenciarzy]</color>\n" + string.Join("\n", Rank.GetTops(5).Select((kv, num) => $"{num + 1}. {Rank.GetName(kv.Key)} = {kv.Value.Values.Sum()}"/*- {string.Join(" | ", kv.Value.Select(v => $"PVP {v.Key}: {v.Value}").ToArray())} = {kv.Value.Values.Sum()}"*/).ToArray())); return;
                default: break;
            }
            AutoEvent.SendInfo(session);
        }

        static T Random<T>(IEnumerable<T> ienumerable) => ienumerable.ElementAt(UnityEngine.Random.Range(0, ienumerable.Count()));
        class EventCars : Event
        {
            static string eCode = "0";
            public EventCars(int inOneTeam, Dictionary<Vector3, ConfigData.CAR_PosData> carsData, List<ItemInfo> prize, Vector3 Pos1, Vector3 Pos2, Action end)
            {
                this.Set("Cars", inOneTeam, carsData.Count(), carsData.Keys.ToList(), Pos1, Pos2, (_event, _player, _reg) =>
                {
                    switch (_reg)
                    {
                        case 1:
                            {
                                _plugin.Player.Message(_player, MSG_REG_One.Replace("{event}", _event.name));
                                _plugin.Server.Broadcast(MSG_REG_All.Replace("{event}", _event.name).Replace("{name}", _player.Identity.Name).Replace("{players}", _event.teams.Sum(team => team.ActivePlayers.Count()).ToString()).Replace("{max}", (_event.MaxTeams * _event.MaxInTeam).ToString()));
                            }
                            return;
                        case -1:
                            {
                                _plugin.Player.Message(_player, MSG_Cant_REG_One.Replace("{event}", _event.name));
                            }
                            return;
                        case -2:
                            {
                                _plugin.Player.Message(_player, MSG_Cant_REG_DIE.Replace("{event}", _event.name));
                            }
                            return;
                        case -3:
                            {
                                _plugin.Player.Message(_player, MSG_Cant_REG_RaidBlock.Replace("{event}", _event.name));
                            }
                            return;
                    }
                }, (_event, _player, _unreg) =>
                {
                    if (_unreg != null)
                    {
                        _plugin.Player.Message(_player, _unreg.Replace("{event}", _event.name));
                        _plugin.Server.Broadcast(MSG_UNREG_All.Replace("{event}", _event.name).Replace("{name}", _player.Identity.Name).Replace("{players}", _event.teams.Sum(team => team.ActivePlayers.Count()).ToString()).Replace("{max}", (_event.MaxTeams * _event.MaxInTeam).ToString()));
                    }
                    else
                    {
                        _plugin.Player.Message(_player, MSG_Cant_UNREG_One.Replace("{event}", _event.name));
                    }
                }, (_event) =>
                {
                    _plugin.Server.Broadcast(MSG_START.Replace("{event}", _event.name));
                    try
                    {
                        eCode = "1";
                        foreach (var team in _event.teams)
                        {
                            eCode = "2";
                            team.Load();
                            eCode = "3";
                            Vector3 teamPos = _event.GetPosition();
                            foreach (var player in team.ActivePlayers)
                            {
                                eCode = "4";
                                player.InvSave();
                                eCode = "5";
                                player.PosSave();
                                eCode = "8";
                                player.GetComponent<PlayerInventory>().ClearItems();
                                eCode = "9";
                                OutVehicle(player.session);
                                eCode = "10";
                                player.transform.position = teamPos;
                                foreach (var weapon in carsData[teamPos].Weapons)
                                {
                                    eCode = $"[{weapon.Key}]-{weapon.Value}";
                                    weapon.Value.ToInventory(weapon.Key, player.GetComponent<PlayerInventory>());
                                    eCode = $"[N][{weapon.Key}]-{weapon.Value.ID}*{weapon.Value.Count}";
                                }
                                eCode = "12";
                                _plugin.Player.Message(player.session, MSG_START_PLAYERS);
                                eCode = "13";
                                Heal(player.session);
                                eCode = "14";
                            }
                            eCode = "15.0";
                            vehicles.Add(CarSpawner.SpawnCar(teamPos, carsData[teamPos].rotation, carsData[teamPos].CarItems));
                            InvokeCircle(carsData[teamPos].RadiusPlayers, team.ActivePlayers.Count(), teamPos, (position, num) => team.ActivePlayers.ElementAt(num).transform.position = position);
                            eCode = "15";
                        }
                        eCode = "16";
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[ECode:{eCode}]\n{e}");
                        _plugin.Server.Broadcast($"<color=red>[ECODE:{eCode}]</color>");
                    }
                }, (_event, session) =>
                {
                    int pcount = _event.teams.Sum(team => team.ActivePlayers.Count());
                    int max = (_event.MaxTeams * _event.MaxInTeam);

                    _plugin.Server.Broadcast(MSG_DIE.Replace("{name}", session.Identity.Name).Replace("{player}", pcount.ToString()).Replace("{max}", max.ToString()));
                    EventPlayer eventPlayer = session.WorldPlayerEntity.GetComponent<EventPlayer>();
                    eventPlayer.DelDieLoot();
                    eventPlayer.GetComponent<PlayerInventory>().ClearItems();
                    eventPlayer.InvLoad();
                    if (_event.teams.Where(team => team.ActivePlayers.Count() > 0).Count() <= 1)
                    {
                        _event.StopEvent();
                        eventCars = null;
                    }
                }, (_event) =>
                {
                    PutsOns("Cars.End.foreach");
                    foreach (var team in _event.teams)
                        foreach (var player in team.players)
                        {
                            OutVehicle(player.Value?.session);
                            player.Value?.InvLoad();
                            player.Value?.PosLoad();
                        }
                    PutsOns("Cars.End.Invoke()");
                    end?.Invoke();
                    PutsOns("Cars.End.Invoked()");
                    if (_event.teams.Where(team => team.ActivePlayers.Count() > 0).Count() <= 1)
                    {
                        EventTeam winteam = _event.teams.Where(team => team.ActivePlayers.Count() > 0).FirstOrDefault();
                        if (winteam == null) return;
                        foreach (var player in winteam.LoadPlayers)
                        {
                            PlayerSession target = _plugin.Player.FindById(player);
                            if (target == null) continue;
                            EventTeam.GetPrize(target, (_event as EventCars).Prize);
                            _plugin.Player.Message(target, MSG_WON_PLAYERS.Replace("{items}", $"{string.Join("\n", (_event as EventCars).Prize.Select(item => $"{item.Count} {item.ID}").ToArray())}"));
                            Heal(target);
                        }
                        _plugin.Server.Broadcast(MSG_WIN.Replace("{event}", _event.name).Replace("{players}", string.Join(", ", winteam.LoadPlayers.Select(player => _plugin.Player.FindById(player)?.Identity?.Name).Where(name => name != null).ToArray())));
                    }
                    DestroyAll(vehicles);
                });
                Prize = prize;
                _positions = carsData.Keys.ToList();
                _plugin.Server.Broadcast(MSG_REG_Open.Replace("{event}", "Cars").Replace("{max}", positions.Sum((val) => inOneTeam).ToString()));
            }
            public List<ItemInfo> Prize;
            public override Vector3 GetPosition()
            {
                if (_positions.Count() == 0)
                {
                    return base.GetPosition();
                }
                else
                {
                    if (_pos >= _positions.Count()) _pos = 0;
                    return _positions[_pos++];
                }
            }

            int _pos = 0;
            List<Vector3> _positions;
            List<VehicleStatManager> vehicles = new List<VehicleStatManager>();
            static void DestroyAll(List<VehicleStatManager> vehicles)
            {
                foreach (var vehicle in vehicles.ToArray()) try { vehicle.DoDelayedDestroy(); } catch { }
                vehicles.Clear();
            }

            public static class CarSpawner
            {
                public static VehicleStatManager SpawnCar(Vector3 position, Vector3 rotation, Dictionary<int, ItemInfo> items)
                {
                    var obj = (Singleton<RuntimeHurtDB>.Instance.SearchObjects("Roach", null).First().Object as GameObject).GetComponent<NetworkInstantiateConfig>();
                    eCode = "15.1";
                    VehicleStatManager vehicle = Singleton<HNetworkManager>.Instance.NetInstantiate(obj, position, Quaternion.Euler(rotation), GameManager.GetSceneTime()).GetComponent<VehicleStatManager>();
                    eCode = "15.2";
                    //vehicle.LootConfig = LootTree.;
                    eCode = "15.3";

                    //vehicle. = new PlayerIdentity() { Name = "ClaimCar(Hash:4815646)" };
                    eCode = "15.4";
                    if (items == null) SetInvRoach(vehicle.GetComponent<Inventory>());
                    else SetInvRoach(vehicle.GetComponent<Inventory>(), items);
                    eCode = "15.5";
                    return vehicle;
                }
                static void SetInvRoach(Inventory inv)
                {
                    eCode = "15.4.1";
                    inv.SetSlot(0, getItemObjectFromName("Roach Rheum Side Panel Left")); inv.Invalidate(false);
                    inv.SetSlot(1, getItemObjectFromName("Roach Rheum Side Panel Right")); inv.Invalidate(false);
                    inv.SetSlot(2, getItemObjectFromName("Roach Rheum Bumper")); inv.Invalidate(false);
                    inv.SetSlot(3, getItemObjectFromName("Roach Rheum Rear Panel")); inv.Invalidate(false);
                    inv.SetSlot(4, getItemObjectFromName("Roach Rheum Roof Scoop")); inv.Invalidate(false);
                    inv.SetSlot(5, getItemObjectFromName("Billycart Wheel")); inv.Invalidate(false);
                    inv.SetSlot(6, getItemObjectFromName("Billycart Wheel")); inv.Invalidate(false);
                    inv.SetSlot(7, getItemObjectFromName("Sand Hopper Wheel")); inv.Invalidate(false);
                    eCode = "15.4.2";
                    inv.SetSlot(8, getItemObjectFromName("Sand Hopper Wheel")); inv.Invalidate(false);
                    inv.SetSlot(9, getItemObjectFromName("Roach Rheum Hood")); inv.Invalidate(false);
                    inv.SetSlot(10, getItemObjectFromName("Roach Engine")); inv.Invalidate(false);
                    inv.SetSlot(11, getItemObjectFromName("Roach Gearbox")); inv.Invalidate(false);
                    eCode = "15.4.3";
                    inv.SetSlot(12, getItemObjectFromName("Gasoline", 200)); inv.Invalidate(false);
                    inv.SetSlot(13, getItemObjectFromName("RoachChassis")); inv.Invalidate(false);
                    //inv.SetSlot(14, null; inv.Invalidate(false);
                    //inv.SetSlot(15, getItemObjectFromName("Roach Gearbox")); inv.Invalidate(false);
                    eCode = "15.4.4";
                    //inv.SetSlot(16, RandomInItem(new int[] { 195, 200 }, 1); inv.Invalidate(false);
                    //inv.SetSlot(18, null; inv.Invalidate(false);
                    eCode = "15.4.5";
                    //inv.SetSlot(20, null; inv.Invalidate(false);
                    //inv.SetSlot(21, null; inv.Invalidate(false);
                    //inv.SetSlot(22, null; inv.Invalidate(false);
                    eCode = "15.4.6";
                    //inv.SetSlot(23, RandomInItem(new int[6] { 178, 179, 204, 205, 206, 207 }, 1); inv.Invalidate(false);
                    eCode = "15.4.7";
                    // inv.SetSlot(17, SlotInItem(172); inv.Invalidate(false);
                    //inv.SetSlot(18, SlotInItem(175); inv.Invalidate(false);
                    // inv.SetSlot(19, SlotInItem(147, 255); inv.Invalidate(false);
                }
                static void SetInvRoach(Inventory inv, Dictionary<int, ItemInfo> items)
                {
                    foreach (var item in items) item.Value.ToInventory(item.Key, inv);
                }
                // not enabled
                static ItemObject RandomInItem(int[] array, int count, bool OnOff = false, int Wheel = -1)
                {
                    if (OnOff)
                        if (UnityEngine.Random.Range(0, 7) == 4)
                            return null;

                    if (array.Length != 0 && Wheel == -1)
                    {
                        return GlobalItemManager.Instance.CreateItem(Singleton<GlobalItemManager>.Instance.GetItem(array[UnityEngine.Random.Range(0, array.Length)]).Generator, count);
                    }
                    else
                    {
                        if (Wheel == -1)
                            return null;
                    }
                    if (Wheel != -1)
                    {
                        if (Wheel == 166 || Wheel == 192)
                        {
                            while (true)
                            {
                                int i = array[UnityEngine.Random.Range(0, array.Length)];
                                if (i != 167 && i != 305) return GlobalItemManager.Instance.CreateItem(Singleton<GlobalItemManager>.Instance.GetItem(i).Generator, count);
                            }
                        }

                        if (Wheel == 305)
                        {
                            while (true)
                            {
                                int i = array[UnityEngine.Random.Range(0, array.Length)];
                                if (i != 167) return GlobalItemManager.Instance.CreateItem(Singleton<GlobalItemManager>.Instance.GetItem(i).Generator, count);
                            }
                        }
                        if (Wheel == 167) return GlobalItemManager.Instance.CreateItem(Singleton<GlobalItemManager>.Instance.GetItem(array[UnityEngine.Random.Range(0, array.Length)]).Generator, count);
                    }
                    return null;
                }
                static ItemObject RandomInItem(int first, int second, int count, bool OnOff = false) => OnOff ? (UnityEngine.Random.Range(0, 7) == 4) ? null : GlobalItemManager.Instance.CreateItem(GlobalItemManager.Instance.GetItem(UnityEngine.Random.Range(first, second + 1)).Generator, count) : GlobalItemManager.Instance.CreateItem(Singleton<GlobalItemManager>.Instance.GetItem(UnityEngine.Random.Range(first, second + 1)).Generator, count);
                static ItemObject SlotInItem(string Id, int count = 1) => GlobalItemManager.Instance.CreateItem(getItemFromName(Id), count);
            }
        }
        class EventPUBG : Event
        {
            public EventPUBG(int players, Dictionary<string, Dictionary<int, ItemInfo>> weaponsList, List<Vector3> positions, List<ItemInfo> prize, Vector3 Pos1, Vector3 Pos2, Action end)
            {
                this.Set("PUBG", 1, players, positions, Pos1, Pos2, (_event, _player, _reg) =>
                {
                    switch (_reg)
                    {
                        case 1:
                            {
                                _plugin.Player.Message(_player, MSG_REG_One.Replace("{event}", _event.name));
                                _plugin.Server.Broadcast(MSG_REG_All.Replace("{event}", _event.name).Replace("{name}", _player.Identity.Name).Replace("{players}", _event.teams.Sum(team => team.ActivePlayers.Count()).ToString()).Replace("{max}", (_event.MaxTeams * _event.MaxInTeam).ToString()));
                            }
                            return;
                        case -1:
                            {
                                _plugin.Player.Message(_player, MSG_Cant_REG_One.Replace("{event}", _event.name));
                            }
                            return;
                        case -2:
                            {
                                _plugin.Player.Message(_player, MSG_Cant_REG_DIE.Replace("{event}", _event.name));
                            }
                            return;
                        case -3:
                            {
                                _plugin.Player.Message(_player, MSG_Cant_REG_RaidBlock.Replace("{event}", _event.name));
                            }
                            return;
                    }
                }, (_event, _player, _unreg) =>
                {
                    if (_unreg != null)
                    {
                        _plugin.Player.Message(_player, _unreg.Replace("{event}", _event.name));
                        _plugin.Server.Broadcast(MSG_UNREG_All.Replace("{event}", _event.name).Replace("{name}", _player.Identity.Name).Replace("{players}", _event.teams.Sum(team => team.ActivePlayers.Count()).ToString()).Replace("{max}", (_event.MaxTeams * _event.MaxInTeam).ToString()));
                    }
                    else
                    {
                        _plugin.Player.Message(_player, MSG_Cant_UNREG_One.Replace("{event}", _event.name));
                    }
                }, (_event) =>
                {
                    _plugin.Server.Broadcast(MSG_START.Replace("{event}", _event.name));
                    KeyValuePair<string, Dictionary<int, ItemInfo>> weapons = Random(weaponsList);
                    string eCode = "0";
                    try
                    {
                        eCode = "1";
                        foreach (var team in _event.teams)
                        {
                            eCode = "2";
                            team.Load();
                            eCode = "3";
                            foreach (var player in team.ActivePlayers)
                            {
                                eCode = "4";
                                player.InvSave();
                                eCode = "5";
                                player.PosSave();
                                eCode = "6";
                                eCode = "7";
                                eCode = "8";
                                player.GetComponent<PlayerInventory>().ClearItems();
                                eCode = "9";
                                OutVehicle(player.session);
                                eCode = "10";
                                player.transform.position = _event.GetPosition();
                                eCode = $"11:{weaponsList.Count()}:{weapons.Key}";
                                foreach (var weapon in weapons.Value)
                                {
                                    eCode = $"[{weapon.Key}]-{weapon.Value}";
                                    weapon.Value.ToInventory(weapon.Key, player.GetComponent<PlayerInventory>());
                                    eCode = $"[N][{weapon.Key}]-{weapon.Value.ID}*{weapon.Value.Count}";
                                }
                                eCode = "12";
                                _plugin.Player.Message(player.session, MSG_START_PLAYERS);
                                eCode = "13";
                                Heal(player.session);
                                eCode = "14";
                            }
                            eCode = "15";
                        }
                        eCode = "16";
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[ECode:{eCode}]\n{e}");
                        _plugin.Server.Broadcast($"<color=red>[ECODE:{eCode}]</color>");
                    }
                }, (_event, session) =>
                {
                    int pcount = _event.teams.Sum(team => team.ActivePlayers.Count());
                    int max = (_event.MaxTeams * _event.MaxInTeam);

                    _plugin.Server.Broadcast(MSG_DIE.Replace("{name}", session.Identity.Name).Replace("{player}", pcount.ToString()).Replace("{max}", max.ToString()));
                    EventPlayer eventPlayer = session.WorldPlayerEntity.GetComponent<EventPlayer>();
                    eventPlayer.DelDieLoot();
                    eventPlayer.GetComponent<PlayerInventory>().ClearItems();
                    eventPlayer.InvLoad();
                    if (_event.teams.Where(team => team.ActivePlayers.Count() > 0).Count() <= 1)
                    {
                        _event.StopEvent();
                        eventPUBG = null;
                    }
                }, (_event) =>
                {
                    foreach (var team in _event.teams)
                        foreach (var player in team.players)
                        {
                            player.Value?.InvLoad();
                            player.Value?.PosLoad();
                        }
                    end?.Invoke();
                    if (_event.teams.Where(team => team.ActivePlayers.Count() > 0).Count() <= 1)
                    {
                        EventTeam winteam = _event.teams.Where(team => team.ActivePlayers.Count() > 0).FirstOrDefault();
                        if (winteam == null) return;
                        foreach (var player in winteam.LoadPlayers)
                        {
                            PlayerSession target = _plugin.Player.FindById(player);
                            if (target == null) continue;
                            EventTeam.GetPrize(target, (_event as EventPUBG).Prize);
                            _plugin.Player.Message(target, MSG_WON_PLAYERS.Replace("{items}", $"{string.Join("\n", (_event as EventPUBG).Prize.Select(item => $"{item.Count} {item.ID}").ToArray())}"));
                            Heal(target);
                        }
                        _plugin.Server.Broadcast(MSG_WIN.Replace("{event}", _event.name).Replace("{players}", string.Join(", ", winteam.LoadPlayers.Select(player => _plugin.Player.FindById(player)?.Identity?.Name).Where(name => name != null).ToArray())));
                    }
                });
                Prize = prize;
                _positions = Shuffle(positions);
                _plugin.Server.Broadcast(MSG_REG_Open.Replace("{event}", "PUBG").Replace("{max}", players.ToString()));
            }
            public List<ItemInfo> Prize;
            public override Vector3 GetPosition()
            {
                if (_positions.Count() == 0)
                {
                    return base.GetPosition();
                }
                else
                {
                    if (_pos >= _positions.Count()) _pos = 0;
                    return _positions[_pos++];
                }
            }

            int _pos = 0;
            List<Vector3> _positions;
        }
        class EventPVPS : Event
        {
            static string eCode = "0";
            public EventPVPS(int inOneTeam, Dictionary<string, Dictionary<Vector3, ConfigData.PVP_PosData>> pvpsData, List<ItemInfo> prize, Vector3 Pos1, Vector3 Pos2, Action end)
            {
                KeyValuePair<string, Dictionary<Vector3, ConfigData.PVP_PosData>> random = Random(pvpsData);
                this.Set("PVPS", inOneTeam, random.Value.Count(), random.Value.Select(val => val.Key).ToList(), Pos1, Pos2, (_event, _player, _reg) =>
                {
                    switch (_reg)
                    {
                        case 1:
                            {
                                _plugin.Player.Message(_player, MSG_REG_One.Replace("{event}", _event.name));
                                _plugin.Server.Broadcast(MSG_REG_All.Replace("{event}", _event.name).Replace("{name}", _player.Identity.Name).Replace("{players}", _event.teams.Sum(team => team.ActivePlayers.Count()).ToString()).Replace("{max}", (_event.MaxTeams * _event.MaxInTeam).ToString()));
                            }
                            return;
                        case -1:
                            {
                                _plugin.Player.Message(_player, MSG_Cant_REG_One.Replace("{event}", _event.name));
                            }
                            return;
                        case -2:
                            {
                                _plugin.Player.Message(_player, MSG_Cant_REG_DIE.Replace("{event}", _event.name));
                            }
                            return;
                        case -3:
                            {
                                _plugin.Player.Message(_player, MSG_Cant_REG_RaidBlock.Replace("{event}", _event.name));
                            }
                            return;
                    }
                }, (_event, _player, _unreg) =>
                {
                    if (_unreg != null)
                    {
                        _plugin.Player.Message(_player, _unreg.Replace("{event}", _event.name));
                        _plugin.Server.Broadcast(MSG_UNREG_All.Replace("{event}", _event.name).Replace("{name}", _player.Identity.Name).Replace("{players}", _event.teams.Sum(team => team.ActivePlayers.Count()).ToString()).Replace("{max}", (_event.MaxTeams * _event.MaxInTeam).ToString()));
                    }
                    else
                    {
                        _plugin.Player.Message(_player, MSG_Cant_UNREG_One.Replace("{event}", _event.name));
                    }
                }, (_event) =>
                {
                    _plugin.Server.Broadcast(MSG_START.Replace("{event}", _event.name));
                    try
                    {
                        eCode = "1";
                        foreach (var team in _event.teams)
                        {
                            eCode = "2";
                            team.Load();
                            eCode = "3";
                            Vector3 teamPos = _event.GetPosition();
                            foreach (var player in team.ActivePlayers)
                            {
                                eCode = "4";
                                player.InvSave();
                                eCode = "5";
                                player.PosSave();
                                eCode = "6";
                                eCode = "7";
                                eCode = "8";
                                player.GetComponent<PlayerInventory>().ClearItems();
                                eCode = "9";
                                OutVehicle(player.session);
                                eCode = "10";
                                player.transform.position = teamPos;
                                foreach (var weapon in random.Value[teamPos].Weapons)
                                {
                                    eCode = $"[{weapon.Key}]-{weapon.Value}";
                                    weapon.Value.ToInventory(weapon.Key, player.GetComponent<PlayerInventory>());
                                    eCode = $"[N][{weapon.Key}]-{weapon.Value.ID}*{weapon.Value.Count}";
                                }
                                eCode = "12";
                                _plugin.Player.Message(player.session, MSG_START_PLAYERS);
                                eCode = "13";
                                Heal(player.session);
                                eCode = "14";
                            }
                            eCode = "15.0";
                            InvokeCircle(random.Value[teamPos].RadiusPlayers, team.ActivePlayers.Count(), teamPos, (position, num) => team.ActivePlayers.ElementAt(num).transform.position = position);
                            eCode = "15";
                        }
                        eCode = "16";
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[ECode:{eCode}]\n{e}");
                        _plugin.Server.Broadcast($"<color=red>[ECODE:{eCode}]</color>");
                    }
                }, (_event, session) =>
                {
                    int pcount = _event.teams.Sum(team => team.ActivePlayers.Count());
                    int max = (_event.MaxTeams * _event.MaxInTeam);

                    _plugin.Server.Broadcast(MSG_DIE.Replace("{name}", session.Identity.Name).Replace("{player}", pcount.ToString()).Replace("{max}", max.ToString()));
                    EventPlayer eventPlayer = session.WorldPlayerEntity.GetComponent<EventPlayer>();
                    eventPlayer.DelDieLoot();
                    eventPlayer.GetComponent<PlayerInventory>().ClearItems();
                    eventPlayer.InvLoad();
                    if (_event.teams.Where(team => team.ActivePlayers.Count() > 0).Count() <= 1)
                    {
                        _event.StopEvent();
                        eventPVPS = null;
                    }
                }, (_event) =>
                {
                    foreach (var team in _event.teams)
                        foreach (var player in team.players)
                        {
                            OutVehicle(player.Value?.session);
                            player.Value?.InvLoad();
                            player.Value?.PosLoad();
                        }
                    end?.Invoke();
                    if (_event.teams.Where(team => team.ActivePlayers.Count() > 0).Count() <= 1)
                    {
                        EventTeam winteam = _event.teams.Where(team => team.ActivePlayers.Count() > 0).FirstOrDefault();
                        if (winteam == null) return;
                        foreach (var player in winteam.LoadPlayers)
                        {
                            PlayerSession target = _plugin.Player.FindById(player);
                            if (target == null) continue;
                            EventTeam.GetPrize(target, (_event as EventPVPS).Prize);
                            _plugin.Player.Message(target, MSG_WON_PLAYERS.Replace("{items}", $"{string.Join("\n", (_event as EventPVPS).Prize.Select(item => $"{item.Count} {item.ID}").ToArray())}"));
                            Heal(target);
                        }
                        _plugin.Server.Broadcast(MSG_WIN.Replace("{event}", _event.name).Replace("{players}", string.Join(", ", winteam.LoadPlayers.Select(player => _plugin.Player.FindById(player)?.Identity?.Name).Where(name => name != null).ToArray())));
                    }
                });
                Prize = prize;
                _positions = random.Value.Keys.ToList();
                _plugin.Server.Broadcast(MSG_REG_Open.Replace("{event}", "PVPS").Replace("{max}", positions.Sum((val) => inOneTeam).ToString()));
            }
            public List<ItemInfo> Prize;
            public override Vector3 GetPosition()
            {
                if (_positions.Count() == 0)
                {
                    return base.GetPosition();
                }
                else
                {
                    if (_pos >= _positions.Count()) _pos = 0;
                    return _positions[_pos++];
                }
            }

            int _pos = 0;
            List<Vector3> _positions;
        }

        class Event
        {
            public bool started = false;
            public string name;
            public int MaxInTeam;
            public int MaxTeams;
            public List<EventTeam> teams;
            public List<Vector3> positions;
            public Vector3 Pos1;
            public Vector3 Pos2;
            public Action<Event, PlayerSession, int> _aReg;
            public Action<Event, PlayerSession, string> _aUnreg;
            public Action<Event> _aStart;
            public Action<Event, PlayerSession> _aNext;
            public Action<Event> _aStop;
            public bool InZone(Vector3 position) => InBox(Pos1, Pos2, position);
            public virtual Vector3 GetPosition() => positions.Random();
            public void Set(string name, int MaxInTeam, int MaxTeams, List<Vector3> positions, Vector3 Pos1, Vector3 Pos2, Action<Event, PlayerSession, int> AReg, Action<Event, PlayerSession, string> AUnreg, Action<Event> AStart, Action<Event, PlayerSession> ANext, Action<Event> AStop)
            {
                this.name = name;
                this.MaxInTeam = MaxInTeam;
                this.MaxTeams = MaxTeams;
                this.positions = positions;
                this.Pos1 = Pos1;
                this.Pos2 = Pos2;
                this._aReg = AReg;
                this._aUnreg = AUnreg;
                this._aStart = AStart;
                this._aNext = ANext;
                this._aStop = AStop;
                teams = new List<EventTeam>();
                for (int i = 0; i < MaxTeams; i++) teams.Add(new EventTeam(this, MaxInTeam));
                foreach (var session in AutoReg.sessions) Reg(session);
            }
            public void Reg(PlayerSession session)
            {
                try
                {
                    if (session?.WorldPlayerEntity?.GetComponent<EventPlayer>() != null)
                    {
                        _aReg.Invoke(this, session, -1);
                        return;
                    }
                    if (session?.WorldPlayerEntity == null)
                    {
                        _aReg.Invoke(this, session, -1);
                        return;
                    }
                    if (NoEscapeV2 && !NoEscapeV2.Call<bool>("IsRaid", session))
                    {
                        _aReg.Invoke(this, session, -3);
                        return;
                    }
                    if (!EventTeam.Add(teams, session, null))
                    {
                        _aReg.Invoke(this, session, -1);
                        return;
                    }
                }
                catch(Exception ex)
                {
                    Interface.GetMod().LogInfo("Error: {0}{1}{2}", ex.Message, Environment.NewLine, ex.StackTrace);
                }
                _aReg.Invoke(this, session, 1);
                StartEvent();
            }
            public void Unreg(PlayerSession session, string invoke)
            {
                if (started && invoke != null) { _aUnreg?.Invoke(this, session, null); return; }
                if (session?.WorldPlayerEntity?.GetComponent<EventPlayer>() == null) return;
                if (EventTeam.Contains(teams, session))
                {
                    bool ret = EventTeam.Remove(teams, session?.WorldPlayerEntity?.GetComponent<EventPlayer>());
                    if (invoke != null) _aUnreg?.Invoke(this, session, ret ? invoke : null);
                    else _aNext.Invoke(this, session);
                    UnityEngine.Object.Destroy(session?.WorldPlayerEntity?.GetComponent<EventPlayer>());
                    return;
                }
                UnityEngine.Object.Destroy(session?.WorldPlayerEntity?.GetComponent<EventPlayer>());
                return;
            }
            public void StartEvent()
            {
                if (teams.Where(team => team.ActivePlayers.Count() != team.players.Count()).Count() > 0) return;
                _aStart.Invoke(this);
                started = true;
            }
            public void StopEvent()
            {
                started = false;
                _aStop.Invoke(this);
                foreach (var user in (teams.Where(team => team.ActivePlayers.Count() > 0)?.FirstOrDefault()?.LoadPlayers ?? new List<string>())) Rank.Add(name, user);
                foreach (var team in teams) team.DelAll();
                _plugin.Server.Broadcast(MSG_STOP.Replace("{event}", name));
            }
        }
        class EventTeam
        {
            public List<string> LoadPlayers = new List<string>();
            public Dictionary<int, EventPlayer> players;
            public Event Event;
            public IEnumerable<EventPlayer> ActivePlayers => players.Where(player => player.Value != null).Select(player => player.Value);
            public EventTeam(Event Event, int count)
            {
                players = new Dictionary<int, EventPlayer>();
                for (int i = 0; i < count; i++) players.Add(i, null);
                this.Event = Event;
            }
            public void DelAll()
            {
                foreach (var player in ActivePlayers)
                    UnityEngine.Object.Destroy(player);
            }
            public void Load() => LoadPlayers = players.Where(player => player.Value?.session != null).Select(player => player.Value.session.SteamId.ToString()).ToList();
            public bool Contains(PlayerSession session) => players.Where(player => player.Value?.session == session && player.Value?.session != null).Count() > 0;
            public static void GetPrize(PlayerSession session, List<ItemInfo> items)
            {
                foreach (var item in items) GlobalItemManager.Instance.GiveItem(item.ItemID(), item.Count, session.WorldPlayerEntity.GetComponent<PlayerInventory>());
            }
            public static bool Contains(IEnumerable<EventTeam> teams, PlayerSession session) => teams.Where(team => team.players.Where(player => player.Value?.session == session && player.Value?.session != null).Count() > 0).Count() > 0;
            public static bool Add(IEnumerable<EventTeam> teams, PlayerSession player, Action<EventPlayer> exit)
            {
                foreach (var team in teams)
                    for (int i = 0; i < team.players.Count(); i++)
                    {
                        if (team.players[i] == null)
                        {
                            team.players[i] = EventPlayer.Create(team.Event, team, player, exit);
                            return true;
                        }
                    }
                return false;
            }
            public static bool Remove(IEnumerable<EventTeam> teams, EventPlayer player)
            {
                bool ret = false;
                foreach (var team in teams)
                    if (team.players.ContainsValue(player))
                    {
                        ret = true;
                        team.players[team.players.First((val) => val.Value == player).Key] = null;
                    }
                return ret;
            }
        }

        public static ItemGeneratorAsset getItemFromName(string name)
        {
            foreach (var it in GlobalItemManager.Instance.GetGenerators())
                if (it.Value.name == name)
                    return GlobalItemManager.Instance.GetGenerators()[it.Value.GeneratorId];

            return GlobalItemManager.Instance.GetGenerators()[1];
        }

        public static ItemObject getItemObjectFromName(string name, int count = 0)
        {
            foreach (var it in GlobalItemManager.Instance.GetGenerators())
                if (it.Value.name == name)
                {
                    if (count != 0)
                        return GlobalItemManager.Instance.CreateItem(GlobalItemManager.Instance.GetGenerators()[it.Value.GeneratorId], count);
                    return GlobalItemManager.Instance.GetItem(it.Value.GeneratorId);
                }


            return GlobalItemManager.Instance.GetItem(0);
        }

        class EventPlayer : MonoBehaviour
        {
            public float InfamyValue = 0;
            public Vector3? Position = null;
            public PlayerSession session;
            public Action<EventPlayer> exit;
            Dictionary<int, ItemInfo?> items = new Dictionary<int, ItemInfo?>();
            public Event Event;
            public int Value;
            public float outZone = 0.5f;
            public EventTeam team;
            public static EventPlayer Create(Event Event, EventTeam team, PlayerSession session, Action<EventPlayer> exit)
            {
                EventPlayer player = session.WorldPlayerEntity.GetComponent<EventPlayer>() ?? session.WorldPlayerEntity.gameObject.AddComponent<EventPlayer>();
                player.session = session;
                player.Event = Event;
                player.Value = 0;
                player.exit = exit;
                player.team = team;
                return player;
            }
            public void FixedUpdate()
            {
                if (Event == null) Destroy(this);
                if (!(Event?.started ?? false))
                {
                    if (NoEscapeV2.Call("isRaid", session)?.ToString() == true.ToString())
                    {
                        try { Event.Unreg(session, MSG_UNREG_RaidBlock); } catch (Exception e) { Debug.LogError(e); }
                        Destroy(this);
                    }
                    if ((session?.WorldPlayerEntity?.GetComponent<EntityStats>()?.GetFluidEffect(effect().Health)?.GetValue() ?? 0) <= 0)
                    {
                        try { Event.Unreg(session, MSG_UNREG_DIE); } catch (Exception e) { Debug.LogError(e); }
                        Destroy(this);
                    }
                    return;
                }
                if (!Event.InZone(this.transform.position) || !session.IsLoaded || (NoEscapeV2.Call<bool>("IsRaidBlock", session)))
                {
                    if (outZone <= 0) session?.WorldPlayerEntity?.GetComponent<EntityStats>()?.GetFluidEffect(effect().Health)?.SetValue(0);
                    else outZone = outZone - Time.fixedDeltaTime;
                }
                else
                {
                    outZone = 0.5f;
                }
                if ((session?.WorldPlayerEntity?.GetComponent<EntityStats>()?.GetFluidEffect(effect().Health)?.GetValue() ?? 0) <= 0)
                {
                    exit?.Invoke(this);
                    try { Event.Unreg(session, null); } catch (Exception e) { Debug.LogError(e); }
                    Destroy(this);
                }
            }
            public void DelDieLoot()
            {
                Inventory inventory = Resources.FindObjectsOfTypeAll<Inventory>().Where(inv => Vector3.Distance(inv.transform.position, transform.position) <= 0.2f && inv.gameObject != this.gameObject).FirstOrDefault();
                if (inventory == null) return;
                Singleton<HNetworkManager>.Instance.NetDestroy(inventory.networkView);
            }
            public void InvAdd(List<ItemInfo> items)
            {
                var inv = session.WorldPlayerEntity.GetComponent<PlayerInventory>();
                foreach (var item in items)
                    inv.GiveItemServer(item.Item());//GlobalItemManager.Instance.GiveItem(item.ItemID, item.Count, );
            }
            public void PosSave() => Position = this.transform.position;
            public void PosLoad() => this.transform.position = Position ?? this.transform.position;
            public void InvSave()
            {
                var inv = session.WorldPlayerEntity.GetComponent<PlayerInventory>();
                KeyValuePair<int, ItemInfo?> dict = new KeyValuePair<int, ItemInfo?>();
                for (int d = 0; d < inv.Capacity; d++)
                {
                    //dict.
                }
                //session.WorldPlayerEntity.GetComponent<PlayerInventory>().Items => new KeyValuePair<int, ItemInfo?>(slot, item?.Item?.ItemId == null ? (ItemInfo?)null : new ItemInfo(item))).ToDictionary(kv => kv.Key, kv => kv.Value);
            }

            public void InvLoad()
            {
                session.WorldPlayerEntity.GetComponent<PlayerInventory>().ClearItems();
                foreach (var item in items)
                    item.Value?.ToInventory(item.Key, session.WorldPlayerEntity.GetComponent<PlayerInventory>());
            }
        }
        struct ItemInfo
        {
            public string ID;
            public int Count;

            public ItemGeneratorAsset ItemID()
            {
                return getItemFromName(ID);
            }

            public ItemInfo(string id, int count)
            {
                ID = id;
                Count = count;
            }
            public ItemInfo(ItemObject item)
            {
                ID = item.Generator.name;
                Count = item.StackSize;
            }

            public static ItemInfo Create(Dictionary<string, object> dict)
            {
                ItemInfo data = new ItemInfo();
                data.ID = (string)dict["ItemID"];
                data.Count = (int)dict["Count"];
                return data;
            }

            public ItemObject Item() => GlobalItemManager.Instance.CreateItem(ItemID(), Count);
            public bool ToInventory(int slot, Inventory inventory)
            {
                inventory.SuspendSync();
                inventory.SetSlot(slot, Item());
                inventory.ResumeSync();
                return true;
            }
        }

        static void InvokeCircle(float Radius, int Count, Vector3 center, Action<Vector3, int> action)
        {
            float b = 6.28f / Count;
            float m = 0;
            int i = 0;
            while (m < 6.28)
            {
                action.Invoke(center + new Vector3(Radius * (float)Math.Cos(m), 0, Radius * (float)Math.Sin(m)), i);
                i++;
                m = m + b;
            }
        }
        static bool InBox(Vector3 pos1, Vector3 pos2, Vector3 pos) => (pos.x >= Mathf.Min(pos1.x, pos2.x)) && (pos.x <= Mathf.Max(pos1.x, pos2.x)) && (pos.y >= Mathf.Min(pos1.y, pos2.y)) && (pos.y <= Mathf.Max(pos1.y, pos2.y)) && (pos.z >= Mathf.Min(pos1.z, pos2.z)) && (pos.z <= Mathf.Max(pos1.z, pos2.z));
    }
}