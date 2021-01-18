using NScumm.Core.Audio.OPL;
using System.IO;
using System.Linq;

namespace WOLF3D.WOLF3DGame.OPL
{
    public class AdlibMultiplexer : IAdlibSignaller
    {
        public AdlibMultiplexer(params IAdlibSignaller[] players)
        {
            Players = players;
            TimeLeft = new int[Players.Length];
            for (int i = 0; i < Players.Length; i++)
                TimeLeft[i] = (int)Players[i].IntervalsOf700HzToWait;
            IntervalsOf700HzToWait = (uint)TimeLeft[Soonest];
        }
        private readonly IAdlibSignaller[] Players;
        private readonly int[] TimeLeft;
        public uint IntervalsOf700HzToWait { get; private set; } = 1;
        public void Init(IOpl opl)
        {
            foreach (IAdlibSignaller player in Players)
                player.Init(opl);
        }
        public void Silence(IOpl opl)
        {
            foreach (IAdlibSignaller player in Players)
                player.Silence(opl);
        }
        public bool Update(IOpl opl)
        {
            do
            {
                int soonest = Soonest;
                int subtract = TimeLeft[soonest];
                Players[soonest].Update(opl);
                for (int i = 0; i < Players.Length; i++)
                    TimeLeft[i] -= subtract;
                TimeLeft[soonest] = (int)Players[soonest].IntervalsOf700HzToWait;
            } while (TimeLeft.Where(f => f <= 0).Any());
            IntervalsOf700HzToWait = (uint)TimeLeft[Soonest];
            return true;
        }
        private int Soonest =>
            IndexOfSmallest(TimeLeft) is int index && index >= 0 ?
                index
                : throw new InvalidDataException("AdlibMultiplexer couldn't find next player!");
        public static int IndexOfSmallest(int[] array)
        {
            if (array == null || array.Length < 1)
                return -1;
            int min = int.MaxValue;
            int result = 0;
            for (int i = 0; i < array.Length; i++)
                if (array[i] < min)
                {
                    min = array[i];
                    result = i;
                }
            return result;
        }
    }
}
