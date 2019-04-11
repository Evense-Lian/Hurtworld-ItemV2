using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Oxide.Core;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("SpawnConfigV2", "Kaidoz", "1.0.2")]
    class SpawnConfigV2 : HurtworldPlugin
    {
        [PluginReference]
        Plugin AirDropManager;

        public class CFG
        {
            [JsonProperty("Version")]
            public int Version;

            [JsonProperty("Ignore Version")]
            public bool IgnoreVersion;

            [JsonProperty("Dictionary MultiSpawners")]
            public Dictionary<string, MultiSpawnerData> MultiSpawners;

            [JsonProperty("Dictionary TerritorySpawners")]
            public Dictionary<string, TerritoryControlData> TerritorySpawners;
        }

        public class MultiSpawnerData
        {
            public bool ChildToSelf;
            public float SecondsPerSpawn;
            public int SpawnedLimit;
            public List<Spawn> Spawns;
            public Vector3 Offset;
            public float InitialWaitTime;
            public int MinimumSpawnCount;

            public MultiSpawnerData Create(MultiSpawner multiSpawner)
            {
                ChildToSelf = multiSpawner.ChildToSelf;
                InitialWaitTime = multiSpawner.InitialWaitTime;
                MinimumSpawnCount = multiSpawner.MinimumSpawnCount;
                Offset = multiSpawner.Offset;
                SecondsPerSpawn = multiSpawner.SecondsPerSpawn;
                SpawnedLimit = multiSpawner.SpawnedLimit;
                Spawns = multiSpawner.Spawns;
                return this;
            }
        }

        public class TerritoryControlData
        {
            public string TerritoryName;

            public TerritoryControlData Create(LevelTerritoryControlMarkerSpawner markerSpawner)
            {
                TerritoryName = markerSpawner.TerritoryName;
                return this;
            }
        }

        void OnServerInitialized()
        {
            NextTick(Loads);
        }

        void Loads()
        {
            loadserver = true;
            AirDropManager = (Plugin)plugins.Find("AirDropManager");
            LoadConfig();
            SetMultiSpawnerData();
            SetTerritorySpawners();
        }

        #region Config

        private CFG cfg;
        private bool loadserver = false;

        private new void LoadConfig()
        {
            try
            {

                cfg = Config.ReadObject<CFG>();
                if (cfg == null) throw new Exception();
                if (cfg.IgnoreVersion && cfg.Version != GameManager.PROTOCOL_VERSION)
                {
                    Config.WriteObject(cfg, false, $"{Interface.Oxide.ConfigDirectory}/{Name}.jsonOld");
                    PrintError("Old verision config!\n" +
                           "The error config file was saved in the .jsonOld extension");
                    LoadDefaultConfig();
                }
            }
            catch (Exception ex)
            {
                LoadDefaultConfig();
            }
            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            if (!loadserver)
                return;
            PrintWarning("Create new config for SpawnConfig...");
            cfg = new CFG()
            {
                Version = GameManager.PROTOCOL_VERSION,
                IgnoreVersion = true,
                MultiSpawners = GetMultiSpawnerData(),
                TerritorySpawners = GetTerritorySpawners()
            };
            SaveConfig();
        }

        protected override void SaveConfig()
        {
             Config.Settings = new JsonSerializerSettings
             {
                 ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                 Converters = new List<JsonConverter>
                    {
                        new StringEnumConverter(),
                        new UnityVector3Converter()
                    },
                 ContractResolver = new DynamicContractResolver()
             };
            Config.WriteObject(cfg);
        }

        #endregion

        [ConsoleCommand("spawnconfig.reload")]
        private void cmdreload(string cmd)
        {
            OnServerInitialized();
            Puts("SpawnConfig reloaded!");
        }

        #region MultiSpawner

        private Dictionary<string, MultiSpawnerData> GetMultiSpawnerData()
        {
            var multiSpawners = UnityEngine.Resources.FindObjectsOfTypeAll<MultiSpawner>();
            Dictionary<string, MultiSpawnerData> Dict = new Dictionary<string, MultiSpawnerData>();
            foreach (var multiSpawner in multiSpawners)
            {
                try
                {
                    Dict.Add(multiSpawner.name, new MultiSpawnerData().Create(multiSpawner));
                }
                catch { }
            }
            return Dict;
        }

        void SetMultiSpawnerData()
        {
            var multiSpawners = UnityEngine.Resources.FindObjectsOfTypeAll<MultiSpawner>();
            Dictionary<string, MultiSpawnerData> Dict = cfg.MultiSpawners;
            foreach (var multiSpawner in multiSpawners)
            {
                if (AirDropManager && multiSpawner.name.Contains("AirdropSpawner"))
                    continue;

                if (Dict.ContainsKey(multiSpawner.name))
                {
                    MultiSpawnerData multiSpawnerData = Dict[multiSpawner.name];
                    multiSpawner.ChildToSelf = multiSpawnerData.ChildToSelf;
                    multiSpawner.InitialWaitTime = multiSpawnerData.InitialWaitTime;
                    multiSpawner.MinimumSpawnCount = multiSpawnerData.MinimumSpawnCount;
                    multiSpawner.Offset = multiSpawnerData.Offset;
                    multiSpawner.SecondsPerSpawn = multiSpawnerData.SecondsPerSpawn;
                    multiSpawner.SpawnedLimit = multiSpawnerData.SpawnedLimit;
                    multiSpawner.Spawns = multiSpawnerData.Spawns;
                }
            }
        }

        #endregion

        #region TerritoryMarker

        private Dictionary<string, TerritoryControlData> GetTerritorySpawners()
        {
            var markersSpawner = Resources.FindObjectsOfTypeAll<LevelTerritoryControlMarkerSpawner>();
            Dictionary<string, TerritoryControlData> Dict = new Dictionary<string, TerritoryControlData>();
            foreach (var marker in markersSpawner)
            {
                Dict.Add(marker.name, new TerritoryControlData().Create(marker));
            }
            return Dict;
        }

        void SetTerritorySpawners()
        {
            var markersSpawner = Resources.FindObjectsOfTypeAll<LevelTerritoryControlMarkerSpawner>();
            Dictionary<string, TerritoryControlData> Dict = cfg.TerritorySpawners;
            foreach (var marker in markersSpawner)
            {
                if (Dict.ContainsKey(marker.name))
                {
                    TerritoryControlData markerSpawnerData = Dict[marker.name];
                    marker.TerritoryName = markerSpawnerData.TerritoryName;
                }
            }
        }

        #endregion

        #region JsonConvert

        private class DynamicContractResolver : DefaultContractResolver
        {
            private static bool IsAllowed(JsonProperty property)
            {
                if (property.PropertyType == typeof(SpawnConfiguration[])) property.Ignored = false;
                if (property.DeclaringType == typeof(SpawnConfiguration) && property.PropertyName.Equals("CellData")) property.Ignored = true;
                return property.PropertyType.IsPrimitive
                       || property.PropertyType == typeof(string)
                       || property.PropertyType == typeof(string[])
                       || property.PropertyType == typeof(MultiSpawner)
                       || property.PropertyType == typeof(ResourceSpawner)
                       //|| property.PropertyType == typeof(ProbabilityBasedResourceSpawner)
                       || property.PropertyType == typeof(SpawnConfiguration[])
                       || property.PropertyType == typeof(Spawn)
                       || property.PropertyType == typeof(GameObject)
                       || property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(List<>)
                       || property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>)
                       || property.PropertyType == typeof(Vector3);
            }

            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                var properties = base.CreateProperties(type, memberSerialization);
                return properties.Where(p => (p.DeclaringType == type || p.DeclaringType == typeof(MultiSpawner)) && IsAllowed(p)).ToList();
            }
        }

        private class UnityVector3Converter : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var vector = (Vector3)value;
                writer.WriteValue($"{vector.x} {vector.y} {vector.z}");
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.String)
                {
                    var values = reader.Value.ToString().Trim().Split(' ');
                    return new Vector3(Convert.ToSingle(values[0]), Convert.ToSingle(values[1]), Convert.ToSingle(values[2]));
                }
                var o = JObject.Load(reader);
                return new Vector3(Convert.ToSingle(o["x"]), Convert.ToSingle(o["y"]), Convert.ToSingle(o["z"]));
            }

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Vector3);
            }
        }

        #endregion
    }
}
