using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("FireTools", "Kaidoz | vk.com/kaidoz", "1.0.0")]
    class FireTools : HurtworldPlugin
    {
        Dictionary<string, string> shp = new Dictionary<string, string>
        {
            {
                "Items/Raw Materials/Titranium Ore", "Items/Processed Materials/Shaped Titranium"
            },
            {
                "Items/Raw Materials/Ultranium Ore", "Items/Processed Materials/Shaped Ultranium"
            },
            {
                "Items/Raw Materials/Mondinium Ore", "Items/Processed Materials/Shaped Mondinium"
            },
            {
                "Items/Raw Materials/Iron Ore", "Items/Processed Materials/Shaped Iron"
            }
        };

        private void Loaded()
        {
            if (!permission.PermissionExists(this.Name.ToLower() + ".use"))
                permission.RegisterPermission(this.Name.ToLower() + ".use", this);
        }

        void OnDispenserGather(GameObject resourceNode, HurtMonoBehavior player, List<ItemObject> items)
        {
            var session = Player.Find(GetNameOfObject(resourceNode).Replace("(P)", string.Empty));
            if (session == null)
                return;

            if (!permission.UserHasPermission(session.SteamId.ToString(), this.Name.ToLower() + ".use"))
                return;

            for (int index = 0; index < items.Count - 1; index++)
            {
                if (shp.ContainsKey(items[index].GetNameKey()))
                    items[index] = GetItem(shp[items[index].GetNameKey()], items[index].StackSize);
            }
        }

        ItemObject GetItem(string name, int count)
        {
            var imng = Singleton<GlobalItemManager>.Instance;
            var item = getItemFromName(name);
            var obj = imng.CreateItem(item, count);
            return obj;
        }

        public ItemGeneratorAsset getItemFromName(string name)
        {
            foreach (var it in GlobalItemManager.Instance.GetGenerators())
                if (name.Contains(it.Value.GetNameKey()))
                    return GlobalItemManager.Instance.GetGenerators()[it.Value.GeneratorId];

            return GlobalItemManager.Instance.GetGenerators()[1];
        }

        string GetNameOfObject(UnityEngine.GameObject obj)
        {
            var ManagerInstance = GameManager.Instance;
            return ManagerInstance.GetDescriptionKey(obj);
        }
    }
}
