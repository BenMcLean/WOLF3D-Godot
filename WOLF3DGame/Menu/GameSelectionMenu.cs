using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace WOLF3D.WOLF3DGame.Menu
{
	public static class GameSelectionMenu
	{
		/// <summary>
		/// "Have it list games." --Matthew Broderick as David Lightman in WarGames. (1983)
		/// </summary>
		public static XElement InsertGameSelectionMenu(this XElement into)
		{
			into.Element("VgaGraph").Element("Menus").Add(XElement.Parse(
				string.Format(@"
<Menu Name=""{0}"" BkgdColor=""41"" Font=""1"" SelectedColor=""19"" TextColor=""23"" Song=""REMIX"">
	<PixelRect X=""6"" Y=""19"" Width=""309"" Height=""165"" Color=""45"" BordColor=""44"" Bord2Color=""35"" />
	<Text String=""Which game to play?"" X=""center"" Y=""3"" Color=""71"" />
	<Image Name=""C_MOUSELBACKPIC"" X=""center"" Y=""184"" Action=""Quit"" />
	<MenuItems StartX=""8"" StartY=""22"" PaddingX=""24"" Font=""0"">{1}</MenuItems>
	<Cancel Action=""Quit"" />
	<Cursor Cursor1=""C_CURSOR1PIC"" Cursor2=""C_CURSOR2PIC"" Y=""-2"" />
</Menu>",
				"_GameSelect",
				string.Join(null, System.IO.Directory.GetDirectories(Main.Path)
					.Select(e => System.IO.Path.Combine(e, "game.xml"))
					.Where(e => System.IO.File.Exists(e) && XElement.Load(e).Attribute("Name") is XAttribute)
					.OrderByDescending(e => Path.GetFileName(Path.GetDirectoryName(e)).Equals("WL1", System.StringComparison.InvariantCultureIgnoreCase))
					.Select(game =>
						"<MenuItem Text=\"" +
						XElement.Load(game).Attribute("Name").Value +
						"\" Action=\"SelectGame\" Argument=\"" +
						game +
						"\"/>"))
					)));
			into.Element("Audio").Add(XElement.Parse(@"<Imf File=""res://Wondering About My Remix.wlf"" Name=""REMIX"" />"));
			return into;
		}
	}
}
