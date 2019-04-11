using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Oxide.Plugins
{
    [Info("CryChat", "Kaidoz", "1.0.0")]
    class CryChat : CovalencePlugin
    {
        #region Class

        public class Prefix
        {
            [JsonProperty("Permission")]
            public string permission;

            [JsonProperty("Prefix")]
            public string prefix;

            [JsonProperty("Prefix color")]
            public string prefixcolor;

            [JsonProperty("Name color")]
            public string namecolor;

            [JsonProperty("Message color")]
            public string messagecolor;

            [JsonProperty("Formats")]
            public Format format;

            public class Format
            {
                [JsonProperty("Format chat")]
                public string format_chat;

                [JsonProperty("Format chat-clantag")]
                public string format_chatclan;
#if ITEMV2
                [JsonProperty("Format OverHead")]
                public string format_up;

                [JsonProperty("Format OverHead Clan")]
                public string format_upclan;
#endif
            }
        }

        #endregion

        #region List

        Dictionary<int, Prefix> prefix_List = new Dictionary<int, Prefix>();
        Dictionary<ulong, DateTime> mute_List = new Dictionary<ulong, DateTime>();

        #endregion

    }
}
