using NScumm.Core.Audio.OPL;
using System.IO;
using System.Linq;

namespace WOLF3D.WOLF3DGame.OPL
{
    public class AdlibMultiplexer : IAdlibPlayer
    {
        public AdlibMultiplexer(params IAdlibPlayer[] players)
        {
            Players = players;
            TimeLeft = new float[Players.Length];
            for (int i = 0; i < Players.Length; i++)
                TimeLeft[i] = Players[i].UntilNextUpdate;
            UntilNextUpdate = TimeLeft[Soonest];
        }
        private readonly IAdlibPlayer[] Players;
        private readonly float[] TimeLeft;
        public float UntilNextUpdate { get; private set; }
        public void Init(IOpl opl)
        {
            foreach (IAdlibPlayer player in Players)
                player.Init(opl);
        }
        public void Silence(IOpl opl)
        {
            foreach (IAdlibPlayer player in Players)
                player.Silence(opl);
        }
        public bool Update(IOpl opl)
        {
            do
            {
                int soonest = Soonest;
                float subtract = TimeLeft[soonest];
                Players[soonest].Update(opl);
                for (int i = 0; i < Players.Length; i++)
                    TimeLeft[i] -= subtract;
                TimeLeft[soonest] = Players[soonest].UntilNextUpdate;
            } while (TimeLeft.Where(f => f <= 0f).Any());
            UntilNextUpdate = TimeLeft[Soonest];
            return true;
        }
        private int Soonest =>
            IndexOfSmallest(TimeLeft) is int index && index >= 0 ?
                index
                : throw new InvalidDataException("AdlibMultiplexer couldn't find next player!");
        public static int IndexOfSmallest(float[] array)
        {
            if (array == null || array.Length < 1)
                return -1;
            float min = float.MaxValue;
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
