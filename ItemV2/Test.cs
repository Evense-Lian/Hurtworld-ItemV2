using System;
using UnityEngine;
using Oxide.Core;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Test", "Kaidoz", "1.0.0")]
    public class Test : HurtworldPlugin
    {
        [ChatCommand("testm")]
        void cmdChat()
        {
            foreach(var d in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if(d.name.Contains("Marker"))
                    Puts(d.name);
            }
        }
        
        [ChatCommand("testmark")]
        void cmdTest(PlayerSession session, string cmd, string[] args)
        {
            CreateMarker(session.WorldPlayerEntity.transform.position);

            Puts("test");
            MapMarkerData marker = new MapMarkerData();
            marker.Label = "test";
            marker.Position = session.WorldPlayerEntity.transform.position;
            marker.Scale = new Vector3(150f, 150f, 0f);
            marker.Color = HexStringToColor("#ffffff".Replace("#", ""));
            marker.ShowInCompass = true;
            marker.Global = true;
            MapManagerServer.Instance.RegisterMarker(marker);
        }

        MapMarkerData CreateMarker(Vector3 position)
        {
            var obj = Resources.FindObjectsOfTypeAll<GameObject>().Where(x=> x.name.Contains("CircleMarker")).First();
            
            MapMarkerData marker = new MapMarkerData();
            marker.Prefab = obj;
            marker.Label = "test";
            marker.Position = position;
            marker.Scale = new Vector3(150f, 150f, 0f);
            marker.Color = HexStringToColor("#ffffff".Replace("#", ""));
            marker.ShowInCompass = true;
            marker.Global = true;
            MapManagerServer.Instance.RegisterMarker(marker);
            return marker;
        }

        public Color HexStringToColor(string hexColor)
        {
            string hc = ExtractHexDigits(hexColor);
            if (hc.Length != 6)
            {
                return Color.white;
            }
            string r = hc.Substring(0, 2);
            string g = hc.Substring(2, 2);
            string b = hc.Substring(4, 2);
            Color color = Color.white;
            try
            {
                int ri
                   = Int32.Parse(r, System.Globalization.NumberStyles.HexNumber);
                int gi
                   = Int32.Parse(g, System.Globalization.NumberStyles.HexNumber);
                int bi
                   = Int32.Parse(b, System.Globalization.NumberStyles.HexNumber);
                color = Color.HSVToRGB(ri, gi, bi);
            }
            catch
            {
                return Color.black;
            }
            return color;
        }

        public string ExtractHexDigits(string input)
        {
            // remove any characters that are not digits (like #)
            Regex isHexDigit
               = new Regex("[abcdefABCDEF\\d]+", RegexOptions.Compiled);
            string newnum = "";
            foreach (char c in input)
            {
                if (isHexDigit.IsMatch(c.ToString()))
                    newnum += c.ToString();
            }
            return newnum;
        }
    }
}