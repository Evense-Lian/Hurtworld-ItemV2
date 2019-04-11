using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using System;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("TradePlus", "Kaidoz | vk.com/kaidoz", "1.0.0")]
    class TradePlus : HurtworldPlugin
    {
        [PluginReference]
        Plugin NoEscapeV2;

        public enum Trade
        {
            NoSend,
            Waiting,
            Accept
        }


        public class BlackList
        {
            public List<Black> list;

            public class Black
            {
                public string name;

                public ulong id;
            }
        }

        public class TradeManager
        {
            public List<ItemObject> items = new List<ItemObject>();

            public Trade tradeStag = Trade.NoSend;

            public ulong memberTrade = 0;

            public void newMember(PlayerSession session)
            {
                this.memberTrade = (ulong)session.SteamId;
            }
        }

        public Dictionary<ulong, BlackList> blacklist = new Dictionary<ulong, BlackList>();

        public Dictionary<ulong, TradeManager> trades = new Dictionary<ulong, TradeManager>();

        void Loaded()
        {
            NoEscapeV2 = plugins.Find("NoEscapeV2");
        }

        #region Lang



        #endregion

        [ChatCommand("trade")]
        void TradeCommand(PlayerSession session, string cmd, string[] args)
        {
            if ((bool)NoEscapeV2.Call("IsRaid", session))
                return;

            if (args.Length == 0)
            {
                Send(session, "Чтобы добавить предмет в обмен - возьмите в руки и введите: /trade add");
                Send(session, "Чтобы вернуть все предметы введите: /trade back");
                Send(session, "Чтобы предложить обмен введите: /trade to <ник>");
                return;
            }
            PlayerInventory inv = session.WorldPlayerEntity.GetComponent<PlayerInventory>();
            var trade = getTrade(session);
            switch (args[0].ToLower())
            {
                case "add":
                    if (trade.tradeStag != Trade.NoSend)
                    {
                        Send(session, "Нельзя изменить предметы для обмена во время обмена!");
                        return;
                    }
                    EquippedHandlerServer ehs = session.WorldPlayerEntity.GetComponent<EquippedHandlerServer>();
                    ItemObject item = ehs.GetEquippedItem();
                    inv.RemoveItem(inv.GetSlotIndex(item));
                    trade.items.Add(item);
                    break;
                case "back":
                    if (trade.tradeStag != Trade.NoSend)
                    {
                        Send(session, "Нельзя вернуть предметы во время обмена!");
                        return;
                    }
                    RefaundItems(trade.items, session);
                    trade.items.Clear();
                    break;
                case "to":
                    if (args.Length == 1)
                    {
                        Send(session, "Введите ник того - кому вы хотите отправить обмен!");
                        return;
                    }
                    var toSession = getSession(args[1]);
                    if (!AllowedTrade(session, toSession))
                        return;
                    var _trade = getTrade(toSession);
                    trade.newMember(toSession);
                    Send(toSession, "Вам отправил предложение об обмене!");
                    break;
                case "a":
                case "accept":
                    var membertrade = getFromTrade(session);
                    if (membertrade == null)
                    {
                        Send(session, "Никто не отправил вам обмен!");
                        return;
                    }


                    break;
                case "c":
                case "cancel":


                    break;

                case "blacklist":

                    break;
            }
        }

        void TimeOutTrade(PlayerSession session, TradeManager trade)
        {
            ulong oldid = trade.memberTrade;
            var sesTo = getSession(trade.memberTrade.ToString());
            string nameTo = sesTo.Identity.Name;
            Timer ti = null;
            ti = timer.Repeat(1f, 30, () =>
            {
                bool destroy = false;
                if (session == null)
                {
                    if (sesTo != null)
                        Send(sesTo, $"Игрок {session.Identity.Name} вышел");
                    destroy = true;
                }

                if (oldid != trade.memberTrade)
                {
                    destroy = true;
                }
                if (getSession(trade.memberTrade.ToString()) == null)
                {
                    Send(sesTo, $"Игрок {nameTo} вышел");
                    destroy = true;
                }
                if (trade.tradeStag == Trade.NoSend)
                    destroy = true;

                if (destroy)
                    ti.Destroy();
            });
        }

        bool AllowedTrade(PlayerSession session, PlayerSession toSession)
        {
            var memTrade = getFromTrade(toSession);
            if (memTrade != null)
            {
                Send(session, $"Игроку уже отправил обмен {memTrade.Identity.Name}!");
                return false;
            }
            if (existInBlackList(session, toSession))
            {
                Send(session, $"Вы находитесь в черном списке у игрока {toSession.Identity.Name}!");
                return false;
            }
            return true;
        }

        bool existInBlackList(PlayerSession thisSession, PlayerSession inSession)
        {
            var exists = (from x in blacklist[(ulong)inSession.SteamId].list where x.id == (ulong)thisSession.SteamId select x).Count();
            if (exists > 0)
            {
                return true;
            }
            return false;
        }

        PlayerSession getFromTrade(PlayerSession session)
        {
            ulong id = (ulong)session.SteamId;
            foreach (var tr in trades)
                if (tr.Value.memberTrade == id)
                    return getSession(tr.Key.ToString());

            return null;
        }

        TradeManager getTrade(PlayerSession session)
        {
            return trades[(ulong)session.SteamId];
        }

        void RefaundItems(List<ItemObject> items, PlayerSession session)
        {
            foreach (var it in items)
                GiveItem(session, it);
        }

        void GiveItem(PlayerSession player, ItemObject item, int amount)
        {
            PlayerInventory inventory = player.WorldPlayerEntity.GetComponent<PlayerInventory>();

            GlobalItemManager.Instance.GiveItem(player.Player, item.Generator, amount);
        }

        void GiveItem(PlayerSession player, ItemObject item)
        {
            PlayerInventory inventory = player.WorldPlayerEntity.GetComponent<PlayerInventory>();

            GlobalItemManager.Instance.GiveItem(player.Player, item);
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

        void Send(PlayerSession session, string msg) => hurt.SendChatMessage(session, null, msg);

        void notice(PlayerSession session, string s) => Singleton<AlertManager>.Instance.GenericTextNotificationServer(s, session.Player);

        private PlayerSession getSession(string identifier)
        {
            var sessions = GameManager.Instance.GetSessions();
            PlayerSession session = null;

            foreach (var i in sessions)
            {
                if (i.Value.Identity.Name.ToUpper().Contains(identifier.ToUpper()) || identifier.Equals(i.Value.SteamId.ToString()))
                {
                    session = i.Value;
                    break;
                }
            }

            return session;
        }
    }
}
