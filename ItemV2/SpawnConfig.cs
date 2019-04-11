using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

using Oxide.Core;

using UnityEngine;

namespace Oxide.Plugins
{
    [Info("SpawnConfig", "Nogrod", "1.0.4")]
    internal class SpawnConfig : HurtworldPlugin
    {
        private const int VersionConfig = 2;
        private ConfigData _config;

        private new bool LoadConfig()
        {
            try
            {
                Config.Settings = new JsonSerializerSettings
                {
                    Converters =
                    {
                        new UnityVector3Converter(),
                        new UnityGameObjectConverter(),
                        new StringEnumConverter()
                    }
                };
                if (!Config.Exists())
                    return CreateDefaultConfig();
                _config = Config.ReadObject<ConfigData>();
            }
            catch (Exception e)
            {
                Puts("Config load failed: {0}{1}{2}", e.Message, Environment.NewLine, e.StackTrace);
                return false;
            }
            return true;
        }

        private new void LoadDefaultConfig()
        {
        }

        private bool CreateDefaultConfig()
        {
            Config.Clear();

            var multiSpawners = UnityEngine.Resources.FindObjectsOfTypeAll<MultiSpawner>();
            var resourceSpawners = UnityEngine.Resources.FindObjectsOfTypeAll<ResourceSpawner>();

            try
            {
                Config.Settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    Converters = new List<JsonConverter>
                    {
                        new StringEnumConverter(),
                        new UnityVector3Converter(),
                        new UnityGameObjectConverter()
                    },
                    ContractResolver = new DynamicContractResolver()
                };
                Config.WriteObject(new ExportData
                {
                    Version = GameManager.PROTOCOL_VERSION,
                    VersionConfig = VersionConfig,
                    MultiSpawners = multiSpawners.ToDictionary(m => m.name)
                });
            }
            catch (Exception e)
            {
                Puts("Config save failed: {0}{1}{2}", e.Message, Environment.NewLine, e.StackTrace);
                return false;
            }
            Puts("Created new config");
            return LoadConfig();
        }

        private void CheckConfig()
        {
            if (_config.Version == GameManager.PROTOCOL_VERSION && _config.VersionConfig == VersionConfig) return;
            Puts("Incorrect config version({0}/{1})", _config.Version, _config.VersionConfig);
            if (_config.Version > 0) Config.WriteObject(_config, false, $"{Config.Filename}.old");
            CreateDefaultConfig();
        }

        private void OnServerInitialized()
        {
            if (!LoadConfig())
                return;
            CheckConfig();
            UpdateSpawns();
        }

        private void UpdateSpawns()
        {
            var multiSpawners = UnityEngine.Resources.FindObjectsOfTypeAll<MultiSpawner>();
            foreach (var multiSpawner in multiSpawners)
            {
                try
                {
                    MultiSpawnerData multiSpawnerData;
                    if (!_config.MultiSpawners.TryGetValue(multiSpawner.name, out multiSpawnerData))
                    {
                        Puts("MultiSpawnerData '{0}' not found, skipped.", multiSpawner.name);
                        continue;
                    }
                    multiSpawner.ChildToSelf = multiSpawnerData.ChildToSelf;
                    multiSpawner.InitialWaitTime = multiSpawnerData.InitialWaitTime;
                    multiSpawner.MinimumSpawnCount = multiSpawnerData.MinimumSpawnCount;
                    multiSpawner.Offset = multiSpawnerData.Offset;
                    multiSpawner.SecondsPerSpawn = multiSpawnerData.SecondsPerSpawn;
                    multiSpawner.SpawnedLimit = multiSpawnerData.SpawnedLimit;
                    multiSpawner.Spawns = multiSpawnerData.Spawns;
                }
                catch { }
            }
        }

        [ConsoleCommand("spawn.reload")]
        private void cmdConsoleReload(string commandString)
        {
            if (!LoadConfig())
                return;
            CheckConfig();
            UpdateSpawns();
            Puts("Spawn config reloaded.");
        }

        #region Nested type: ConfigData

        public class ConfigData
        {
            public int Version { get; set; }
            public int VersionConfig { get; set; }
            public Dictionary<string, MultiSpawnerData> MultiSpawners { get; set; }
        }

        #endregion

        #region Nested type: ExportData
        public class ExportData
        {
            public int Version { get; set; }
            public int VersionConfig { get; set; }
            public Dictionary<string, MultiSpawner> MultiSpawners { get; set; }
            public Dictionary<string, ResourceSpawner> ResourceSpawners { get; set; }
        }
        #endregion

        #region Nested type: MultiSpawnerData
        public class MultiSpawnerData
        {
            public bool ChildToSelf { get; set; }
            public float SecondsPerSpawn { get; set; }
            public int SpawnedLimit { get; set; }
            public List<Spawn> Spawns { get; set; }
            public string[] Objects { get; set; }
            public Vector3 Offset { get; set; }
            public float InitialWaitTime { get; set; }
            public int MinimumSpawnCount { get; set; }
        }
        #endregion

        #region Nested type: LootSpawnerData
        public class LootSpawnerData
        {
            public bool ChildToSelf { get; set; }
            public float SecondsPerSpawn { get; set; }
            public float SpawnedLimit { get; set; }
            public Vector3 Offset { get; set; }
            public float InitialWaitTime { get; set; }
            public int MinimumSpawnCount { get; set; }
        }
        #endregion

        #region Nested type: CreatureSpawnDirectorData
        public class CreatureSpawnDirectorData
        {
            public List<SpawnConfigData> SpawnConfigs { get; set; }
            public bool InitializeRandomData { get; set; }
        }
        #endregion

        #region Nested type: SpawnConfigData
        public class SpawnConfigData
        {
            public GameObject Object { get; set; }
            public bool AlignToSurface { get; set; }
            public float FastestSpawnRateSeconds { get; set; }
            public CellSpawnRateMapping CellSpawnRates { get; set; }
            public bool IsDebug { get; set; }
        }
        #endregion

        #region Nested type: DynamicContractResolver
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

        #endregion

        #region Nested type: UnityVector3Converter
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

        #region Nested type: UnityGameObjectConverter
        private class UnityGameObjectConverter : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteValue(((GameObject)value).name);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var prefabName = reader.Value.ToString();
                var config = NetworkObjectPool.Instance.GetPrefabConfig(prefabName);
                if (config == null)
                {
                    Interface.Oxide.LogInfo("[{0}] Prefab config not found: {1}", nameof(SpawnConfig), prefabName);
                    var prefab = (GameObject)Resources.Load(prefabName);
                    if (prefab == null)
                        Interface.Oxide.LogInfo("[{0}] Prefab failed to load: {1}", nameof(SpawnConfig), prefabName);
                    return prefab;
                }
                return config.Server.Prefab.gameObject;
            }

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(GameObject);
            }
        }
        #endregion
    }
}
