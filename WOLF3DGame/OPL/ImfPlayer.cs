using Godot;
using NScumm.Core.Audio.OPL;
using WOLF3DModel;

namespace WOLF3D.WOLF3DGame.OPL
{
    /// <summary>
    /// Plays IMF songs in Godot.
    /// </summary>
    public class ImfPlayer : Node
    {
        public IOpl Opl { get; set; }
        public bool Mute { get; set; } = false;
        public bool Loop { get; set; } = true;
        public uint CurrentPacket
        {
            get => currentPacket;
            set
            {
                currentPacket = value;
                CalcDelay = Delay / 700d;
            }
        }
        private uint currentPacket = 0;
        public double CalcDelay { get; private set; } = 0d;
        public Imf? Packet => Song != null && CurrentPacket < Song.Length ? Song[CurrentPacket] : (Imf?)null;
        public byte Register => Packet?.Register ?? 0;
        public byte Data => Packet?.Data ?? 0;
        public ushort Delay => Packet?.Delay ?? 0;
        public double TimeSinceLastPacket { get; private set; } = 0d;

        public Imf[] Song
        {
            get => song;
            set
            {
                if (song != value) song = value;
                CurrentPacket = 0;
            }
        }
        private Imf[] song = null;

        public ImfPlayer PlayMilliseconds(long milliseconds) => PlaySeconds(milliseconds / 1000d);

        public ImfPlayer PlaySeconds(double seconds)
        {
            if (Mute || Opl == null || Song == null)
                return this;
            TimeSinceLastPacket += seconds;
            while (CurrentPacket < Song.Length && TimeSinceLastPacket >= CalcDelay)
            {
                TimeSinceLastPacket -= CalcDelay;
                do
                {
                    Opl.WriteReg(Register, Data);
                    CurrentPacket++;
                }
                while (CurrentPacket < Song.Length && Delay == 0);
            }
            if (CurrentPacket >= Song.Length)
                Song = Loop ? Song : null;
            return this;
        }
    }
}
