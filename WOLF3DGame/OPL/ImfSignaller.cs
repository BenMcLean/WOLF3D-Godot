using NScumm.Core.Audio.OPL;
using System.Collections.Concurrent;
using WOLF3DModel;

namespace WOLF3D.WOLF3DGame.OPL
{
    public class ImfSignaller : IAdlibSignaller
    {
        public void Init(IOpl opl) => opl?.WriteReg(1, 32); // go to OPL2 mode
        public uint IntervalsOf700HzToWait { get; private set; } = 1;
        public int Position;
        public Imf[] Imf
        {
            get => imf;
            set
            {
                imf = value;
                Position = 0;
            }
        }
        private Imf[] imf = null;
        public static readonly ConcurrentQueue<Imf[]> ImfQueue = new ConcurrentQueue<Imf[]>();
        public bool Update(IOpl opl)
        {
            if (ImfQueue.TryDequeue(out Imf[] imf))
            {
                Silence(opl);
                Imf = imf;
            }
            if (Imf != null)
            {
                ushort delay;
                do
                {
                    opl?.WriteReg(Imf[Position].Register, Imf[Position].Data);
                    delay = Imf[Position].Delay;
                    Position++;
                } while (delay == 0 && Position < Imf.Length);

                if (Position >= Imf.Length)
                {
                    Position = 0;
                    IntervalsOf700HzToWait = 1;
                }
                else IntervalsOf700HzToWait = delay;
            }
            return Imf == null;
        }
        public void Silence(IOpl opl)
        {
            opl?.WriteReg(189, 0);
            for (int i = 0; i < 10; i++)
                opl?.WriteReg(177 + i, 0);
        }
    }
}
