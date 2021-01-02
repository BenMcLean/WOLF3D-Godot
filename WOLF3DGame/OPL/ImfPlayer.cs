using NScumm.Core.Audio.OPL;
using System.Collections.Concurrent;
using WOLF3DModel;

namespace WOLF3D.WOLF3DGame.OPL
{
    public class ImfPlayer : IMusicPlayer
    {
        public IOpl Opl
        {
            get => opl;
            set
            {
                opl = value;
                Opl?.WriteReg(1, 32); // go to OPL2 mode
                MusicOff();
            }
        }
        private IOpl opl = null;

        public float RefreshRate { get; set; } = 700f;
        public int Position;
        public Imf[] Imf
        {
            get => imf;
            set
            {
                MusicOff();
                imf = value;
                Position = 0;
            }
        }
        private Imf[] imf = null;

        public static readonly ConcurrentQueue<Imf[]> ImfQueue = new ConcurrentQueue<Imf[]>();

        public bool Update()
        {
            if (ImfQueue.TryDequeue(out Imf[] imf))
                Imf = imf;
            if (Imf != null)
            {
                ushort delay;
                do
                {
                    Opl?.WriteReg(Imf[Position].Register, Imf[Position].Data);
                    delay = Imf[Position].Delay;
                    Position++;
                } while (delay == 0 && Position < Imf.Length);

                if (Position >= Imf.Length)
                {
                    Position = 0;
                    RefreshRate = 700f;
                }
                else RefreshRate = 700f / delay;
            }
            return Imf == null;
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
