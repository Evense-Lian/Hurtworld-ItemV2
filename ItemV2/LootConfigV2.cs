// Reference: UnityEngine
using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("LootConfig для Itemv2", "Kaidoz", 1.3)]
    internal class LootConfigV2 : HurtworldPlugin
    {
        private ConfigData _config;
        private readonly FieldInfo testField = typeof(RuntimeHurtDB).GetField("_objectDatabase", BindingFlags.NonPublic | BindingFlags.Instance);

        public class ConfigData
        {
            public float GlobalStackSizeMultiplier { get; set; } = 1;
            public int Version { get; set; }
            public GroupAndItemConfig LootConfig { get; set; } = new GroupAndItemConfig();
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

        private new void LoadDefaultConfig() { }

        private new void LoadConfig()
        {
            try
            {
                Config.Settings = new JsonSerializerSettings()
                {
                    Formatting = Formatting.Indented,
                    DefaultValueHandling = DefaultValueHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                };
                _config = Config.ReadObject<ConfigData>();
            }
            catch (Exception ex)
            {
                Puts("Config load failed: {0}{1}{2}", ex.Message, Environment.NewLine, ex.StackTrace);
            }
        }

        private void OnServerInitialized()
        {
            AddCovalenceCommand("reloot", "CmdReloot");
            NextTick(InitWhenLoaded);
        }

        HashSet<string> modifiedGuids = new HashSet<string>();

        void InitWhenLoaded()
        {
            try
            {
                LoadConfig();
                CheckConfig();
                ChangeRatesOnFly();
            }
            catch (Exception ex)
            {
                PrintError(ex.Message);
            }

        }

        private void CmdReloot(IPlayer player, string command, string[] args)
        {
            LoadConfig();
            CheckConfig();
            ChangeRatesOnFly();
        }

        private void CheckConfig()
        {
            //if (_config.Version == GameManager.PROTOCOL_VERSION)

            //PrintError("Версия игры устарела {gameversion}.".Replace("{gameversion}", GameManager.PROTOCOL_VERSION.ToString()));
            try
            {
                CreateDefaultConfig();
            }
            catch(Exception ex) {

                Puts(ex.ToString());
            }
        }

        private void CreateDefaultConfig()
        {
            if (_config != null)
            {
                PrintWarning("Creating a new config file for " + this.Title);
            }
            Config.Clear();

            var config = CollectTreeLoot();
            _config = new ConfigData()
            {
                Version = GameManager.PROTOCOL_VERSION,
                LootConfig = config
            };

            try
            {
                Config.Settings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    DefaultValueHandling = DefaultValueHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                };
                Config.WriteObject(_config);
            }
            catch (Exception e)
            {
                PrintError("Failed to save config file: {0}{1}{2}", e.Message, Environment.NewLine, e.StackTrace);
            }
        }



        #region ReadConfigValues
        private GroupAndItemConfig CollectTreeLoot()
        {
            var config = new GroupAndItemConfig();
            foreach (var lt in Singleton<RuntimeHurtDB>.Instance.GetOrderedAssetsAssignableToType<LootTree>())
            {
                try
                {
                    if (lt.name.ToLower().Contains("testnode") || lt.name.ToLower().Contains("recipe") || lt.name.ToLower().Contains("cost") || lt.name.ToLower().Contains("spawn") || lt.name.ToLower().Contains("builder"))
                    {
                        PrintWarning($"{lt.name} не в лутконфиге игры");
                        continue;
                    }
                    var gc = new GroupAndItemConfig()
                    {
                        Note = lt.name
                    };
                    CollectTreeLootConfig(lt.Root, ref gc);
                    if (config.List == null) config.List = new List<GroupAndItemConfig>();
                    config.List.Add(gc);
                }
                catch { }
            }
            return config;
        }

        private void CollectTreeLootConfig(LootTreeNodeRollGroup root, ref GroupAndItemConfig lootconfig)
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
            if (lootconfig.List == null) lootconfig.List = new List<GroupAndItemConfig>();
            lootconfig.List.Add(gc);
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

        #endregion

        #region SetConfigValues

        private static int depth = 0;
        private void ChangeRatesOnFly()
        {
            var lootConfig = _config.LootConfig;
            var ltree = Singleton<RuntimeHurtDB>.Instance.GetOrderedAssetsAssignableToType<LootTree>();
            foreach (var ch in lootConfig.List)
            {
                foreach (var lt in ltree)
                {
                    if (ch.Note == lt.name)
                    {
                        lt.Root.RollAll = ch.RollAll;
                        lt.Root.RollCount = ch.RollCount;
                        lt.Root.RollWithoutReplacement = ch.RollWithoutReplacement;
                        lt.Root.ProbabilityCache = ch.ProbabilityCache;
                        lt.Root.SiblingProbability = ch.SiblingProbability;
                        lt.Root.FailsPerSuccess = ch.FailsPerSuccess;

                        SetLootTreeConfig(ch, lt);

                        break;
                    }
                }
            }
        }

        private bool SetLootTreeConfig(GroupAndItemConfig config, LootTree root)
        {
            depth++;

            bool modified = false;
            foreach (var ch in config.List)
            {
                depth++;
                modified = SubSetLootTreeConfig(ch, root.Root);
                depth--;
                if (modified)
                    break;

            }
            depth--;
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
                        depth++;
                        bool m = SubSetLootTreeConfig(ch, child);
                        depth--;
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
                    depth++;
                    bool m = SubSetLootTreeConfig(ch, ltnst.LootTree.Root);
                    depth--;
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
                    var value = config.Mutliplier * (config.Force ? 1 : _config.GlobalStackSizeMultiplier);
                    p.Mutliplier = value;
                }
                return true;
            }

            var itemgenerator = root as LootTreeNodeItemGenerator;
            if (itemgenerator != null)
            {
                if (config.GUID != itemgenerator.Guid)
                    return false;
                var value1 = Mathf.CeilToInt((float)config.MinDrop * (config.Force ? 1 : _config.GlobalStackSizeMultiplier));
                var value2 = Mathf.CeilToInt((float)config.MaxDrop * (config.Force ? 1 : _config.GlobalStackSizeMultiplier));
                itemgenerator.MinStack = value1;
                itemgenerator.MaxStack = value2;
                return true;
            }
            return false;
        }
        #endregion
    }
}
