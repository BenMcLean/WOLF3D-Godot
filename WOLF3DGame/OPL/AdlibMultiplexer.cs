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
                TimeLeft[i] = 1f / Players[i].RefreshRate;
            RefreshRate = 1f / TimeLeft[IndexOfSmallest(TimeLeft)];
        }
        private readonly IAdlibPlayer[] Players;
        private readonly float[] TimeLeft;
        public float RefreshRate { get; private set; }
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
                int smallest = IndexOfSmallest(TimeLeft);
                if (smallest == -1)
                    throw new InvalidDataException("AdlibMultiPlexer couldn't find next player!");
                float subtract = TimeLeft[smallest];
                Players[smallest].Update(opl);
                for (int i = 0; i < Players.Length; i++)
                    TimeLeft[i] -= subtract;
                TimeLeft[smallest] = 1f / Players[smallest].RefreshRate;
            } while (TimeLeft.Where(f => f <= 0f).Any());
            RefreshRate = 1f / TimeLeft[IndexOfSmallest(TimeLeft)];
            return true;
        }
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
