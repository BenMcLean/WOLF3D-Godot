using Godot;
using NScumm.Core.Audio.OPL;

namespace WOLF3D.WOLF3DGame.OPL
{
    public class OplPlayer : AudioStreamPlayer
    {
        public const int MixRate = 44100;
        public const int FramesPerUpdate = 63; // 700 Hz interval
        public OplPlayer()
        {
            Name = "OplPlayer";
            Stream = new AudioStreamGenerator()
            {
                MixRate = MixRate,
                BufferLength = 0.05f, // Keep this as short as possible to minimize latency
            };
        }

        public IAdlibSignaller AdlibSignaller
        {
            get => adlibSignaller;
            set
            {
                if ((adlibSignaller = value) != null && Opl != null)
                    AdlibSignaller.Init(Opl);
            }
        }
        private IAdlibSignaller adlibSignaller = null;

        public IOpl Opl
        {
            get => opl;
            set
            {
                if ((opl = value) != null)
                {
                    Opl?.Init(MixRate);
                    AdlibSignaller?.Init(Opl);
                }
            }
        }
        private IOpl opl;

        public override void _Ready() => Play();

        public override void _Process(float delta)
        {
            if (Playing && AdlibSignaller != null)
                if (Opl == null)
                    Stop();
                else
                    FillBuffer();
        }

        /// <summary>
        /// This code was originally here: https://github.com/adplug/adplay-unix/blob/master/src/sdl.cc
        /// Then it got ported to C# here: https://github.com/scemino/NScumm.Audio/blob/master/NScumm.Audio.Player/AlPlayer.cs
        /// Then I (Ben McLean) adapted it to work with Godot.
        /// The confusing way it is written is awful and I wish there was a better way.
        /// </summary>
        public OplPlayer FillBuffer()
        {
            if (Opl == null)
                return this;
            int toFill = ((AudioStreamGeneratorPlayback)GetStreamPlayback()).GetFramesAvailable();
            if (ShortBuffer == null || ShortBuffer.Length < toFill)
                ShortBuffer = new short[toFill];
            int pos = 0;

            while (LeftoverFrames > 0 && pos + LeftoverFrames < toFill && LeftoverFrames > FramesPerUpdate)
            {
                Opl.ReadBuffer(ShortBuffer, pos, FramesPerUpdate);
                pos += FramesPerUpdate;
                LeftoverFrames -= FramesPerUpdate;
            }
            if (LeftoverFrames > 0 && pos + LeftoverFrames < toFill)
            {
                Opl.ReadBuffer(ShortBuffer, pos, LeftoverFrames);
                pos += LeftoverFrames;
                LeftoverFrames = 0;
            }
            while (pos + LeftoverFrames < toFill)
            {
                LeftoverFrames += (int)AdlibSignaller.Update(Opl) * FramesPerUpdate;
                if (LeftoverFrames > 0 && pos + LeftoverFrames < toFill)
                {
                    Opl.ReadBuffer(ShortBuffer, pos, LeftoverFrames);
                    pos += LeftoverFrames;
                    LeftoverFrames = 0;
                }
            }

            if (pos > 0)
            {
                Vector2[] vector2Buffer = new Vector2[pos];
                for (uint i = 0; i < vector2Buffer.Length; i++)
                {
                    float soundbite = ShortBuffer[i] / 32767f; // Convert from 16 bit signed integer audio to 32 bit signed float audio
                    vector2Buffer[i] = new Vector2(soundbite, soundbite); // Convert mono to stereo
                }
                if (vector2Buffer.Length > 0)
                    ((AudioStreamGeneratorPlayback)GetStreamPlayback()).PushBuffer(vector2Buffer);
            }
            return this;
        }
        private short[] ShortBuffer;
        private int LeftoverFrames = 0;
    }
}
