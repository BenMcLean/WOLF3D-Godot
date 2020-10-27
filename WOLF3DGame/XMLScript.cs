using System;
using System.Linq;
using System.Xml.Linq;
using WOLF3D.WOLF3DGame.Action;
using WOLF3D.WOLF3DGame.Menu;
using WOLF3DModel;

namespace WOLF3D.WOLF3DGame
{
    public static class XMLScript
    {
        public static void Action(XElement xml)
        {
            if (xml == null || !Main.InGameMatch(xml))
                return;
            if (xml.Attribute("VRMode")?.Value is string vrMode && !string.IsNullOrWhiteSpace(vrMode))
                Settings.SetVrMode(vrMode);
            if (xml.Attribute("FX")?.Value is string fx && !string.IsNullOrWhiteSpace(fx))
                Settings.SetFX(fx);
            if (xml.Attribute("DigiSound")?.Value is string d && !string.IsNullOrWhiteSpace(d))
                Settings.SetDigiSound(d);
            if (xml.Attribute("Music")?.Value is string m && !string.IsNullOrWhiteSpace(m))
                Settings.SetMusic(m);
            if (byte.TryParse(xml.Attribute("Episode")?.Value, out byte episode))
                MenuRoom.Episode = episode;
            if (xml.Attribute("Action")?.Value.Equals("Cancel", StringComparison.InvariantCultureIgnoreCase) ?? false)
                Main.MenuRoom.MenuScreen.Cancel();
            if ((xml.Attribute("Action")?.Value.Equals("Menu", StringComparison.InvariantCultureIgnoreCase) ?? false) &&
                Assets.Menu(xml.Attribute("Argument").Value) is MenuScreen menuScreen)
            {
                Main.MenuRoom.MenuScreen = menuScreen;
                if (Main.Room != Main.MenuRoom)
                    Main.Room.ChangeRoom(Main.MenuRoom);
            }
            if (xml.Attribute("Action")?.Value.Equals("Modal", StringComparison.InvariantCultureIgnoreCase) ?? false)
                Main.MenuRoom.MenuScreen.AddModal(xml.Attribute("Argument").Value);
            if (xml.Attribute("Action")?.Value.Equals("Update", StringComparison.InvariantCultureIgnoreCase) ?? false)
                Main.MenuRoom.MenuScreen.Update();
            if (xml.Attribute("Action")?.Value.Equals("NewGame", StringComparison.InvariantCultureIgnoreCase) ?? false)
            {
                Main.NextLevelStats = null;
                Main.StatusBar = new StatusBar();
                Main.StatusBar["Difficulty"].Value = MenuRoom.Difficulty;
                Main.Room.ChangeRoom(new LoadingRoom((GameMap)Assets.GetMap(MenuRoom.Episode, 1)));
            }
            if (xml.Attribute("Action")?.Value.Equals("NextFloor", StringComparison.InvariantCultureIgnoreCase) ?? false)
                Main.Room.ChangeRoom(new LoadingRoom((GameMap)(
                        Assets.XML?.Element("VSwap")?.Element("Walls")?.Elements("Override")?.Where(e => ushort.TryParse(e.Attribute("Number")?.Value, out ushort number) && number == MenuRoom.LastPushedTile)?.FirstOrDefault() is XElement over
                            && byte.TryParse(over.Attribute("Floor")?.Value, out byte floor) ?
                            Assets.GetMap(Main.ActionRoom.Level.Map.Episode, floor)
                            : Assets.NextMap(Main.ActionRoom.Level.Map)
                        )));
            if (xml.Attribute("Action")?.Value.Equals("End", StringComparison.InvariantCultureIgnoreCase) ?? false)
            {
                Main.MenuRoom.MenuScreen.AddModal(xml.Attribute("Argument")?.Value ?? "Are you sure you want\nto end the game you\nare currently playing?");
                Main.MenuRoom.MenuScreen.Question = Modal.QuestionEnum.END;
                Main.MenuRoom.MenuScreen.Modal.YesNo = true;
            }
            if (xml.Attribute("Action")?.Value.Equals("Resume", StringComparison.InvariantCultureIgnoreCase) ?? false)
                Main.Room.ChangeRoom(Main.ActionRoom);
            if (xml.Attribute("Action")?.Value.Equals("Quit", StringComparison.InvariantCultureIgnoreCase) ?? false)
            {
                Main.MenuRoom.MenuScreen.AddModal(xml.Attribute("Argument")?.Value ?? Main.RNG.RandomElement(Assets.EndStrings));
                Main.MenuRoom.MenuScreen.Question = Modal.QuestionEnum.QUIT;
                Main.MenuRoom.MenuScreen.Modal.YesNo = true;
            }
            return;
        }
    }
}
