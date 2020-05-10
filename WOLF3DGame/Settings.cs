using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace WOLF3D.WOLF3DGame
{
    public static class Settings
    {
        public static byte Episode { get; set; } = 0;
        public static byte Difficulty { get; set; } = 0;

        public enum VRModeEnum
        {
            ROOMSCALE, FIVEDOF
        }

        public static VRModeEnum VRMode
        {
            get => vrMode;
            set
            {
                vrMode = value;
                Save();
            }
        }
        private static VRModeEnum vrMode = VRModeEnum.ROOMSCALE;

        public static bool Roomscale => VRMode == VRModeEnum.ROOMSCALE;
        public static bool FiveDOF => VRMode == VRModeEnum.FIVEDOF;

        public static void SetVrMode(string vrMode)
        {
            if (vrMode?.Equals("Roomscale", StringComparison.InvariantCultureIgnoreCase) ?? false)
                VRMode = VRModeEnum.ROOMSCALE;
            else if (vrMode?.Equals("FiveDOF", StringComparison.InvariantCultureIgnoreCase) ?? false)
                VRMode = VRModeEnum.FIVEDOF;
        }

        public static string XML()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\" ?>\n<Settings ");
            sb.Append("VRMode=\"").Append(VRMode.ToString()).Append("\" ");
            sb.Append("/>");
            return sb.ToString();
        }

        public static void XML(XElement xml)
        {
            if (xml == null)
                return;
            if (xml?.Attribute("VRMode")?.Value is string vrMode && !string.IsNullOrWhiteSpace(vrMode))
                SetVrMode(vrMode);
        }

        public const string Filename = "settings.xml";

        public static void Load() => Load(Main.Folder);
        public static void Load(string folder) => XML(Assets.LoadXML(folder, Filename));

        public static void Save() => System.IO.File.WriteAllText(System.IO.Path.Combine(Main.Folder, Filename), XML());
    }
}
