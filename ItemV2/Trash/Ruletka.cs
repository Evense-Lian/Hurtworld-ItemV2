// Reference: Oxide.Core.MySql

using System;
using System.Collections.Generic;
using System.Text;
using Oxide.Core.Database;
using Oxide.Core.Libraries.Covalence;
using UnityEngine;
using System.Collections;
using Oxide.Core;
using UnityEngine.Scripting;
using System.Linq;
using Oxide.Core.MySql;

namespace Oxide.Plugins
{
    [Info("Ruletka", "531devv", "1.0.2", ResourceId = 1923)]
    [Description("Plugin do ruletki na strone www")]

    class Ruletka : HurtworldPlugin
    {

        class RouletteData
        {
            public Dictionary<string, RoulettePlayer> Players = new Dictionary<string, RoulettePlayer>();

            public RouletteData()
            {
            }
        }


        class RoulettePlayer
        {
            public string SteamID;
            public string timer;

            public RoulettePlayer()
            {

            }

            public RoulettePlayer(PlayerSession p)
            {
                SteamID = p.SteamId.ToString();
                timer = "0";
            }
        }

        private readonly Core.MySql.Libraries.MySql _mySql = Interface.Oxide.GetLibrary<Core.MySql.Libraries.MySql>();
        private Core.Database.Connection _mySqlConnection;
        static readonly DateTime epoch = new DateTime(2017, 1, 13, 17, 44, 0);
        static double CurrentTime() { return DateTime.UtcNow.Subtract(epoch).TotalSeconds; }
        RouletteData rouletteData;

        void Loaded()
        {
            rouletteData = Interface.GetMod().DataFileSystem.ReadObject<RouletteData>("RouletteData");
        }

        void OnPlayerConnected(PlayerSession player)
        {
            RoulettePlayer p = new RoulettePlayer(player);
            if (!rouletteData.Players.ContainsKey(p.SteamID))
            {
                rouletteData.Players.Add(p.SteamID, p);
                SaveData();
            }
        }

        bool checkDatabase()
        {
            if (_mySqlConnection == null)
            {
                Puts("Can't connect to the database!");
                return false;
            }
            return true;
        }
        /* <color=aqua>[DarkWorld.pl]</color> */
        [ChatCommand("ruletka")]
        void cmdRuletka(PlayerSession session, string command, string[] args)
        {
            var timer = rouletteData.Players[session.SteamId.ToString()].timer;
            var finish = Convert.ToDouble(timer) - CurrentTime();
            var toInt = (int)finish;
            if (finish <= 0)
            {
                var time = CurrentTime() + 5;
                rouletteData.Players[session.SteamId.ToString()].timer = time.ToString();
                SaveData();
                var steamID = session.SteamId;
                var ItemMgr = Singleton<GlobalItemManager>.Instance;
                _mySqlConnection = _mySql.OpenDb("host" + "", 3306, "database" + "", "user" + "", "passworld" + "", this);
                string SelectData = "SELECT * FROM roulette_table WHERE steamid = " + steamID + " AND claimed = 0 LIMIT 1";
                var sql = Core.Database.Sql.Builder.Append(SelectData);
                _mySql.Query(sql, _mySqlConnection, list =>
                {
                    if (list == null) return;
                    var sb = new StringBuilder();
                    foreach (var entry in list)
                    {
                        sb.AppendFormat("{0}", entry["item_name"]);
                    }

                    var sbb = new StringBuilder();
                    foreach (var entry in list)
                    {
                        sbb.AppendFormat("{0}", entry["id"]);
                    }

                    if (sb.ToString() == "red_jacket")
                    {
                        hurt.SendChatMessage(session, null, "<color=#3333cc>[HardHurtx5]</color> Otrzymałeś: " + sb.ToString());
                        string UpdateData = "UPDATE `roulette_table` SET `claimed` = '1' WHERE `roulette_table`.`id` = " + sbb.ToString();
                        _mySql.Update(Sql.Builder.Append(UpdateData), _mySqlConnection);
                        _mySql.CloseDb(_mySqlConnection);
                        ItemMgr.GiveItem(session.Player, getItemFromName(""), 1);
                    }
                    if (sb.ToString() == "owrong")
                    {
                        hurt.SendChatMessage(session, null, "<color=#3333cc>[HardHurtx5]</color> Otrzymałeś: " + sb.ToString());
                        string UpdateData = "UPDATE `roulette_table` SET `claimed` = '1' WHERE `roulette_table`.`id` = " + sbb.ToString();
                        _mySql.Update(Sql.Builder.Append(UpdateData), _mySqlConnection);
                        _mySql.CloseDb(_mySqlConnection);
                        ItemMgr.GiveItem(session.Player, getItemFromName(""), 40);
                    }
                    if (sb.ToString() == "arrow")
                    {
                        hurt.SendChatMessage(session, null, "<color=#3333cc>[HardHurtx5]</color> Otrzymałeś: " + sb.ToString());
                        string UpdateData = "UPDATE `roulette_table` SET `claimed` = '1' WHERE `roulette_table`.`id` = " + sbb.ToString();
                        _mySql.Update(Sql.Builder.Append(UpdateData), _mySqlConnection);
                        _mySql.CloseDb(_mySqlConnection);
                        ItemMgr.GiveItem(session.Player, getItemFromName("Items/Ammo/Arrow"), 255);
                    }
                    if (sb.ToString() == "wood")
                    {
                        hurt.SendChatMessage(session, null, "<color=#3333cc>[HardHurtx5]</color> Otrzymałeś: " + sb.ToString());
                        string UpdateData = "UPDATE `roulette_table` SET `claimed` = '1' WHERE `roulette_table`.`id` = " + sbb.ToString();
                        _mySql.Update(Sql.Builder.Append(UpdateData), _mySqlConnection);
                        _mySql.CloseDb(_mySqlConnection);
                        ItemMgr.GiveItem(session.Player, getItemFromName(""), 255);
                    }
                    if (sb.ToString() == "stone")
                    {
                        hurt.SendChatMessage(session, null, "<color=#3333cc>[HardHurtx5]</color> Otrzymałeś: " + sb.ToString());
                        string UpdateData = "UPDATE `roulette_table` SET `claimed` = '1' WHERE `roulette_table`.`id` = " + sbb.ToString();
                        _mySql.Update(Sql.Builder.Append(UpdateData), _mySqlConnection);
                        _mySql.CloseDb(_mySqlConnection);
                        ItemMgr.GiveItem(session.Player, getItemFromName(""), 255);
                    }
                    else if (sb.ToString() == "deto")
                    {
                        hurt.SendChatMessage(session, null, "<color=#3333cc>[HardHurtx5]</color> Otrzymałeś: " + sb.ToString());
                        string UpdateData = "UPDATE `roulette_table` SET `claimed` = '1' WHERE `roulette_table`.`id` = " + sbb.ToString();
                        _mySql.Update(Sql.Builder.Append(UpdateData), _mySqlConnection);
                        _mySql.CloseDb(_mySqlConnection);
                        ItemMgr.GiveItem(session.Player, getItemFromName(""), 1);
                    }
                    else if (sb.ToString() == "gray_jacket")
                    {
                        hurt.SendChatMessage(session, null, "<color=#3333cc>[HardHurtx5]</color> Otrzymałeś: " + sb.ToString());
                        string UpdateData = "UPDATE `roulette_table` SET `claimed` = '1' WHERE `roulette_table`.`id` = " + sbb.ToString();
                        _mySql.Update(Sql.Builder.Append(UpdateData), _mySqlConnection);
                        _mySql.CloseDb(_mySqlConnection);
                        ItemMgr.GiveItem(session.Player, getItemFromName(""), 1);
                    }
                    else if (sb.ToString() == "m4")
                    {
                        hurt.SendChatMessage(session, null, "<color=#3333cc>[HardHurtx5]</color> Otrzymałeś: " + sb.ToString());
                        string UpdateData = "UPDATE `roulette_table` SET `claimed` = '1' WHERE `roulette_table`.`id` = " + sbb.ToString();
                        _mySql.Update(Sql.Builder.Append(UpdateData), _mySqlConnection);
                        _mySql.CloseDb(_mySqlConnection);
                        ItemMgr.GiveItem(session.Player, getItemFromName(""), 1);
                    }
                    else if (sb.ToString() == "rifle")
                    {
                        hurt.SendChatMessage(session, null, "<color=#3333cc>[HardHurtx5]</color> Otrzymałeś: " + sb.ToString());
                        string UpdateData = "UPDATE `roulette_table` SET `claimed` = '1' WHERE `roulette_table`.`id` = " + sbb.ToString();
                        _mySql.Update(Sql.Builder.Append(UpdateData), _mySqlConnection);
                        _mySql.CloseDb(_mySqlConnection);
                        ItemMgr.GiveItem(session.Player, getItemFromName(""), 1);
                    }
                    else if (sb.ToString() == "shotgun")
                    {
                        hurt.SendChatMessage(session, null, "<color=#3333cc>[HardHurtx5]</color> Otrzymałeś: " + sb.ToString());
                        string UpdateData = "UPDATE `roulette_table` SET `claimed` = '1' WHERE `roulette_table`.`id` = " + sbb.ToString();
                        _mySql.Update(Sql.Builder.Append(UpdateData), _mySqlConnection);
                        _mySql.CloseDb(_mySqlConnection);
                        ItemMgr.GiveItem(session.Player, getItemFromName(""), 1);
                    }
                    else if (sb.ToString() == "ar_bullet")
                    {
                        hurt.SendChatMessage(session, null, "<color=#3333cc>[HardHurtx5]</color> Otrzymałeś: " + sb.ToString());
                        string UpdateData = "UPDATE `roulette_table` SET `claimed` = '1' WHERE `roulette_table`.`id` = " + sbb.ToString();
                        _mySql.Update(Sql.Builder.Append(UpdateData), _mySqlConnection);
                        _mySql.CloseDb(_mySqlConnection);
                        ItemMgr.GiveItem(session.Player, getItemFromName(""), 60);
                    }
                    else if (sb.ToString() == "c4")
                    {
                        hurt.SendChatMessage(session, null, "<color=#3333cc>[HardHurtx5]</color> Otrzymałeś: " + sb.ToString());
                        string UpdateData = "UPDATE `roulette_table` SET `claimed` = '1' WHERE `roulette_table`.`id` = " + sbb.ToString();
                        _mySql.Update(Sql.Builder.Append(UpdateData), _mySqlConnection);
                        _mySql.CloseDb(_mySqlConnection);
                        ItemMgr.GiveItem(session.Player, getItemFromName(""), 1);
                    }
                    else if (sb.ToString() == "rifle_bullet")
                    {
                        hurt.SendChatMessage(session, null, "<color=#3333cc>[HardHurtx5]</color> Otrzymałeś: " + sb.ToString());
                        string UpdateData = "UPDATE `roulette_table` SET `claimed` = '1' WHERE `roulette_table`.`id` = " + sbb.ToString();
                        _mySql.Update(Sql.Builder.Append(UpdateData), _mySqlConnection);
                        _mySql.CloseDb(_mySqlConnection);
                        ItemMgr.GiveItem(session.Player, getItemFromName(""), 60);
                    }
                    else if (sb.ToString() == "powerfull_kanga")
                    {
                        hurt.SendChatMessage(session, null, "<color=#3333cc>[HardHurtx5]</color> Otrzymałeś: " + sb.ToString());
                        string UpdateData = "UPDATE `roulette_table` SET `claimed` = '1' WHERE `roulette_table`.`id` = " + sbb.ToString();
                        _mySql.Update(Sql.Builder.Append(UpdateData), _mySqlConnection);
                        _mySql.CloseDb(_mySqlConnection);
                        ItemMgr.GiveItem(session.Player, getItemFromName(""), 1);
                    }
                    else if (sb.ToString() == "powerfull_quad")
                    {
                        hurt.SendChatMessage(session, null, "<color=#3333cc>[HardHurtx5]</color> Otrzymałeś: " + sb.ToString());
                        string UpdateData = "UPDATE `roulette_table` SET `claimed` = '1' WHERE `roulette_table`.`id` = " + sbb.ToString();
                        _mySql.Update(Sql.Builder.Append(UpdateData), _mySqlConnection);
                        _mySql.CloseDb(_mySqlConnection);
                        ItemMgr.GiveItem(session.Player, getItemFromName(""), 1);
                    }
                    else if (sb.ToString() == "powerfull_car")
                    {
                        hurt.SendChatMessage(session, null, "<color=#3333cc>[HardHurtx5]</color> Otrzymałeś: " + sb.ToString());
                        string UpdateData = "UPDATE `roulette_table` SET `claimed` = '1' WHERE `roulette_table`.`id` = " + sbb.ToString();
                        _mySql.Update(Sql.Builder.Append(UpdateData), _mySqlConnection);
                        _mySql.CloseDb(_mySqlConnection);
                        ItemMgr.GiveItem(session.Player, getItemFromName(""), 1);
                    }
                    else if (sb.ToString() == "shotgun_shell")
                    {
                        hurt.SendChatMessage(session, null, "<color=#3333cc>[HardHurtx5]</color> Otrzymałeś: " + sb.ToString());
                        string UpdateData = "UPDATE `roulette_table` SET `claimed` = '1' WHERE `roulette_table`.`id` = " + sbb.ToString();
                        _mySql.Update(Sql.Builder.Append(UpdateData), _mySqlConnection);
                        _mySql.CloseDb(_mySqlConnection);
                        ItemMgr.GiveItem(session.Player, getItemFromName(""), 50);
                    }
                    else if (sb.ToString() == "shigi")
                    {
                        hurt.SendChatMessage(session, null, "<color=#3333cc>[HardHurtx5]</color> Otrzymałeś: " + sb.ToString());
                        string UpdateData = "UPDATE `roulette_table` SET `claimed` = '1' WHERE `roulette_table`.`id` = " + sbb.ToString();
                        _mySql.Update(Sql.Builder.Append(UpdateData), _mySqlConnection);
                        _mySql.CloseDb(_mySqlConnection);
                        ItemMgr.GiveItem(session.Player, getItemFromName(""), 1);
                    }
                    else if (sb.ToString() == "boar_backpack")
                    {
                        hurt.SendChatMessage(session, null, "<color=#3333cc>[HardHurtx5]</color> Otrzymałeś: " + sb.ToString());
                        string UpdateData = "UPDATE `roulette_table` SET `claimed` = '1' WHERE `roulette_table`.`id` = " + sbb.ToString();
                        _mySql.Update(Sql.Builder.Append(UpdateData), _mySqlConnection);
                        _mySql.CloseDb(_mySqlConnection);
                        ItemMgr.GiveItem(session.Player, getItemFromName(""), 1);
                    }
                    else if (sb.ToString() == "red_bow")
                    {
                        hurt.SendChatMessage(session, null, "<color=#3333cc>[HardHurtx5]</color> Otrzymałeś: " + sb.ToString());
                        string UpdateData = "UPDATE `roulette_table` SET `claimed` = '1' WHERE `roulette_table`.`id` = " + sbb.ToString();
                        _mySql.Update(Sql.Builder.Append(UpdateData), _mySqlConnection);
                        _mySql.CloseDb(_mySqlConnection);
                        ItemMgr.GiveItem(session.Player, getItemFromName(""), 1);
                    }
                    else if (sb.ToString() == "bow")
                    {
                        hurt.SendChatMessage(session, null, "<color=#3333cc>[HardHurtx5]</color> Otrzymałeś: " + sb.ToString());
                        string UpdateData = "UPDATE `roulette_table` SET `claimed` = '1' WHERE `roulette_table`.`id` = " + sbb.ToString();
                        _mySql.Update(Sql.Builder.Append(UpdateData), _mySqlConnection);
                        _mySql.CloseDb(_mySqlConnection);
                        ItemMgr.GiveItem(session.Player, getItemFromName(""), 1);
                    }
                    else if (sb.ToString() == "red_pickaxe")
                    {
                        hurt.SendChatMessage(session, null, "<color=#3333cc>[HardHurtx5]</color> Otrzymałeś: " + sb.ToString());
                        string UpdateData = "UPDATE `roulette_table` SET `claimed` = '1' WHERE `roulette_table`.`id` = " + sbb.ToString();
                        _mySql.Update(Sql.Builder.Append(UpdateData), _mySqlConnection);
                        _mySql.CloseDb(_mySqlConnection);
                        ItemMgr.GiveItem(session.Player, getItemFromName(""), 1);
                    }
                    else if (sb.ToString() == "axe")
                    {
                        hurt.SendChatMessage(session, null, "<color=#3333cc>[HardHurtx5]</color> Otrzymałeś: " + sb.ToString());
                        string UpdateData = "UPDATE `roulette_table` SET `claimed` = '1' WHERE `roulette_table`.`id` = " + sbb.ToString();
                        _mySql.Update(Sql.Builder.Append(UpdateData), _mySqlConnection);
                        _mySql.CloseDb(_mySqlConnection);
                        ItemMgr.GiveItem(session.Player, getItemFromName(""), 1);
                    }
                    else if (sb.ToString() == "blue_pickaxe")
                    {
                        hurt.SendChatMessage(session, null, "<color=#3333cc>[HardHurtx5]</color> Otrzymałeś: " + sb.ToString());
                        string UpdateData = "UPDATE `roulette_table` SET `claimed` = '1' WHERE `roulette_table`.`id` = " + sbb.ToString();
                        _mySql.Update(Sql.Builder.Append(UpdateData), _mySqlConnection);
                        _mySql.CloseDb(_mySqlConnection);
                        ItemMgr.GiveItem(session.Player, getItemFromName(""), 1);
                    }
                    else if (sb.ToString() == "green_pickaxe")
                    {
                        hurt.SendChatMessage(session, null, "<color=#3333cc>[HardHurtx5]</color> Otrzymałeś: " + sb.ToString());
                        string UpdateData = "UPDATE `roulette_table` SET `claimed` = '1' WHERE `roulette_table`.`id` = " + sbb.ToString();
                        _mySql.Update(Sql.Builder.Append(UpdateData), _mySqlConnection);
                        _mySql.CloseDb(_mySqlConnection);
                        ItemMgr.GiveItem(session.Player, getItemFromName(""), 1);
                    }
                    else if (sb.ToString() == "blue_ore")
                    {
                        hurt.SendChatMessage(session, null, "<color=#3333cc>[HardHurtx5]</color> Otrzymałeś: " + sb.ToString());
                        string UpdateData = "UPDATE `roulette_table` SET `claimed` = '1' WHERE `roulette_table`.`id` = " + sbb.ToString();
                        _mySql.Update(Sql.Builder.Append(UpdateData), _mySqlConnection);
                        _mySql.CloseDb(_mySqlConnection);
                        ItemMgr.GiveItem(session.Player, getItemFromName(""), 100);
                    }
                    else if (sb.ToString() == "iron_ore")
                    {
                        hurt.SendChatMessage(session, null, "<color=#3333cc>[HardHurtx5]</color> Otrzymałeś: " + sb.ToString());
                        string UpdateData = "UPDATE `roulette_table` SET `claimed` = '1' WHERE `roulette_table`.`id` = " + sbb.ToString();
                        _mySql.Update(Sql.Builder.Append(UpdateData), _mySqlConnection);
                        _mySql.CloseDb(_mySqlConnection);
                        ItemMgr.GiveItem(session.Player, getItemFromName(""), 255);
                    }
                    else if (sb.ToString() == "green_ore")
                    {
                        hurt.SendChatMessage(session, null, "<color=#3333cc>[HardHurtx5]</color> Otrzymałeś: " + sb.ToString());
                        string UpdateData = "UPDATE `roulette_table` SET `claimed` = '1' WHERE `roulette_table`.`id` = " + sbb.ToString();
                        _mySql.Update(Sql.Builder.Append(UpdateData), _mySqlConnection);
                        _mySql.CloseDb(_mySqlConnection);
                        ItemMgr.GiveItem(session.Player, getItemFromName(""), 150);
                    }
                    else if (sb.ToString() == "red_ore")
                    {
                        hurt.SendChatMessage(session, null, "<color=#3333cc>[HardHurtx5]</color> Otrzymałeś: " + sb.ToString());
                        string UpdateData = "UPDATE `roulette_table` SET `claimed` = '1' WHERE `roulette_table`.`id` = " + sbb.ToString();
                        _mySql.Update(Sql.Builder.Append(UpdateData), _mySqlConnection);
                        _mySql.CloseDb(_mySqlConnection);
                        ItemMgr.GiveItem(session.Player, getItemFromName(""), 150);
                    }
                    else if (sb.ToString() == "gasoline")
                    {
                        hurt.SendChatMessage(session, null, "<color=#3333cc>[HardHurtx5]</color> Otrzymałeś: " + sb.ToString());
                        string UpdateData = "UPDATE `roulette_table` SET `claimed` = '1' WHERE `roulette_table`.`id` = " + sbb.ToString();
                        _mySql.Update(Sql.Builder.Append(UpdateData), _mySqlConnection);
                        _mySql.CloseDb(_mySqlConnection);
                        ItemMgr.GiveItem(session.Player, getItemFromName(""), 100);
                    }
                    else if (sb.ToString() == "car_gearbox")
                    {
                        hurt.SendChatMessage(session, null, "<color=#3333cc>[HardHurtx5]</color> Otrzymałeś: " + sb.ToString());
                        string UpdateData = "UPDATE `roulette_table` SET `claimed` = '1' WHERE `roulette_table`.`id` = " + sbb.ToString();
                        _mySql.Update(Sql.Builder.Append(UpdateData), _mySqlConnection);
                        _mySql.CloseDb(_mySqlConnection);
                        ItemMgr.GiveItem(session.Player, getItemFromName(""), 1);
                    }
                    else if (sb.ToString() == "quad_gearbox")
                    {
                        hurt.SendChatMessage(session, null, "<color=#3333cc>[HardHurtx5]</color> Otrzymałeś: " + sb.ToString());
                        string UpdateData = "UPDATE `roulette_table` SET `claimed` = '1' WHERE `roulette_table`.`id` = " + sbb.ToString();
                        _mySql.Update(Sql.Builder.Append(UpdateData), _mySqlConnection);
                        _mySql.CloseDb(_mySqlConnection);
                        ItemMgr.GiveItem(session.Player, getItemFromName(""), 1);
                    }
                    else if (sb.ToString() == "boar_mask")
                    {
                        hurt.SendChatMessage(session, null, "<color=#3333cc>[HardHurtx5]</color> Otrzymałeś: " + sb.ToString());
                        string UpdateData = "UPDATE `roulette_table` SET `claimed` = '1' WHERE `roulette_table`.`id` = " + sbb.ToString();
                        _mySql.Update(Sql.Builder.Append(UpdateData), _mySqlConnection);
                        _mySql.CloseDb(_mySqlConnection);
                        ItemMgr.GiveItem(session.Player, getItemFromName(""), 1);
                    }
                    else if (sb.ToString() == "m4_auto")
                    {
                        hurt.SendChatMessage(session, null, "<color=#3333cc>[HardHurtx5]</color> Otrzymałeś: " + sb.ToString());
                        string UpdateData = "UPDATE `roulette_table` SET `claimed` = '1' WHERE `roulette_table`.`id` = " + sbb.ToString();
                        _mySql.Update(Sql.Builder.Append(UpdateData), _mySqlConnection);
                        _mySql.CloseDb(_mySqlConnection);
                        ItemMgr.GiveItem(session.Player, getItemFromName(""), 1);
                    }
                    else
                    {
                        hurt.SendChatMessage(session, null, "<color=#3333cc>[HardHurtx5]</color> Niestety nie ma nic dla ciebie! :(");
                    }
                });
            }
            else
            {
                hurt.SendChatMessage(session, null, "<color=#3333cc>[HardHurtx5]</color> Musisz odczekać chwilę! (5 sekund od ostatniego odebrania)");
            }
        }

        public void SaveData() => Interface.GetMod().DataFileSystem.WriteObject("RouletteData", rouletteData);

        public ItemGeneratorAsset getItemFromName(string name)
        {
            foreach (var it in GlobalItemManager.Instance.GetGenerators())
                if (name.Contains(it.Value.GetNameKey()))
                    return GlobalItemManager.Instance.GetGenerators()[it.Value.GeneratorId];

            return GlobalItemManager.Instance.GetGenerators()[1];
        }
    }
}