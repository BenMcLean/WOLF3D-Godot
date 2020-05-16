using Godot;
using NScumm.Core.Audio.OPL;
using WOLF3DModel;
using static WOLF3D.WOLF3DGame.OPL.SoundBlaster;

namespace WOLF3D.WOLF3DGame.OPL
{
    public class OplPlayer : AudioStreamPlayer
    {
        public OplPlayer()
        {
            Name = "OplPlayer";
            Stream = new AudioStreamGenerator()
            {
                MixRate = 48000,
                BufferLength = 0.05f, // Keep this as short as possible to minimize latency
            };
        }

        public IOpl Opl
        {
            get => opl;
            set
            {
                opl = value;
                if (value != null)
                {
                    Opl.Init((int)((AudioStreamGenerator)Stream).MixRate);
                }
                if (ImfPlayer != null) ImfPlayer.Opl = value;
                if (AdlPlayer != null) AdlPlayer.Opl = value;
            }
        }
        private IOpl opl;

        public ImfPlayer ImfPlayer { get; private set; } = new ImfPlayer();
        public AdlPlayer AdlPlayer { get; private set; } = new AdlPlayer();

        public override void _Ready()
        {
            Play();
            //FillBuffer();
        }

        public override void _Process(float delta)
        {
            FillBuffer();
            while (SoundMessages.TryDequeue(out object soundMessage))
                if (soundMessage is Imf[] imf)
                    ImfPlayer.Song = imf;
                else if (soundMessage is Adl adl)
                    AdlPlayer.Adl = adl;
                else if (soundMessage is SoundMessage message)
                    switch (message)
                    {
                        case SoundMessage.STOP_MUSIC:
                            ImfPlayer.Song = null;
                            break;
                        case SoundMessage.STOP_SFX:
                            AdlPlayer.Adl = null;
                            break;
                            //case SoundMessage.QUIT:
                            //    quit = true;
                            //    break;
                    }
            ImfPlayer.PlaySeconds(delta);
            AdlPlayer.PlayNotes(delta);
            //SoundBlaster.PlayNotes(delta);
        }

        public OplPlayer FillBuffer()
        {
            if (Opl == null)
                return this;
            int toFill = ((AudioStreamGeneratorPlayback)GetStreamPlayback()).GetFramesAvailable() * (Opl.IsStereo ? 2 : 1);
            if (Buffer.Length < toFill)
                Buffer = new short[toFill];
            Opl.ReadBuffer(Buffer, 0, toFill);
            Vector2[] buffer = new Vector2[toFill];
            for (uint i = 0; i < toFill; i++)
            {
                float soundbite = Buffer[i] / 32767f; // Convert from 16 bit signed integer audio to 32 bit signed float audio
                buffer[i] = new Vector2(soundbite, soundbite);
            }
            ((AudioStreamGeneratorPlayback)GetStreamPlayback()).PushBuffer(buffer);
            return this;
        }
        private short[] Buffer = new short[70000];
    }
}
