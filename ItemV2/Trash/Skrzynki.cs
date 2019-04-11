// Reference: Oxide.Core.MySql

using System.Text;
using Oxide.Core.Database;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using System.Linq;
using System;
using uLink;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Skrzynki", "Pablo", "1.0.0")]
    [Description("Odbieranie itemków ze skrzynek")]

    class Skrzynki : HurtworldPlugin
    {

        HashSet<string> Cooldowns = new HashSet<string>();

        readonly Core.MySql.Libraries.MySql mySql = new Core.MySql.Libraries.MySql();
        Connection connection;

        [ChatCommand("skrzynki")]
        void ClaimRoulette(PlayerSession session, string command, string[] args)
        {
            if (!Cooldowns.Contains(session.SteamId.ToString()))
            {
                Cooldowns.Add(session.SteamId.ToString());
                timer.Once(3, () => Cooldowns.Remove(session.SteamId.ToString()));

                PlayerSession target = session;
                string steamID = target.SteamId.ToString();


                string queryStr = "SELECT id, item_cout, item_name FROM skrzynki_itemy WHERE ipayerid = '" + steamID + "' AND claimed = 0";
                connection = mySql.OpenDb("localhost", 3306, "dbname", "dbuser", "password", this);
                var sql = Sql.Builder.Append(queryStr);
                mySql.Query(sql, connection, list =>
                {

                    if (list.Count > 0)
                    {
                        string id = list[0]["id"].ToString();
                        queryStr = "UPDATE skrzynki_itemy SET claimed = 1 WHERE id = " + id;
                        var sql2 = Sql.Builder.Append(queryStr);
                        mySql.Query(sql2, connection, listb => { });
                        string ItemID = list[0]["item_name"].ToString();
                        int Amount = Convert.ToInt32(list[0]["item_cout"].ToString());
                        var ItemMgr = Singleton<GlobalItemManager>.Instance;
                        var Item = GetItem(ItemID);
                        var CleanName = Item.GetNameKey().Replace("Items/", "").Replace("AmmoType/", "").Replace("Machines/", "");
                        Player.Message(session, "<color=green>[Skrzynki]</color> <color=yellow>Otrzymujesz:</color> " + CleanName);
                        ItemMgr.GiveItem(target.Player, Item.Generator, Amount);
                    }
                    else
                    {
                        Player.Message(session, "<color=green>[Skrzynki]</color> Nie masz itemu do odebrania");
                    }
                });

                mySql.CloseDb(connection);
            }
            else
            {
                Player.Message(session, "<color=green>[Skrzynki]</color> Musisz poczekać 3 sekundy od odebrania ostatniego itemu");
            }
        }

        ItemObject GetItem(string name)
        {
            var imng = Singleton<GlobalItemManager>.Instance;
            var item = getItemFromName(name);
            var obj = imng.CreateItem(item);
            return obj;
        }

        public ItemGeneratorAsset getItemFromName(string name)
        {
            name = name.ToLower();
            foreach (var it in GlobalItemManager.Instance.GetGenerators())
                if (name.Contains(it.Value.GetNameKey().ToLower()))
                    return GlobalItemManager.Instance.GetGenerators()[it.Value.GeneratorId];

            return GlobalItemManager.Instance.GetGenerators()[1];
        }
    }
}