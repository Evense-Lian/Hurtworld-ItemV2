using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using uLink;
using UnityEngine;
using BitStream = uLink.BitStream;

namespace Oxide.Plugins
{
    [Info("Fix", "Kaidoz", "1.0.0")]
    public class Fix : HurtworldPlugin
    {
        List<string> dd = new List<string>();
        System.Random rnd = new System.Random();

        void Loaded()
        {
            var item = getItemFromName("InvisiHat");
            item.DataProvider.DescriptionKey = "asd";
            //ItemObject itemObj = GlobalItemManager.Instance.GetItem(item.GeneratorId);
            foreach (var dd in item.SharedComponentLookup)
                if((dd.Value as ItemComponentWorldItemAttachmentBuilder) !=null)
                    Puts((dd.Value as ItemComponentWorldItemAttachmentBuilder).ScaleMultiplier.ToString());
            //Puts(item.SharedComponentLookup);
            //itemObj.Generator.GetDataProvider().DescriptionKey
            //foreach(var eff in item.DataProvider.DescriptionKey)
            return;

            var pl = GameManager.Instance._playerSessions.ElementAt(0);
            for (int d = 0; d < 10; d++)
            {
                uLink.NetworkPlayer networkPlayer = new uLink.NetworkPlayer(rnd.Next(50, 100));
                PlayerSession session = new PlayerSession();
                ulong dd = 777325255 + (ulong)rnd.Next(2000, 3000);
                session.SteamId = new Steamworks.CSteamID();
                GameManager.Instance._playerSessions.Add(networkPlayer, session);
            }
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

        void OnBypass(uLink.BitStream stream, uLink.NetworkPlayer sender)
        {
            string tx = stream.ToString();
            if (!dd.Contains(tx))
            {
                dd.Add(tx);
                Puts(stream.bitsRemaining + "| " + tx + " " + sender.isClient + " " + sender.isConnected + " " + sender.isServer + " " + sender.id);
            }
        }

        object CanUserLogin(string name, string id, string ip)
        {
            var numbersInStr = name.ToCharArray().Count(char.IsDigit);
            if (name.Length == numbersInStr)
            {
                Puts("Bad packes");
                return "Bad packets";
            }


            return null;
        }

        void OnEntitySpawned(HNetworkView data)
        {
            if (data.gameObject.name.Contains("WorkBench"))
            {
                var workb = data.gameObject.GetComponent<Crafter>();
                
                Puts(workb.UnpoweredThinkCoefficient.ToString());
                Puts(workb.ThinksPerSecond.ToString());
                Puts(workb.ThinkCoefficient.ToString());
                workb.ThinksPerSecond = 1f;
            }
        }

        [ChatCommand("getinfo")]
        private void command(PlayerSession session, string cmd, string[] args)
        {
            foreach (GameObject obj in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (obj.name.ToLower().Contains("roach"))
                {
                    Puts("-----");
                    var inv = obj.GetComponent<Inventory>();
                    for (int d = 0; d < inv.Capacity; d++)
                    {
                        try
                        {
                            if (inv.GetSlot(d) != null)
                                Puts("SLOT: " + d + " NAME:" + inv.GetSlot(d).Generator.name);
                        }
                        catch { }
                    }
                }
            }
        }
    }
}
