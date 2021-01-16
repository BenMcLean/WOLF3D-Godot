using Godot;
using NScumm.Audio.OPL.Woody;
using NScumm.Core.Audio.OPL;
using NScumm.Core.Audio.OPL.DosBox;
using System;
using System.Xml.Linq;
using WOLF3DModel;

namespace WOLF3D.WOLF3DGame.OPL
{
    public static class SoundBlaster
    {
        public static OplPlayer ImfOplPlayer;
        public static ImfPlayer ImfPlayer => (ImfPlayer)ImfOplPlayer.MusicPlayer;
        public static OplPlayer IdAdlOplPlayer;
        public static IdAdlPlayer IdAdlPlayer => (IdAdlPlayer)IdAdlOplPlayer.MusicPlayer;
        public static Node MidiPlayer = (Node)GD.Load<GDScript>("res://addons/midi/MidiPlayer.gd").New();
        public static readonly Reference SMF = (Reference)GD.Load<GDScript>("res://addons/midi/SMF.gd").New();

        static SoundBlaster()
        {
            IOpl imfOpl = new WoodyEmulatorOpl(OplType.Opl2);
            ImfOplPlayer = new OplPlayer()
            {
                Opl = imfOpl,
                MusicPlayer = new ImfPlayer()
                {
                    Opl = imfOpl,
                },
            };
            IOpl idAdlOpl = new WoodyEmulatorOpl(OplType.Opl2);
            IdAdlOplPlayer = new OplPlayer()
            {
                Opl = idAdlOpl,
                MusicPlayer = new IdAdlPlayer()
                {
                    Opl = idAdlOpl,
                },
            };
            MidiPlayer.Set("soundfont", "res://1mgm.sf2");
            MidiPlayer.Set("loop", true);
        }

        public static AudioT.Song Song
        {
            get => song;
            set
            {
                if (!((song = value) is AudioT.Song s))
                {
                    ImfPlayer.ImfQueue.Enqueue(null);
                    MidiPlayer.Call("stop");
                }
                else if (s.IsImf)
                {
                    MidiPlayer.Call("stop");
                    ImfPlayer.ImfQueue.Enqueue(s.Imf);
                }
                else
                {
                    ImfPlayer.ImfQueue.Enqueue(null);
                    MidiPlayer.Call("stop");
                    MidiPlayer.Set("smf_data", SMF.Call("read_data", s.Bytes));
                    MidiPlayer.Call("play", 0f);
                }
            }
        }
        private static AudioT.Song song = null;

        public static Adl Adl
        {
            get => throw new NotImplementedException();
            set => IdAdlPlayer.IdAdlQueue.Enqueue(value);
        }

        public static void Play(XElement xml)
        {
            if (xml?.Attribute("Sound")?.Value is string sound && !string.IsNullOrWhiteSpace(sound) && Assets.Sound(sound) is Adl adl && adl != null)
                Adl = adl;
        }
    }
}
