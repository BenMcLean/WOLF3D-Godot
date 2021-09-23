using System.Xml.Linq;

namespace WOLF3D.WOLF3DGame
{
	/// <summary>
	/// For objects that can be serialized into the savegame files
	/// </summary>
	public interface ISavable
	{
		XElement Save();
	}
}
