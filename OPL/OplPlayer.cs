using Godot;
using NScumm.Core.Audio.OPL;

namespace OPL
{
    public class OplPlayer : Node
    {
        public OplPlayer(IOpl Opl)
        {
            this.Opl = Opl;
        }

        public IOpl Opl { get; set; }
        public ImfPlayer ImfPlayer { get; set; }
        public AdlPlayer AdlPlayer { get; set; }

        private AudioStreamPlayer audioStreamPlayer;

        public AudioStreamPlayer AudioStreamPlayer
        {
            get
            {
                return audioStreamPlayer;
            }
            private set
            {
                audioStreamPlayer = value;
                value.Stream = new AudioStreamGenerator()
                {
                    MixRate = 48000,
                    BufferLength = 0.05f, // Keep this as short as possible to minimize latency
                };
            }
        }

        public AudioStreamGeneratorPlayback AudioStreamGeneratorPlayback
        {
            get
            {
                return (AudioStreamGeneratorPlayback)AudioStreamPlayer?.GetStreamPlayback();
            }
        }

        public AudioStreamGenerator AudioStreamGenerator
        {
            get
            {
                return (AudioStreamGenerator)AudioStreamPlayer?.GetStream();
            }
        }

        public override void _Ready()
        {
            base._Ready();
            AddChild(AudioStreamPlayer = new AudioStreamPlayer());
            Opl.Init((int)AudioStreamGenerator.MixRate);
            AudioStreamPlayer.Play();
            AddChild(ImfPlayer = new ImfPlayer(Opl));
            AddChild(AdlPlayer = new AdlPlayer(Opl));
            FillBuffer();
        }

        public override void _PhysicsProcess(float delta)
        {
            base._Process(delta);
            FillBuffer();
        }

        public OplPlayer FillBuffer()
        {
            int toFill = AudioStreamGeneratorPlayback.GetFramesAvailable() * (Opl.IsStereo ? 2 : 1);
            if (Buffer.Length < toFill)
                Buffer = new short[toFill];
            Opl.ReadBuffer(Buffer, 0, toFill);
            for (uint i = 0; i < toFill; i++)
            {
                float soundbite = Buffer[i] / 32767f; // Convert from 16 bit signed integer audio to 32 bit signed float audio
                Vector2.Set(soundbite, soundbite);
                AudioStreamGeneratorPlayback.PushFrame(Vector2);
            }
            return this;
        }
        private Vector2 Vector2 = new Vector2(0f, 0f);
        private short[] Buffer = new short[70000];
    }
}
