using Godot;
using NScumm.Core.Audio.OPL;
using static OPL.Imf;

namespace OPL
{
    /// <summary>
    /// Plays IMF songs in Godot.
    /// </summary>
    public class ImfPlayer : Node
    {
        public IOpl Opl { get; set; }
        public bool Mute { get; set; } = false;
        public bool Loop { get; set; } = true;
        private float CurrentPacketDelay = 0f;
        public int CurrentPacket { get; set; } = 0;
        public float TimeSinceLastPacket { get; set; } = 0f;

        public Imf[] Song
        {
            get { return song; }
            set
            {
                if (song != value) song = value;
                CurrentPacket = -1;
                CurrentPacketDelay = 0f;
                TimeSinceLastPacket = 0f;
            }
        }
        private Imf[] song;

        public override void _PhysicsProcess(float delta)
        {
            base._Process(delta);
            if (!Mute && Opl != null && Song != null)
                PlayNotes(delta);
        }

        public ImfPlayer PlayNotes(float delta)
        {
            TimeSinceLastPacket += delta;
            while (CurrentPacket < Song.Length && TimeSinceLastPacket >= CurrentPacketDelay)
            {
                TimeSinceLastPacket -= CurrentPacketDelay;
                do
                {
                    CurrentPacket++;
                    if (CurrentPacket < Song.Length)
                        Opl.WriteReg(Song[CurrentPacket].Register, Song[CurrentPacket].Data);
                }
                while (CurrentPacket < Song.Length && Song[CurrentPacket].Delay == 0);
                CurrentPacketDelay = CurrentPacket < Song.Length ?
                    CalcDelay(Song[CurrentPacket].Delay)
                    : 0;
            }
            if (CurrentPacket >= Song.Length)
                Song = Loop ? Song : null;
            return this;
        }
    }
}
