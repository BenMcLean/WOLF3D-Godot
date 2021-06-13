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
		}
		private readonly IAdlibSignaller[] Players;
		private readonly int[] TimeLeft;
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
		public uint Update(IOpl opl)
		{
			do
			{
				int soonest = Soonest, subtract = TimeLeft[soonest];
				TimeLeft[soonest] = (int)Players[soonest].Update(opl);
				for (int i = 0; i < Players.Length; i++)
					if (i != soonest)
						TimeLeft[i] -= subtract;
			} while (TimeLeft.Where(f => f <= 0).Any());
			return (uint)TimeLeft[Soonest];
		}
		private int Soonest =>
			IndexOfSmallest(TimeLeft) is int index && index >= 0 ?
				index
				: throw new InvalidDataException("AdlibMultiplexer couldn't find next player!");
		public static int IndexOfSmallest(int[] array)
		{
			int min = int.MaxValue, result = -1;
			for (int i = 0; i < (array?.Length ?? 0); i++)
				if (array[i] < min)
				{
					min = array[i];
					result = i;
				}
			return result;
		}
	}
}
