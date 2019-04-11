using Oxide.Core;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Oxide.Plugins
{
    [Info("ItemsHelper", "Kaidoz", "0.0.1")]
    class ItemsHelper : HurtworldPlugin
    {
        #region Class

        public class Item_
        {
            public string Item_Name;
            public string Item_FullName;
            public int Item_Id;
            public string Item_ShortName;
            public string Item_Guid;
            public Item_(ItemGeneratorAsset item)
            {
                Item_Name = item.name;
                Item_FullName = item.GetNameKey();
                Item_Id = item.GeneratorId;
                Item_ShortName = GetShortName(item.name);
                Item_Guid = RuntimeHurtDB.Instance.GetGuid(item);
            }
        }

        #endregion

        #region List

        public static List<Item_> items = new List<Item_>();

        #endregion

        #region Data

        void SaveData() => Interface.Oxide.DataFileSystem.WriteObject("ItemsHelper_List", items);

        #endregion

        #region Init

        [HookMethod("OnServerInitialized")]
        void OnServerInitialized()
        {
            LoadItems();
            SaveData();
        }

        void LoadItems()
        {
            foreach (var it in GlobalItemManager.Instance.GetGenerators())
            {
                var item = it.Value;
                System.IO.File.WriteAllBytes(item.name, item.GetComponentByInterface<IItemIconOverlay>().GetIconOverlayImage().texture.EncodeToPNG());
                items.Add(new Item_(item));
            }
        }

        #endregion

        #region Commands

        string prefix = "[ItemsHelper]";

        [ChatCommand("item")]
        void getinfo(PlayerSession session,string command,string[] args)
        {
            
            var ehs = session.WorldPlayerEntity.GetComponent<EquippedHandlerServer>();
            var item = getItem_(ehs.GetEquippedItem().Generator.GeneratorId);

            session.IPlayer.Reply(prefix +
                $"\nName: {item.Item_Name}" +
                $"\nShortName: {item.Item_ShortName}" +
                $"\nGuid: {item.Item_Guid}" +
                $"\nFullName: {item.Item_FullName}");
        }

        #endregion

        #region Hooks

        /// <summary>
        /// Получить ItemGeneratorAsset из GUID.
        /// Get ItemGeneratorAsset form GUID.
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public ItemGeneratorAsset getItemFromGuid(string guid)
        {
            return RuntimeHurtDB.Instance.GetObjectByGuid<ItemGeneratorAsset>(guid);
        }


        /// <summary>
        /// Получить ItemGeneratorAsset из 'shortname' предмета.
        /// Get ItemGeneratorAsset from 'shortname' of Item.
        /// </summary>
        /// <param name="shortname"></param>
        /// <returns></returns>
        public ItemGeneratorAsset getItemFromShortName(string shortname)
        {
            foreach (var it in items)
                if (it.Item_ShortName == shortname)
                    return GlobalItemManager.Instance.GetGenerators()[it.Item_Id];

            return GlobalItemManager.Instance.GetGenerators()[2];
        }


        /// <summary>
        /// Получить ItemGeneratorAsset из Полного Имени Предмета.
        /// Get ItemGeneratorAsset from Full Name of Item.
        /// </summary>
        /// <param name="fullname"></param>
        /// <returns></returns>
        public static ItemGeneratorAsset getItemFromFullName(string fullname)
        {
            foreach (var it in items)
                if (it.Item_FullName == fullname)
                    return GlobalItemManager.Instance.GetGenerators()[it.Item_Id];

            return GlobalItemManager.Instance.GetGenerators()[1];
        }


        /// <summary>
        /// Получить ItemGeneratorAsset из Названия Предмета.
        /// Get ItemGeneratorAsset form Name of Item.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ItemGeneratorAsset getItemFromName(string name)
        {
            foreach (var it in items)
                if (it.Item_Name == name)
                    return GlobalItemManager.Instance.GetGenerators()[it.Item_Id];

            return GlobalItemManager.Instance.GetGenerators()[1];
        }

        #endregion

        #region Helper

        public Item_ getItem_(int d)
        {
            foreach (var it in items)
                if (it.Item_Id == d)
                    return it;

            return items[0];
        }

        static public string GetShortName(string name)
        {
            string newname = string.Empty;
            for (int d = 0; d < name.Split(' ').Count(); d++)
            {
                if (d == 0)
                    newname +=
                        
                        
                        name.Split(' ')[d].ToLower();
                else
                    newname += name.Split(' ')[d];

            }
            return newname;
        }

        #endregion
    }
}
