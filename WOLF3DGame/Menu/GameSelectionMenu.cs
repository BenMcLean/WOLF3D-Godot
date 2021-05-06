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
			IEnumerable<string> games = System.IO.Directory.GetDirectories(Main.Path, "*", SearchOption.TopDirectoryOnly)
				.Select(e => System.IO.Path.Combine(e, "game.xml"))
				.Where(e => System.IO.File.Exists(e));
			StringBuilder sb = new StringBuilder();
			sb.Append(MenuStart);
			foreach (string game in games)
				sb.Append("<MenuItem Text=\"" +
					Path.GetFileName(Path.GetDirectoryName(game)) +
					"\"/>");
			sb.Append(MenuEnd);
			into.Element("VgaGraph").Element("Menus").Add(XElement.Parse(sb.ToString()));
			into.Element("Audio").Add(XElement.Parse(@"<Imf File=""res://Wondering About My Remix.wlf"" Name=""REMIX"" />"));
			return into;
		}

		public static readonly string MenuStart = @"
<Menu Name=""_GameSelect"" BkgdColor=""41"" Font=""1"" SelectedColor=""19"" TextColor=""23"" DisabledColor=""43"" Song=""REMIX"">
	<Image Name=""C_OPTIONSPIC"" X=""center"" Y=""0"" XBanner=""0"" />
	<Image Name=""C_MOUSELBACKPIC"" X=""center"" Y=""184"" Action=""Cancel"" />
	<PixelRect X=""68"" Y=""52"" Width=""179"" Height=""130"" Color=""45"" BordColor=""44"" Bord2Color=""35"" />
	<MenuItems StartX=""72"" StartY=""56"" PaddingX=""28"">";
		public static readonly string MenuEnd = @"
	</MenuItems>
	<Cancel Action=""Quit"" />
	<Cursor Cursor1=""C_CURSOR1PIC"" Cursor2=""C_CURSOR2PIC"" X=""-1"" Y=""-3"" />
</Menu>";
	}
}
