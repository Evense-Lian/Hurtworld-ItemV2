using Oxide.Core;
using Oxide.Core.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Meteors", "Smeag", "1.1.4", ResourceId = 1725)]
    [Description("Meteors fall and bring stuff from space.")]
    public class MeteorEvent : HurtworldPlugin
    {

        struct Prefabs
        {
            public static NetworkInstantiateConfig GetKey(string name)
            {
                if (KeysData.Count() < 5) LoadKey();
                if (KeysData.ContainsKey((name + " (networkinstantiateconfig)").ToLower()))
                {
                    return KeysData[(name + " (networkinstantiateconfig)").ToLower()];
                }
                /*foreach (var st in Resources.FindObjectsOfTypeAll<NetworkInstantiateConfig>())
                {
                    if (st != null)
                    {
                        if (!(st + "").ToLower().Contains("(Clone)"))
                        if ((st + "").ToLower() == (name + "").ToLower())
                        {
                            return st;
                        }
                    }
                }*/
                return null;
            }

            static Dictionary<string, NetworkInstantiateConfig> KeysData = new Dictionary<string, NetworkInstantiateConfig>();

            public static void LoadKey(int i = 0)
            {
                foreach (var st in Resources.FindObjectsOfTypeAll<NetworkInstantiateConfig>())
                {
                    if (st != null)
                    {
                        if (!(st + "").ToLower().Contains("(clone)"))
                        {
                            if (!KeysData.ContainsKey((st + "").ToLower()))
                            {
                                Interface.Oxide.LogInfo((st + "").ToLower());
                                KeysData.Add((st + "").ToLower(), st);
                            }
                        }
                    }
                }
            }
        }

        void Init()
        {
            Prefabs.LoadKey();
        }

        [ChatCommand("kmer")]
        void SpawnMeteor(PlayerSession session)
        {
            hurt.SendChatMessage(session, null, "test");
            Vector3 vector = session.WorldPlayerEntity.transform.position;
            GameObject obj = Singleton<HNetworkManager>.Instance.NetInstantiate(
                uLink.NetworkPlayer.server,
                Prefabs.GetKey("meteor"),
                Vector3.zero,
                Quaternion.Euler(0, 180, 0),
                GameManager.GetSceneTime());

            global::NetworkInstantiateConfig component = obj.GetComponent<NetworkInstantiateConfig>();

            //Vector3 vector = this._dropPoints[this._dropPoints.Count - 1] + Vector3.up * 1000f;
            //this._dropPoints.RemoveAt(this._dropPoints.Count - 1);
            RaycastHit raycastHit;
            if (!Physics.Raycast(vector, Vector3.down, out raycastHit, 2000f, global::LayerMaskManager.TerrainAndConstructions))
            {
                Debug.Log("No terrain at " + vector);
                return;
            }
            vector = raycastHit.point;
            global::MeteorEvent component2 = global::Singleton<global::HNetworkManager>.Instance.NetInstantiate(component, vector, Quaternion.identity, 0, ushort.MaxValue).GetComponent<global::MeteorEvent>();
            if (component2 != null)
            {
                component2.SetRadius(20f);
                component2.MapMarker.Color = Color.yellow;
                //component2.
                //component2.OverrideResourceNode = this.ResourceNodeOverride;
            }
        }


        [ChatCommand("testm")]
        private void SpawnMeteor2(PlayerSession session)
        {
            MeteorShowerEvent meteorShowerEvent = new MeteorShowerEvent();
            GameObject obj = Singleton<HNetworkManager>.Instance.NetInstantiate(
                uLink.NetworkPlayer.server,
                Prefabs.GetKey("meteor"),
                Vector3.zero,
                Quaternion.Euler(0, 180, 0),
                GameManager.GetSceneTime());
            Vector3 a = session.WorldPlayerEntity.transform.position;
            global::NetworkInstantiateConfig component = obj.GetComponent<global::NetworkInstantiateConfig>();

            Vector3 vector = a + Vector3.up * 1000f;
            RaycastHit raycastHit;
            if (!Physics.Raycast(vector, Vector3.down, out raycastHit, 2000f, global::LayerMaskManager.TerrainAndConstructions))
            {
                Debug.Log("No terrain at " + vector);
                return;
            }
            vector = raycastHit.point;
            global::MeteorEvent component2 = global::Singleton<global::HNetworkManager>.Instance.NetInstantiate(component, vector, Quaternion.identity, 0, ushort.MaxValue).GetComponent<global::MeteorEvent>();
            if (component2 != null)
            {
                component2.SetRadius(20f);
                component2.MapMarker.Color = Color.blue;
                component2.OverrideResourceNode = meteorShowerEvent.ResourceNodeOverride;
            }
            component2.Update();
            component2.FireLateUpdate();
        }
    }
}
