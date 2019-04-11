using System;
using Oxide.Core;
using Oxide.Game;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("ClansRewardsPlus", "Kaidoz", "1.0.0")]
    class ClansRewardsPlus : HurtworldPlugin
    {
        public class Sett
        {
            public Clan clan;

            public Dictionary<string, int> biomeTotems;
        }

        public class Configuration
        {
            [JsonProperty("Руды")]
            public Disp disp;

            public class Disp
            {
                [JsonProperty("Увеличивать рейты добычи для руд")]
                public bool enable;

                [JsonProperty("Рейт")]
                public float rate;
            }

            [JsonProperty("Дрели")]
            public Drills drills;

            public class Drills
            {
                [JsonProperty("Увеличивать рейты добычи для дрелей")]
                public bool enable;

                [JsonProperty("Рейт")]
                public float rate;
            }

            [JsonProperty("Животные")]
            public Animals animals;

            public class Animals
            {
                [JsonProperty("Увеличивать рейты добычи для животных")]
                public bool enable;

                [JsonProperty("Рейт")]
                public float rate;
            }

            [JsonProperty("Показывать сообщение о увеличение рейтов")]
            public bool ShowMsgRate;
        }


        #region Config
        Configuration _config;

        protected override void SaveConfig() => Config.WriteObject(_config);

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating new config file for ClansRewardsPlus...");
            _config = new Configuration()
            {
                disp = new Configuration.Disp()
                {
                    enable = true,
                    rate = 0.5f
                },
                animals = new Configuration.Animals()
                {
                    enable = true,
                    rate = 0.5f
                },
                drills = new Configuration.Drills()
                {
                    enable = true,
                    rate = 0.5f
                },
                ShowMsgRate = true
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

        void Loaded()
        {
            LoadConfig();
        }

        void AddClansRates()
        {
            var listTerritotyMarkers = Resources.FindObjectsOfTypeAll<OwnershipStakeServer>()?
            .Where(
                    e =>
                        e.IsClanTotem)
                        .ToList() ?? new List<OwnershipStakeServer>();
            foreach (var st in listTerritotyMarkers)
            {
                if (st.AuthorizedClans.Count() != 0)
                {
                    string biome = getBiome(st.transform.position);
                    Clan clan = st.AuthorizedClans.First();
                    string guid = clan.ClanGuid;
                    if (!clansRates.ContainsKey(guid))
                        clansRates.Add(guid, new Sett()
                        {
                            clan = clan,
                            biomeTotems = new Dictionary<string, int>()
                            {
                                {
                                    biome,
                                    1
                                }
                            }
                        });
                    else
                    {

                        if (clansRates[guid].biomeTotems.ContainsKey(biome))
                            clansRates[guid].biomeTotems[biome]++;
                        else
                        {
                            clansRates[guid].biomeTotems.Add(biome, 1);
                        }
                    }
                }

                //marker.Data.
            }
        }

        Dictionary<string, Sett> clansRates = new Dictionary<string, Sett>();
        Dictionary<MapMarkerServer, Clan> clansMarkers = new Dictionary<MapMarkerServer, Clan>();

        string prefix = "<color=#2E9EFF>[ClansRates]</color> ";

        [ChatCommand("getrate")]
        private void commandRate(PlayerSession session, string cmd, string[] args)
        {
            if (session.Identity.Clan == null)
            {
                SendChat(session, prefix + $"У вас нет клана");
            }

            var newClan = session.Identity.Clan;
            string biome = getBiome(session.WorldPlayerEntity.transform.position);
            var cl = clansRates[newClan.ClanGuid];
            if (cl.biomeTotems.ContainsKey(biome))
                SendChat(session, prefix + $"Ваш бонус в этом биоме: {cl.biomeTotems[biome]}");
            else
                SendChat(session, prefix + $"У вас нет бонуса в этом биоме");
        }

        #region Hooks

        void OnPlayerAuthorize(PlayerSession session, GameObject obj, OwnershipStakeServer stake)
        {
            if (session.Identity.Clan == null || !stake.IsClanTotem)
                return;

            var newClan = session.Identity.Clan;
            string biome = getBiome(stake.transform.position);
            if(stake.AuthorizedClans.Count!=0)
            {
                var oldClan = stake.AuthorizedClans.First();
                if (clansRates.ContainsKey(oldClan.ClanGuid))
                {
                    var cl = clansRates[oldClan.ClanGuid];
                    if (cl.biomeTotems.ContainsKey(biome))
                        cl.biomeTotems[biome]--;
                }
            }

            if (clansRates.ContainsKey(newClan.ClanGuid))
            {
                var cl = clansRates[newClan.ClanGuid];
                if (cl.biomeTotems.ContainsKey(biome))
                    cl.biomeTotems[biome]++;
                else
                    cl.biomeTotems.Add(biome, 1);
            }
            else
            {
                clansRates.Add(newClan.ClanGuid, new Sett()
                {
                    clan = newClan,
                    biomeTotems = new Dictionary<string, int>()
                    {
                         {
                             biome,
                             1
                         }
                    }
                });
            }
            if(_config.ShowMsgRate)
                SendChat(session, prefix + "У вас увеличены рейты в данном биоме! Тотемов захвачено в биоме: " + clansRates[newClan.ClanGuid].biomeTotems[biome]);
        }

        void OnDrillDispenserGather(GameObject go, DrillMachine drill, List<ItemObject> items)
        {
            if (!_config.drills.enable)
                return;

            OwnershipStakeServer stake = getStake(go.transform.position);
            if (stake == null)
                return;

            Clan clan = getClanFromTotem(stake);

            if (clansRates.ContainsKey(clan.ClanGuid))
            {
                string biome = getBiome(go.transform.position);
                if (clansRates[clan.ClanGuid].biomeTotems.ContainsKey(biome))
                {
                    int rate = clansRates[clan.ClanGuid].biomeTotems[biome];
                    foreach (var item in items)
                        item.StackSize = item.StackSize * (int)(rate * _config.disp.rate + 1);
                }
            }
        }

        void OnDispenserGather(GameObject resourceNode, HurtMonoBehavior player, List<ItemObject> items)
        {
            if (!_config.disp.enable || resourceNode ==null)
                return;

            PlayerSession session = GameManager.Instance.GetSession(resourceNode.HNetworkView().owner);
            if (session.Identity.Clan == null)
                return;

            var clan = session.Identity.Clan;

            if (clansRates.ContainsKey(clan.ClanGuid))
            {
                string biome = getBiome(session.WorldPlayerEntity.transform.position);
                if(clansRates[clan.ClanGuid].biomeTotems.ContainsKey(biome))
                {
                    int rate = clansRates[clan.ClanGuid].biomeTotems[biome];
                    foreach (var item in items)
                        item.StackSize = item.StackSize * (int)(rate * _config.disp.rate + 1);
                }
            }
        }

        void OnEntityDropLoot(GameObject go, List<ItemObject> items)
        {
            if (!_config.disp.enable || go == null)
                return;

            PlayerSession session = GameManager.Instance.GetSession(go.HNetworkView().owner);
            if (session.Identity.Clan == null)
                return;

            var clan = session.Identity.Clan;

            if (clansRates.ContainsKey(clan.ClanGuid))
            {
                string biome = getBiome(go.transform.position);
                if (clansRates[clan.ClanGuid].biomeTotems.ContainsKey(biome))
                {
                    int rate = clansRates[clan.ClanGuid].biomeTotems[biome];
                    foreach (var item in items)
                        item.StackSize = item.StackSize * (int)(rate * _config.disp.rate + 1);
                }
            }
        }

        #endregion

        #region Helper

        void CreateMarker(Vector3 position)
        {
            MapMarkerData marker = new MapMarkerData();
            marker.Position = position;
            marker.Color = Color.grey;
            //marker.Scale = new Vector3(_config.Radius, _config.Radius, _config.Radius);
        }

        void SendChat(PlayerSession session, string msg) => hurt.SendChatMessage(session, null, msg);

        OwnershipStakeServer getStake(Vector3 position)
        {
            var cell = ConstructionUtilities.GetOwnershipCell(position);
            if (cell >= 0)
            {
                OwnershipStakeServer stake;
                ConstructionManager.Instance.OwnershipCells.TryGetValue(cell, out stake);
                if (stake?.AuthorizedPlayers != null)
                {
                    return stake;
                }
            }
            return null;
        }

        Clan getClanFromTotem(OwnershipStakeServer stake)
        {
            foreach (var identity in stake.AuthorizedPlayers)
                if (identity.Clan != null)
                    return identity.Clan;

            return null;
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

        #endregion
    }
}
