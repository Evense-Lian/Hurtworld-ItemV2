using Newtonsoft.Json;
using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("DecaySystem", "Kaidoz | vk.com/kaidoz", "1.0.0")]
    [Description("Система распада объектов")]
    class DecaySystem : HurtworldPlugin
    {

        #region Configuration

        public class Configuration
        {
            [JsonProperty("Включен ли плагин?")]
            public bool enabled = true;

            [JsonProperty("Количество часов через которое распадется объект")]
            public int timehours = 4;
        }

        Configuration _config;

        protected override void SaveConfig() => Config.WriteObject(_config);

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating config file for DecaySystem...");
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

        void Loaded()
        {
            LoadConfig();
            CheckAllObject();
        }

        void CheckAllObject()
        {
            foreach (GameObject obj in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (obj.name.Contains("Constructed") || obj.name.Contains("StructureManager") || obj.name.Contains("Dynamic") || obj.name.Contains("LadderCollider(Clone)"))
                {
                    AddComponent(obj);
                }
            }
        }

        void OnEntitySpawned(HNetworkManager data)
        {
            if(check(data.gameObject.name))
            {
                AddComponent(data.gameObject);
            }
        }

        void AddComponent(GameObject obj)
        {
            if (obj.GetComponent<Decay>() == null)
            {
                obj.AddComponent<Decay>();
                obj.GetComponent<Decay>().currentMax = _config.timehours;
            }

        }

        static List<string> exclude = new List<string>()
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

        public class Decay : MonoBehaviour
        {
            public int currentSet = 0;
            public int currentMax = 4;

            public Decay(int h)
            {
                currentMax = h;
            }

            void Awake()
            {
                InvokeRepeating("CheckSelfForDestroy", 1f, 60f);
            }

            OwnershipStakeServer stake;

            void CheckSelfForDestroy()
            {
                ConstructionManager.Instance.OwnershipCells.TryGetValue(ConstructionUtilities.GetOwnershipCell(gameObject.transform.position), out stake);
                if (stake == null)
                {
                    currentSet++;
                }
                else
                    currentSet = 0;
                if (currentSet >= currentMax * 60)
                    Destroy();
            }

            public void Destroy() => Destroy(this);
        }

    }
}
