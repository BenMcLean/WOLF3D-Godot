using Godot;
using NScumm.Audio.OPL.Woody;
using NScumm.Core.Audio.OPL;
using System;
using System.Xml.Linq;
using WOLF3D.WOLF3DGame.Action;
using WOLF3DModel;

namespace WOLF3D.WOLF3DGame.OPL
{
    public static class SoundBlaster
    {
        //public static readonly AudioStreamPlayer AudioStreamPlayer = new AudioStreamPlayer();
        public static readonly ImfSignaller ImfSignaller = new ImfSignaller();
        public static readonly IdAdlSignaller IdAdlSignaller = new IdAdlSignaller();
        public static readonly OplPlayer OplPlayer = new OplPlayer()
        {
            Opl = new WoodyEmulatorOpl(OplType.Opl2),
            AdlibSignaller = new AdlibMultiplexer(ImfSignaller, IdAdlSignaller),
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
                    ImfSignaller.ImfQueue.Enqueue(null);
                else if (s.IsImf)
                    ImfSignaller.ImfQueue.Enqueue(s.Imf);
                else
                {
                    ImfSignaller.ImfQueue.Enqueue(null);
                    MidiPlayer.Set("smf_data", SMF.Call("read_data", s.Bytes));
                    MidiPlayer.Call("play", 0f);
                }
            }
        }
        private static AudioT.Song song = null;

        public static Adl Adl
        {
            get => throw new NotImplementedException();
            set => IdAdlSignaller.IdAdlQueue.Enqueue(value);
        }

        public static void Play(XElement xml, ISpeaker iSpeaker = null)
        {
            if (!Settings.MusicMuted
                && xml?.Attribute("Song")?.Value is string songName
                && !string.IsNullOrWhiteSpace(songName)
                && Assets.AudioT.Songs[songName] is AudioT.Song song
                && (Song != song || xml.IsTrue("OverrideSong")))
                Song = song;
            if (!Settings.DigiSoundMuted
                && xml?.Attribute("DigiSound")?.Value is string digiSound
                && !string.IsNullOrWhiteSpace(digiSound)
                && Assets.DigiSound(digiSound) is AudioStreamSample audioStreamSample
                && iSpeaker != null)
                iSpeaker.Play = audioStreamSample;
            //else
            //{
            //AudioStreamPlayer.Stream = audioStreamSample;
            //AudioStreamPlayer.Play();
            //}
            else if (!Settings.FXMuted
                && xml?.Attribute("Sound")?.Value is string sound
                && !string.IsNullOrWhiteSpace(sound)
                && Assets.Sound(sound) is Adl adl)
                Adl = adl;
        }
    }
}
