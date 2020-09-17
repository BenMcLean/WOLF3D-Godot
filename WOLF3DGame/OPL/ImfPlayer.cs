using NScumm.Core.Audio.OPL;
using WOLF3DModel;

namespace WOLF3D.WOLF3DGame.OPL
{
    /// <summary>
    /// Plays IMF songs in Godot.
    /// </summary>
    public class ImfPlayer
    {
        public IOpl Opl { get; set; }
        public bool Loop { get; set; } = true;
        public uint CurrentPacket
        {
            get => currentPacket;
            set
            {
                currentPacket = value;
                CalcDelay = Delay * Imf.Hz;
            }
        }
        private uint currentPacket = 0;
        public double CalcDelay { get; private set; } = 0d;
        public Imf? Packet => Song != null && Song.IsImf && CurrentPacket < Song.Imf.Length ? Song.Imf[CurrentPacket] : (Imf?)null;
        public byte Register => Packet?.Register ?? 0;
        public byte Data => Packet?.Data ?? 0;
        public ushort Delay => Packet?.Delay ?? 0;
        public double TimeSinceLastPacket { get; private set; } = 0d;

        public AudioT.Song Song
        {
            get => song;
            set
            {
                if (Settings.MusicMuted)
                {
                    song = null;
                    return;
                }
                if (song != value)
                    song = value;
                if (song == null) MusicOff();
                CurrentPacket = 0;
            }
        }
        private AudioT.Song song = null;

        public ImfPlayer PlayMilliseconds(long milliseconds) => PlaySeconds(milliseconds / 1000d);

        public ImfPlayer PlaySeconds(double delta)
        {
            if (Opl == null || Song == null)
                return this;
            if (Song.IsImf)
            {
                TimeSinceLastPacket += delta;
                while (CurrentPacket < Song.Imf.Length && TimeSinceLastPacket >= CalcDelay)
                {
                    TimeSinceLastPacket -= CalcDelay;
                    do
                    {
                        CurrentPacket++;
                        if (CurrentPacket < Song.Imf.Length)
                            Opl.WriteReg(Register, Data);
                    }
                    while (CurrentPacket < Song.Imf.Length && Delay == 0);
                }
                if (CurrentPacket >= Song.Imf.Length)
                    Song = Loop ? Song : null;
            }
            return this;
        }

        public ImfPlayer MusicOff()
        {
            Opl?.WriteReg(189, 0);
            for (int i = 0; i < 10; i++)
                Opl?.WriteReg(177 + i, 0);
            return this;
        }
    }
}
