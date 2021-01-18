using Godot;
using NScumm.Audio.OPL.Woody;
using NScumm.Core.Audio.OPL;
using System;
using System.Xml.Linq;
using WOLF3DModel;

namespace WOLF3D.WOLF3DGame.OPL
{
    public static class SoundBlaster
    {
        public static readonly ImfPlayer ImfPlayer = new ImfPlayer();
        public static readonly IdAdlPlayer IdAdlPlayer = new IdAdlPlayer();
        public static readonly OplPlayer OplPlayer = new OplPlayer()
        {
            Opl = new WoodyEmulatorOpl(OplType.Opl2),
            MusicPlayer = new AdlibMultiplexer(ImfPlayer, IdAdlPlayer),
        };
        public static readonly Node MidiPlayer = (Node)GD.Load<GDScript>("res://addons/midi/MidiPlayer.gd").New();
        public static readonly Reference SMF = (Reference)GD.Load<GDScript>("res://addons/midi/SMF.gd").New();

        static SoundBlaster()
        {
            MidiPlayer.Set("soundfont", "res://1mgm.sf2");
            MidiPlayer.Set("loop", true);
        }

        public static AudioT.Song Song
        {
            get => song;
            set
            {
                MidiPlayer.Call("stop");
                if (!((song = value) is AudioT.Song s))
                    ImfPlayer.ImfQueue.Enqueue(null);
                else if (s.IsImf)
                    ImfPlayer.ImfQueue.Enqueue(s.Imf);
                else
                {
                    ImfPlayer.ImfQueue.Enqueue(null);
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
