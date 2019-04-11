using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("AirDropManager", "Kaidoz", "1.0.0")]
    [Description("AirDropManager for ItemV2")]
    public class AirDropManager : HurtworldPlugin
    {
        System.Random random = new System.Random();
        Dictionary<GameObject, MapMarkerData> markes = new Dictionary<GameObject, MapMarkerData>();
        public CFG cfg;
        public CFGLoot cfgLoot;

        public class CFG
        {
            [JsonProperty("Main")]
            public Main main;

            public class Main
            {
                [JsonProperty("Loot Multiplier")]
                public int lootMultiplier;

                [JsonProperty("Show message")]
                public bool showmessage;

                [JsonProperty("Minimum online for Air")]
                public int minCountPlayer;

                [JsonProperty("AirDrop Repeat(min.)")]
                public int time;

                [JsonProperty("Items")]
                public Items items;

                [JsonProperty("Marker")]
                public Marker marker;

                public class Items
                {
                    [JsonProperty("Replace items")]
                    public bool ReplaceItems;

                    [JsonProperty("Max count items")]
                    public int maxItems;

                    [JsonProperty("Min count items")]
                    public int minItems;

                    [JsonProperty("Item list")]
                    public List<ItemInstance> items;

                    public class ItemInstance
                    {
                        [JsonProperty("Item name")]
                        public string ItemName;

                        [JsonProperty("Min. Count")]
                        public int minCount;

                        [JsonProperty("Max. Count")]
                        public int maxCount;
                    }
                }

                public class Marker
                {
                    [JsonProperty("Enabled")]
                    public bool enabled;

                    [JsonProperty("Show in compass")]
                    public bool compas;

                    [JsonProperty("Scale")]
                    public float scale;

                    [JsonProperty("Color")]
                    public string color;

                    [JsonProperty("Label for Marker")]
                    public string label;
                }
            }

            [JsonProperty("AirDrop Points")]
            public List<CFG.Spawn> Spawns;

            public class Spawn
            {
                public string Position;

                public float Radius;
            }
        }

        public class CFGLoot
        {
            public GroupAndItemConfig lootTree;

            public Dictionary<string, GroupAndItemConfig> lootTreeBiomes;
        }

        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"CargoShow", "<color=gray><b>[RadioTower]</b></color> Замечен самолет!"},
                {"CargoDrop", "<color=gray<b>[RadioTower]</b></color> Самолет сбросил груз {biome}"},
                {"StartingDesertT1", "в начальном биоме!" },
                {"Forest", "в лесном биоме!" },
                {"YellowDunes", "в песчаном биоме!" },
                {"RedDesert", "в красной пустыне!" },
                {"Snow", "в снежном биоме!" }
            }, this, "ru");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                 {"CargoShow", "<color=gray<b>[RadioTower]</b></color> Plane in sight!"},
                 {"CargoDrop", "<color=gray><b>[RadioTower]</b></color> Plane dropped loot {biome}"},
                 {"StartingDesertT1", "in the starting biome!" },
                 {"Forest", "in the forest biome!" },
                 {"YellowDunes", "in the sand biome!" },
                 {"RedDesert", "in the red desert!" },
                 {"Snow", "in the snow biome!" }
            }, this);
        }

        public class GroupAndItemConfig
        {
            public string GUID;
            public bool Force;
            public float ProbabilityCache;
            public float SiblingProbability;
            public float FailsPerSuccess;
            public string Note;
            public bool RollAll;
            public bool RollWithoutReplacement;
            public int RollCount;
            public float LevelMin;
            public float LevelMax;
            public string Name;
            public int StackSize;
            public int MinDrop;
            public int MaxDrop;
            public float Mutliplier;
            public List<GroupAndItemConfig> List;
        }

        bool initServer = false;

        void OnServerInitialized()
        {
            NextTick(LoadPl);
        }

        void LoadPl()
        {
            initServer = true;
            LoadConfig();
            LoadRate();
            setSpawnList();
            RepeatAir();
        }

        #region Config

        private new void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                cfg = Config.ReadObject<CFG>($"{Interface.Oxide.ConfigDirectory}/{Name}.json");
                if (cfg == null) throw new Exception("CFG");
                cfgLoot = Config.ReadObject<CFGLoot>($"{Interface.Oxide.ConfigDirectory}/{Name}Loot.json");
                if (cfgLoot == null) throw new Exception("CFGLoot");
            }
            catch (Exception ex)
            {
                Puts(ex.ToString());
                LoadDefaultConfig();
            }
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
            Config.WriteObject(cfg, false, $"{Interface.Oxide.ConfigDirectory}/{Name}.json");
            if (Config.Exists($"{Interface.Oxide.ConfigDirectory}/{Name}Loot.json"))
                Config.WriteObject(cfgLoot, false, $"{Interface.Oxide.ConfigDirectory}/{Name}Loot.Backupjson");
            Config.WriteObject(cfgLoot, false, $"{Interface.Oxide.ConfigDirectory}/{Name}Loot.json");
        }

        protected override void LoadDefaultConfig()
        {
            if (!initServer)
                return;

            PrintWarning("Creating new config file for AirDropManager...");
            LoadDefaultCFG();
            LoadDefaultLoot();
        }

        void LoadDefaultCFG()
        {
            cfg = new CFG()
            {
                main = new CFG.Main()
                {
                    items = new CFG.Main.Items()
                    {
                        maxItems = 2,
                        minItems = 1,
                        ReplaceItems = false,
                        items = new List<CFG.Main.Items.ItemInstance>()
                        {
                            new CFG.Main.Items.ItemInstance()
                            {
                                ItemName = "Amber",
                                minCount = 5,
                                maxCount = 10
                            }
                        }
                    },
                    time = 1,
                    lootMultiplier = 1,
                    marker = new CFG.Main.Marker()
                    {
                        color = "gray",
                        compas = true,
                        enabled = true,
                        scale = 100f,
                        label = "AirDrop"
                    },
                    minCountPlayer = 1,
                    showmessage = true
                },
                Spawns = getSpawnList(),
            };
        }

        void RepeatAir()
        {
            timer.Repeat(cfg.main.time * 60f, 0, delegate
            {
                try
                {
                    AirStart();
                }
                catch { }
            });
        }

        void LoadDefaultLoot()
        {
            var Air = Resources.FindObjectsOfTypeAll<AirDropEvent>();
            foreach (var air in Air)
            {
                if (air != null && air.Loot != null)
                    cfgLoot = new CFGLoot()
                    {
                        lootTree = LootTree(air.Loot),
                        lootTreeBiomes = GetBiomesLoot(air)
                    };
            }
        }

        void LoadRate(AirDropEvent ai = null)
        {
            var Air = Resources.FindObjectsOfTypeAll<AirDropEvent>();
            foreach (var air in Air)
            {
                if (air.BiomeLoot != null)
                {
                    foreach (var biome in air.BiomeLoot)
                    {
                        if (cfgLoot.lootTreeBiomes.ContainsKey(biome.Biomes.First().name))
                        {
                            var data = cfgLoot.lootTreeBiomes[biome.Biomes.First().name];
                            ChangeRates(data, biome.Loot);
                        }
                    }
                }
                if (air.Loot != null)
                {
                    var data = cfgLoot.lootTree;
                    ChangeRates(data, air.Loot);
                }
            }
        }

        #endregion

        #region Command

        [ConsoleCommand("airdrop.start")]
        private void AirStart(string command, string[] args)
        {
            if (args.Length == 1)
            {

                AirStart(args[0]);
                return;
            }
            AirStart();
        }

        #endregion

        #region Hooks

        bool EnableSpawn = false;

        void OnEntitySpawned(HNetworkView data)
        {
            if (data.name.Contains("CargoPlane"))
            {
                if (!EnableSpawn)
                {
                    Singleton<HNetworkManager>.Instance.NetDestroy(data.HNetworkView());
                    return;
                }
                if (cfg.main.showmessage)
                {
                    BroadcastChat(GetMsg("CargoShow"));
                }
                EnableSpawn = false;
            }
        }

        void OnAirdrop(GameObject obj, AirDropEvent air, List<ItemObject> items)
        {
            if (GameManager.Instance._activePlayerCount < cfg.main.minCountPlayer)
            {
                Singleton<HNetworkManager>.Instance.NetDestroy(obj.HNetworkView());
                return;
            }
            if (cfg.main.items.ReplaceItems)
            {
                if (cfg.main.items.items.Count != 0)
                    items = getItems(cfg.main.items.items);
            }
            else
            {
                foreach (var item in getItems(cfg.main.items.items))
                    items.Add(item);
            }
            if (cfg.main.marker.enabled)
            {
                markes.Add(obj, CreateMarker(obj.transform.position));
                obj.AddComponent<MarkerLoot>();
            }
            string biome = getBiome(obj.transform.position);
            BroadcastChat(GetMsg("CargoDrop").Replace("{biome}", GetMsg(biome)).Replace("{DarkPluginsId}", air.name));
        }

        void DestroyLoot(GameObject obj)
        {
            MapManagerServer.Instance.DeregisterMarker(markes[obj]);
        }

        #endregion

        private void AirStart(string steamid = "")
        {
            EnableSpawn = true;
            var Air = Resources.FindObjectsOfTypeAll<AirDropEvent>().FirstOrDefault();
            Air.OnNetInstantiate();
        }

        #region MonoBehaviur

        public class MarkerLoot : MonoBehaviour
        {
            void OnDestroy()
            {
                Interface.GetMod().CallHook("DestroyLoot", gameObject);
            }
        }

        #endregion

        List<CFG.Spawn> getSpawnList()
        {
            var multiSpawners = UnityEngine.Resources.FindObjectsOfTypeAll<MultiSpawner>();
            foreach (var multiSpawner in multiSpawners)
            {
                if (multiSpawner.name.Contains("Air"))
                {
                    if (multiSpawner.Spawns != null)
                    {
                        var lst = multiSpawner.Spawns;
                    }
                }
            }
            return null;
        }

        void setSpawnList()
        {
            var multiSpawners = UnityEngine.Resources.FindObjectsOfTypeAll<MultiSpawner>();
            foreach (var multiSpawner in multiSpawners)
            {
                if (multiSpawner.name.Contains("Air"))
                {
                    multiSpawner.Spawns = ConvertFromCFG(cfg.Spawns);
                }
            }
        }

        Dictionary<string, GroupAndItemConfig> GetBiomesLoot(AirDropEvent air)
        {
            Dictionary<string, GroupAndItemConfig> lootTreeBiomes = new Dictionary<string, GroupAndItemConfig>();
            GroupAndItemConfig group = new GroupAndItemConfig();
            foreach (var d in air.BiomeLoot)
            {
                var loot = LootTree(d.Loot);
                group = loot;
                lootTreeBiomes.Add(d.Biomes.First().name, LootTree(d.Loot));
            }

            foreach (string d in getBiomes())
            {
                if (!lootTreeBiomes.ContainsKey(d))
                {
                    lootTreeBiomes.Add(d, group);
                }
            }

            return lootTreeBiomes;
        }

        GroupAndItemConfig LootTree(LootTree lt)
        {
            var gc = new GroupAndItemConfig()
            {
                Note = lt.name
            };
            CollectTreeLootConfig(lt.Root, ref gc);
            return gc;
        }

        private LootTree GetLootTreeForBiome(LootTree lt, Vector3 drop, BiomeCellData biome, AirDropEvent.BiomeBoundLoot[] BiomeLoot)
        {
            LevelSingleton<BiomeManager>.LevelInstance.FillGenerateBiomeData(drop.xz(), biome);
            HurtBiome item = null;
            float num = 0f;
            foreach (KeyValuePair<HurtBiome, BiomeCellData.BiomeStrengthData> keyValuePair in biome.Result)
            {
                if (keyValuePair.Value.BlendMode != HurtBiomeGridMapping.EBiomeBlendMode.Overlay)
                {
                    Dictionary<HurtBiome, BiomeCellData.BiomeStrengthData>.Enumerator enumerator = new Dictionary<HurtBiome, BiomeCellData.BiomeStrengthData>.Enumerator();
                    KeyValuePair<HurtBiome, BiomeCellData.BiomeStrengthData> keyValuePair2 = enumerator.Current;
                    if (keyValuePair2.Value.Value > num)
                    {
                        KeyValuePair<HurtBiome, BiomeCellData.BiomeStrengthData> keyValuePair3 = enumerator.Current;
                        num = keyValuePair3.Value.Value;
                        KeyValuePair<HurtBiome, BiomeCellData.BiomeStrengthData> keyValuePair4 = enumerator.Current;
                        item = keyValuePair4.Key;
                    }
                }
            }
            for (int i = 0; i < BiomeLoot.Length; i++)
            {
                if (BiomeLoot[i].Biomes.Contains(item))
                {
                    return BiomeLoot[i].Loot;
                }
            }
            return lt;
        }

        private void CollectTreeLootConfig(LootTreeNodeRollGroup root, ref GroupAndItemConfig ltcfg)
        {
            var gc = new GroupAndItemConfig()
            {
                GUID = root.Guid,
                Note = root.Note,
                RollAll = root.RollAll,
                RollWithoutReplacement = root.RollWithoutReplacement,
                RollCount = root.RollCount,
                ProbabilityCache = root.ProbabilityCache,
                SiblingProbability = root.SiblingProbability,
                FailsPerSuccess = root.FailsPerSuccess,
            };
            foreach (var child in root.Children)
                SubCollectTreeLootConfig(child, ref gc);
            if (ltcfg.List == null) ltcfg.List = new List<GroupAndItemConfig>();
            ltcfg.List.Add(gc);
        }

        private void SubCollectTreeLootConfig(LootTreeNodeBase child, ref GroupAndItemConfig gconfig)
        {
            var ltnrg = child as LootTreeNodeRollGroup;
            if (ltnrg != null)
            {
                var gc = new GroupAndItemConfig()
                {
                    GUID = ltnrg.Guid,
                    Note = ltnrg.Note,
                    RollAll = ltnrg.RollAll,
                    RollWithoutReplacement = ltnrg.RollWithoutReplacement,
                    RollCount = ltnrg.RollCount
                };
                foreach (var ch in ltnrg.Children)
                    SubCollectTreeLootConfig(ch, ref gc);
                if (gconfig.List == null) gconfig.List = new List<GroupAndItemConfig>();
                gconfig.List.Add(gc);
                return;
            }

            var ltnst = child as LootTreeNodeSubtree;
            if (ltnst != null)
            {
                var gc = new GroupAndItemConfig()
                {
                    GUID = ltnst.Guid,
                    ProbabilityCache = ltnst.ProbabilityCache,
                    SiblingProbability = ltnst.SiblingProbability,
                    FailsPerSuccess = ltnst.FailsPerSuccess
                };
                SubCollectTreeLootConfig(ltnst.LootTree.Root, ref gc);
                if (gconfig.List == null) gconfig.List = new List<GroupAndItemConfig>();
                gconfig.List.Add(gc);
                return;
            }

            var ltnitemgenerator = child as LootTreeNodeItemGeneratorContainer;
            if (ltnitemgenerator != null)
            {
                var gc = new GroupAndItemConfig()
                {
                    GUID = ltnitemgenerator.Guid,
                    ProbabilityCache = ltnitemgenerator.ProbabilityCache,
                    SiblingProbability = ltnitemgenerator.SiblingProbability,
                    FailsPerSuccess = ltnitemgenerator.FailsPerSuccess,
                };
                SubCollectTreeLootConfig(ltnitemgenerator.ChildTree, ref gc);
                if (gconfig.List == null) gconfig.List = new List<GroupAndItemConfig>();
                gconfig.List.Add(gc);
                return;
            }

            var ltsfn = child as LootTreeSourceFilterNode;
            if (ltsfn != null)
            {
                if (ltsfn.LootTree != null)
                {
                    var gc = new GroupAndItemConfig()
                    {
                        GUID = ltsfn.Guid,
                        ProbabilityCache = ltsfn.ProbabilityCache,
                        SiblingProbability = ltsfn.SiblingProbability,
                        FailsPerSuccess = ltsfn.FailsPerSuccess,
                        LevelMin = ltsfn.LevelMin,
                        LevelMax = ltsfn.LevelMax,
                    };
                    SubCollectTreeLootConfig(ltsfn.LootTree, ref gc);
                    if (gconfig.List == null) gconfig.List = new List<GroupAndItemConfig>();
                    gconfig.List.Add(gc);
                }
                return;
            }

            var ltniga = child as LootTreeNodeItemGeneratorAdvanced;
            if (ltniga != null)
            {
                var p = ltniga.StackSize as MutatorGeneratorFloatFromReferenceCurve;
                if (p != null)
                {
                    if (gconfig.List == null) gconfig.List = new List<GroupAndItemConfig>();
                    gconfig.List.Add(new GroupAndItemConfig()
                    {
                        GUID = ltniga.Guid,
                        Name = ltniga.LootResult?.name,
                        Mutliplier = p.Mutliplier
                    });
                }
                return;
            }

            var itemgenerator = child as LootTreeNodeItemGenerator;
            if (itemgenerator != null)
            {
                if (gconfig.List == null) gconfig.List = new List<GroupAndItemConfig>();
                gconfig.List.Add(new GroupAndItemConfig()
                {
                    GUID = itemgenerator.Guid,
                    Name = itemgenerator.LootResult?.name,
                    MinDrop = itemgenerator.MinStack,
                    MaxDrop = itemgenerator.MaxStack
                });
                return;
            }
        }

        private LootTree ChangeRates(GroupAndItemConfig gaic, LootTree lt)
        {
            var ltree = Singleton<RuntimeHurtDB>.Instance.GetOrderedAssetsAssignableToType<LootTree>();
            if (gaic.Note == lt.name)
            {
                lt.Root.RollAll = gaic.RollAll;
                lt.Root.RollCount = gaic.RollCount;
                lt.Root.RollWithoutReplacement = gaic.RollWithoutReplacement;
                lt.Root.ProbabilityCache = gaic.ProbabilityCache;
                lt.Root.SiblingProbability = gaic.SiblingProbability;
                lt.Root.FailsPerSuccess = gaic.FailsPerSuccess;

                SetLootTreeConfig(gaic, lt);
            }
            return lt;
        }

        private bool SetLootTreeConfig(GroupAndItemConfig config, LootTree root)
        {
            bool modified = false;
            foreach (var ch in config.List)
            {
                modified = SubSetLootTreeConfig(ch, root.Root);
                if (modified)
                    break;

            }
            return false;
        }

        private bool SubSetLootTreeConfig(GroupAndItemConfig config, LootTreeNodeBase root)
        {
            var ltnrg = root as LootTreeNodeRollGroup;
            if (ltnrg != null)
            {
                if (config.GUID != ltnrg.Guid)
                    return false;

                ltnrg.RollAll = config.RollAll;
                ltnrg.RollWithoutReplacement = config.RollWithoutReplacement;
                ltnrg.RollCount = config.RollCount;

                bool modified = false;
                if (config.List == null)
                    return modified;
                foreach (var ch in config.List)
                {
                    foreach (var child in ltnrg.Children)
                    {
                        bool m = SubSetLootTreeConfig(ch, child);
                        modified |= m;
                        if (m) break;
                    }
                }
                return modified;
            }


            var ltnst = root as LootTreeNodeSubtree;
            if (ltnst != null)
            {
                if (config.GUID != ltnst.Guid)
                    return false;

                ltnst.ProbabilityCache = config.ProbabilityCache;
                ltnst.SiblingProbability = config.SiblingProbability;
                ltnst.FailsPerSuccess = config.FailsPerSuccess;

                bool modified = false;
                if (config.List == null)
                    return modified;
                foreach (var ch in config.List)
                {
                    bool m = SubSetLootTreeConfig(ch, ltnst.LootTree.Root);
                    modified |= m;
                    if (m) break;
                }

                return modified;
            }

            var ltniga = root as LootTreeNodeItemGeneratorAdvanced;
            if (ltniga != null)
            {
                if (config.GUID != ltniga.Guid)
                    return false;
                var p = ltniga.StackSize as MutatorGeneratorFloatFromReferenceCurve;
                if (p != null)
                {
                    var value = config.Mutliplier * cfg.main.lootMultiplier;
                    p.Mutliplier = value;
                }
                return true;
            }

            var itemgenerator = root as LootTreeNodeItemGenerator;
            if (itemgenerator != null)
            {
                if (config.GUID != itemgenerator.Guid)
                    return false;
                var value1 = Mathf.CeilToInt((float)config.MinDrop * cfg.main.lootMultiplier);
                var value2 = Mathf.CeilToInt((float)config.MaxDrop * cfg.main.lootMultiplier);
                itemgenerator.MinStack = value1;
                itemgenerator.MaxStack = value2;
                return true;
            }
            return false;
        }

        List<string> getBiomes()
        {
            List<string> biomes = new List<string>();
            var biomesManager = UnityEngine.Resources.FindObjectsOfTypeAll<BiomeManager>();
            foreach (var data in biomesManager)
                foreach (var d in data.Data.BiomeGridDictionary)
                    if (d.Value.BlendMode != HurtBiomeGridMapping.EBiomeBlendMode.Overlay)
                        biomes.Add(d.Key.name);

            return biomes;
        }

        List<ItemObject> getItems(List<CFG.Main.Items.ItemInstance> items)
        {
            List<ItemObject> _items = new List<ItemObject>();
            int d = 0;
            int max = random.Next(cfg.main.items.minItems, cfg.main.items.maxItems);
            foreach (var it in items)
            {
                d++;
                _items.Add(GlobalItemManager.Instance.CreateItem(getItemFromName(it.ItemName), random.Next(it.minCount, it.maxCount)));
                if (d >= max)
                    break;
            }
            return _items;
        }

        public ItemGeneratorAsset getItemFromName(string name)
        {
            foreach (var item in GlobalItemManager.Instance.GetGenerators())
            {
                if (item.Value.name == name || item.Value.GeneratorId.ToString() == name)
                    return item.Value;
            }
            return GlobalItemManager.Instance.GetGenerators()[2];
        }

        public string getBiome(Vector3 position)
        {
            BiomeCellData _cellDataBuffer = new BiomeCellData();
            LevelSingleton<BiomeManager>.LevelInstance.FillGenerateBiomeData(position.xz(), _cellDataBuffer);
            HurtBiome hurtBiome = null;
            float num = 0f;
            var enumerator = _cellDataBuffer.Result;
            foreach (var enn in enumerator)
            {
                if (enn.Value.BlendMode != HurtBiomeGridMapping.EBiomeBlendMode.Overlay && enn.Value.Value > (double)num)
                {
                    hurtBiome = enn.Key;
                    return hurtBiome.name;
                }
            }
            return string.Empty;
        }

        MapMarkerData CreateMarker(Vector3 position)
        {
            MapMarkerData marker = new MapMarkerData();
            marker.Label = cfg.main.marker.label;
            marker.Position = position;
            marker.Scale = new Vector3(cfg.main.marker.scale, cfg.main.marker.scale);
            marker.Color = Color.white;
            marker.ShowInCompass = cfg.main.marker.compas;
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
                return Color.white;
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


        List<CFG.Spawn> ConvertFromSpawn(List<Spawn> spawns)
        {
            List<CFG.Spawn> list = new List<CFG.Spawn>();
            foreach (var li in spawns)
                list.Add(new CFG.Spawn()
                {
                    Position = li.Position.ToString().Replace("(", "").Replace(")", ""),
                    Radius = li.Range
                });

            return list;
        }

        List<Spawn> ConvertFromCFG(List<CFG.Spawn> spawns)
        {
            List<Spawn> list = new List<Spawn>();
            foreach (var li in spawns)
            {
                var d = li.Position.Split(" ".ToCharArray());
                Vector3 pos = new Vector3(Convert.ToSingle(d[0]), Convert.ToSingle(d[1]), Convert.ToSingle(d[2]));
                list.Add(new Spawn()
                {
                    Position = pos,
                    Range = li.Radius
                });
            }

            return list;
        }

        string GetMsg(string key, string userID = "") => lang.GetMessage(key, this, userID);

        void BroadcastChat(string msg) => Server.Broadcast(msg);

        void Send(PlayerSession session, string msg) => hurt.SendChatMessage(session, null, msg);

        #region ForConfig

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
