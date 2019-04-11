using Oxide.Core.Plugins;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("AdminInvis", "Kaidoz", "1.0.2")]
    [Description("Invisible mode for ItemV2")]
    public class AdminInvis : HurtworldPlugin
    {
        [HookMethod("Loaded")]
        void Loaded()
        {
            if (!permission.PermissionExists(Name.ToLower() + ".admin"))
                permission.RegisterPermission(Name.ToLower() + ".admin", this);
        }

        Dictionary<ulong, string> activ = new Dictionary<ulong, string>();

        [HookMethod("OnPlayerDisconnected")]
        void OnPlayerDisconnected(PlayerSession session)
        {
            if (activ.ContainsKey((ulong)session.SteamId))
                activ.Remove((ulong)session.SteamId);
        }

        string prefix = "<color=#BABABA>[Invis]</color> ";

        [ChatCommand("invis")]
        void CommandInvis(PlayerSession session, string command, string[] args)
        {
            if (permission.UserHasPermission(session.SteamId.ToString(), Name.ToLower() + ".admin") || session.IsAdmin)
            {
                if (args.Length != 1)
                {
                    SendChat(session, prefix + "Syntax: invis 1 OR 0");
                    return;
                }
                var inv = session.WorldPlayerEntity.GetComponent<PlayerInventory>();
                switch (args[0].ToLower())
                {
                    case "true":
                    case "on":
                    case "1":
                        inv.SetSlot(8, GetItem("InvisiHat", 1));
                        if (!activ.ContainsKey((ulong)session.SteamId))
                            activ.Add((ulong)session.SteamId, getUpName(session));
                        else
                            SendChat(session, prefix + "Mode already is on");
                        SendUpName(session, " ");
                        SendClanTag(session, " ");
                        SendChat(session, prefix + "Mode on");
                        break;
                    case "false":
                    case "off":
                    case "0":
                        if (inv.GetSlot(8) != null && inv.GetSlot(8).Generator.name.Contains("InvisiHat"))
                            inv.RemoveItem(8);
                        if (activ.ContainsKey((ulong)session.SteamId))
                        {
                            SendUpName(session, activ[(ulong)session.SteamId]);
                            if (session.Identity.Clan != null)
                                SendClanTag(session, session.Identity.Clan.ClanTag);
                            activ.Remove((ulong)session.SteamId);
                        }
                        else
                        {
                            SendChat(session, prefix + "Mode already is off");
                            return;
                        }
                        SendChat(session, prefix + "Mode off");
                        break;
                    default:
                        SendChat(session, prefix + "Syntax: invis 1 OR 0");
                        break;
                }
            }
            else
                SendChat(session, "No acces");
        }


        void SendUpName(PlayerSession session, string name)
        {
            session.WorldPlayerEntity.GetComponent<HurtMonoBehavior>().RPC("UpdateName", uLink.RPCMode.OthersExceptOwnerBuffered, name);
        }

        void SendClanTag(PlayerSession session, string name)
        {
            var en = (uLink.RPCMode)uLink.RPCMode.OthersExceptOwnerBuffered;
            session.WorldPlayerEntity.GetComponent<HurtMonoBehavior>().RPC("RPCUpdateTag", en, name);
        }

        void SendChat(PlayerSession session, string message)
        {
            hurt.SendChatMessage(session, null, message);
        }

        string getUpName(PlayerSession session) => session.WorldPlayerEntity.GetComponent<HurtMonoBehavior>().RPC("UpdateName", uLink.RPCMode.OthersExceptOwnerBuffered).RPCName;

        ItemObject GetItem(string id, int count)
        {
            var imng = Singleton<GlobalItemManager>.Instance;
            var item = getItemFromName(id);
            var obj = imng.CreateItem(item, count);
            return obj;
        }

        public ItemGeneratorAsset getItemFromName(string name)
        {
            foreach (var it in GlobalItemManager.Instance.GetGenerators())
                if (it.Value.name == name)
                    return GlobalItemManager.Instance.GetGenerators()[it.Value.GeneratorId];

            return GlobalItemManager.Instance.GetGenerators()[1];
        }

    }
}
