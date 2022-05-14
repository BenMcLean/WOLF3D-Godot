using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace WOLF3D.WOLF3DGame.Menu
{
	public static class GameSelectionMenu
	{
		public const int PerPage = 16;
		/// <summary>
		/// "Have it list games." --Matthew Broderick as David Lightman in WarGames. (1983)
		/// </summary>
		public static XElement InsertGameSelectionMenu(this XElement into)
		{
			string[] games = System.IO.Directory.GetDirectories(Main.Path)
					.Select(e => System.IO.Path.Combine(e, "game.xml"))
					.Where(e => System.IO.File.Exists(e) && XElement.Load(e).Attribute("Name") is XAttribute)
					.OrderByDescending(e => Path.GetFileName(Path.GetDirectoryName(e)).Equals("WL1", System.StringComparison.InvariantCultureIgnoreCase))
					.ToArray();
			int pages = (games.Length - 1) / PerPage + 1;
			for (int page = 0; page < pages; page++)
				into.Element("VgaGraph").Element("Menus").Add(XElement.Parse(string.Format(
"<Menu Name=\"_GameSelect" + page + "\" BkgdColor=\"41\" Font=\"1\" SelectedColor=\"19\" TextColor=\"23\" Song=\"REMIX\">" +
"<Text String=\"Which game to play?\" X=\"center\" Y=\"3\" Color=\"71\" Font=\"1\" />" +
"<Image Name=\"C_MOUSELBACKPIC\" X=\"center\" Y=\"184\" Action=\"Quit\" />" +
"<PixelRect X=\"2\" Y=\"19\" Width=\"316\" Height=\"168\" Color=\"45\" BordColor=\"44\" Bord2Color=\"35\" />" +
"<MenuItems StartX=\"8\" StartY=\"22\" PaddingX=\"24\" Font=\"0\">" +
string.Join(null, Enumerable.Range(page * PerPage, System.Math.Min(PerPage, games.Length - page * PerPage)).Select(e => games[e])
	.Select(game =>
				"<MenuItem Text=\"" +
				XElement.Load(game).Attribute("Name").Value +
				"\" Action=\"SelectGame\" Argument=\"" +
				game +
				"\"/>")) +
"</MenuItems>" +
(pages > 1 ?
"<Up Action=\"Menu\" Argument=\"_GameSelect" + (page <= 0 ? pages - 1 : page - 1) + "\" />" +
"<Down Action=\"Menu\" Argument=\"_GameSelect" + (page >= pages - 1 ? 0 : page + 1) + "\" />" +
"<Text String=\"pg " + (page + 1) + " of " + pages + "\" X=\"220\" Y=\"188\" Action=\"Menu\" Argument=\"_GameSelect" + (page >= pages - 1 ? 0 : page + 1) + "\" />"
: "") +
"<Cancel Action=\"Quit\" />" +
"<Cursor Cursor1=\"C_CURSOR1PIC\" Cursor2=\"C_CURSOR2PIC\" Y=\"-2\" />" +
"</Menu>")));
			into.Element("Audio").Add(XElement.Parse(@"<Imf File=""res://Wondering About My Remix.wlf"" Name=""REMIX"" />"));
			return into;
		}
	}
}
