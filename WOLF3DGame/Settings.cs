using System;
using System.Text;
using System.Xml.Linq;
using WOLF3D.WOLF3DGame.Menu;
using WOLF3D.WOLF3DGame.OPL;

namespace WOLF3D.WOLF3DGame
{
    public static class Settings
    {
        public static byte Episode { get; set; } = 0;
        public static byte Difficulty { get; set; } = 0;

        #region VRMode
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
            if (Enum.TryParse(vrMode.ToUpperInvariant(), out VRModeEnum newVRMode))
                VRMode = newVRMode;
        }
        #endregion

        #region FX
        public enum FXEnum
        {
            NONE, ADLIB
        }
        public static FXEnum FX
        {
            get => fx;
            set
            {
                fx = value;
                Save();
            }
        }
        private static FXEnum fx = FXEnum.ADLIB;
        public static bool FXMuted => FX == FXEnum.NONE;
        public static bool FXAdlib => FX == FXEnum.ADLIB;
        public static void SetFX(string fx)
        {
            if (Enum.TryParse(fx.ToUpperInvariant(), out FXEnum newFX))
                FX = newFX;
        }
        #endregion

        #region DigiSound
        public enum DigiSoundEnum
        {
            NONE, SOUNDBLASTER
        }
        public static DigiSoundEnum DigiSound
        {
            get => digiSound;
            set
            {
                digiSound = value;
                Save();
            }
        }
        private static DigiSoundEnum digiSound = DigiSoundEnum.SOUNDBLASTER;
        public static bool DigiSoundMuted => DigiSound == DigiSoundEnum.NONE;
        public static bool DigiSoundBlaster => DigiSound == DigiSoundEnum.SOUNDBLASTER;
        public static void SetDigiSound(string d)
        {
            if (Enum.TryParse(d.ToUpperInvariant(), out DigiSoundEnum newD))
                DigiSound = newD;
        }
        #endregion

        #region Music
        public enum MusicEnum
        {
            NONE, ADLIB
        }
        public static MusicEnum Music
        {
            get => music;
            set
            {
                MusicEnum old = music;
                music = value;
                if (MusicMuted)
                {
                    SoundBlaster.Song = null;
                    SoundBlaster.MusicOff();
                }
                else if (old == MusicEnum.NONE && Main.Room is MenuRoom menuRoom)
                    menuRoom.MenuScreen.OnSet();
                Save();
            }
        }
        private static MusicEnum music = MusicEnum.ADLIB;
        public static bool MusicMuted => Music == MusicEnum.NONE;
        public static bool MusicAdlib => Music == MusicEnum.ADLIB;
        public static void SetMusic(string m)
        {
            if (Enum.TryParse(m.ToUpperInvariant(), out MusicEnum newM))
                Music = newM;
        }
        #endregion

        public static string XML()
        {
            StringBuilder sb = new StringBuilder()
                .Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<Settings ");
            if (VRMode != VRModeEnum.ROOMSCALE)
                sb.Append("VRMode=\"").Append(VRMode.ToString()).Append("\" ");
            if (FX != FXEnum.ADLIB)
                sb.Append("FX=\"").Append(FX.ToString()).Append("\" ");
            if (DigiSound != DigiSoundEnum.SOUNDBLASTER)
                sb.Append("DigiSound=\"").Append(DigiSound.ToString()).Append("\" ");
            if (Music != MusicEnum.ADLIB)
                sb.Append("Music=\"").Append(Music.ToString()).Append("\" ");
            return sb.Append("/>").ToString();
        }

        public static void XML(XElement xml)
        {
            if (xml == null)
                return;
            if (xml?.Attribute("VRMode")?.Value is string vrMode && !string.IsNullOrWhiteSpace(vrMode))
                SetVrMode(vrMode);
            if (xml?.Attribute("FX")?.Value is string fx && !string.IsNullOrWhiteSpace(fx))
                SetFX(fx);
            if (xml?.Attribute("DigiSound")?.Value is string d && !string.IsNullOrWhiteSpace(d))
                SetDigiSound(d);
            if (xml?.Attribute("Music")?.Value is string m && !string.IsNullOrWhiteSpace(m))
                SetMusic(m);
        }

        public const string Filename = "settings.xml";

        public static void Load() => Load(Main.Folder);
        public static void Load(string folder) => XML(Assets.LoadXML(folder, Filename));

        public static void Save() => System.IO.File.WriteAllText(System.IO.Path.Combine(Main.Folder, Filename), XML());
    }
}
