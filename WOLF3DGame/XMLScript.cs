using System;
using System.Linq;
using System.Xml.Linq;
using WOLF3D.WOLF3DGame.Action;
using WOLF3D.WOLF3DGame.Menu;
using WOLF3D.WOLF3DGame.OPL;
using WOLF3DModel;

namespace WOLF3D.WOLF3DGame
{
	public static class XMLScript
	{
		public static bool Run(XElement xml, ITarget target = null)
		{
			if (Conditional(xml, target))
			{
				Effect(xml, target);
				foreach (XElement child in xml.Elements())
					if (!"And".Equals(child.Name?.LocalName)
						&& !"Else".Equals(child.Name?.LocalName))
						Run(child, target);
				return true;
			}
			foreach (XElement child in xml.Elements("Else"))
				Run(child, target);
			return false;
		}

		private static bool Conditional(XElement xml, ITarget target = null)
		{
			if (!ConditionalOne(xml, target))
				return false;
			foreach (XElement and in xml?.Elements("And") ?? Enumerable.Empty<XElement>())
				if (!ConditionalOne(and, target))
					return false;
			return true;
		}

		private static bool ConditionalOne(XElement xml, ITarget target = null) =>
			!(xml?.Attribute("If")?.Value is string stat)
				|| string.IsNullOrWhiteSpace(stat)
				|| !Main.StatusBar.TryGetValue(stat, out StatusNumber statusNumber)
			|| (
			(
			!uint.TryParse(xml?.Attribute("Equals")?.Value, out uint equals)
				|| statusNumber.Value == equals
			)
			&&
			(
			!uint.TryParse(xml?.Attribute("LessThan")?.Value, out uint less)
				|| statusNumber.Value < less
			)
			&&
			(
			!uint.TryParse(xml?.Attribute("GreaterThan")?.Value, out uint greater)
				|| statusNumber.Value > greater
			)
			&&
			(
			!uint.TryParse(xml?.Attribute("MaxEquals")?.Value, out uint maxEquals)
				|| statusNumber.Max == maxEquals
			)
			&&
			(
			!uint.TryParse(xml?.Attribute("MaxLessThan")?.Value, out uint maxLess)
				|| statusNumber.Max < maxLess
			)
			&&
			(
			!uint.TryParse(xml?.Attribute("MaxGreaterThan")?.Value, out uint maxGreater)
|| statusNumber.Max > maxGreater
			)
			);

		private static void Effect(XElement xml, ITarget target = null)
		{
			SoundBlaster.Play(xml);

			// Status effects
			if (xml?.Attribute("SetMaxOf")?.Value is string setMaxOfString
				&& !string.IsNullOrWhiteSpace(setMaxOfString)
				&& Main.StatusBar.TryGetValue(setMaxOfString, out StatusNumber setMaxOf)
				&& uint.TryParse(xml?.Attribute("SetMax")?.Value, out uint setMax))
				setMaxOf.Max = setMax;
			if (xml?.Attribute("AddToMaxOf")?.Value is string addToMaxString
				&& !string.IsNullOrWhiteSpace(addToMaxString)
				&& Main.StatusBar.TryGetValue(addToMaxString, out StatusNumber addToMaxOf)
				&& uint.TryParse(xml?.Attribute("AddToMax")?.Value, out uint addToMax))
				addToMaxOf.Max = addToMax;
			if (xml?.Attribute("SetTo")?.Value is string setString
				&& !string.IsNullOrWhiteSpace(setString)
				&& Main.StatusBar.TryGetValue(setString, out StatusNumber setStatusNumber)
				&& uint.TryParse(xml?.Attribute("Set")?.Value, out uint set))
				setStatusNumber.Value = set;
			if (xml?.Attribute("AddTo")?.Value is string stat
				&& !string.IsNullOrWhiteSpace(stat)
				&& Main.StatusBar.TryGetValue(stat, out StatusNumber statusNumber)
				&& uint.TryParse(xml?.Attribute("Add")?.Value, out uint add))
				statusNumber.Value += add;

			// Menu effects
			if (xml == null || !Main.InGameMatch(xml))
				return;
			if (xml.Attribute("VRMode")?.Value is string vrMode && !string.IsNullOrWhiteSpace(vrMode))
				Settings.SetVrMode(vrMode);
			if (xml.Attribute("SetFX")?.Value is string fx && !string.IsNullOrWhiteSpace(fx))
				Settings.SetFX(fx);
			if (xml.Attribute("SetDigiSound")?.Value is string d && !string.IsNullOrWhiteSpace(d))
				Settings.SetDigiSound(d);
			if (xml.Attribute("SetMusic")?.Value is string m && !string.IsNullOrWhiteSpace(m))
				Settings.SetMusic(m);
			if (byte.TryParse(xml.Attribute("Episode")?.Value, out byte episode))
				MenuRoom.Episode = episode;

			// Actions
			if (xml.Attribute("Action")?.Value.Equals("SelectGame", StringComparison.InvariantCultureIgnoreCase) ?? false)
				Main.SelectGame(xml.Attribute("Argument").Value);
			if (xml.Attribute("Action")?.Value.Equals("Cancel", StringComparison.InvariantCultureIgnoreCase) ?? false)
				Main.MenuRoom.MenuScreen.Cancel();
			if ((xml.Attribute("Action")?.Value.Equals("Menu", StringComparison.InvariantCultureIgnoreCase) ?? false) &&
				Assets.Menu(xml.Attribute("Argument").Value) is MenuScreen menuScreen)
			{
				Main.MenuRoom.MenuScreen = menuScreen;
				if (xml.Name.LocalName.Equals("Up", StringComparison.InvariantCultureIgnoreCase)
					&& menuScreen.MenuItems?.Count is int count
					&& count > 1)
					menuScreen.SelectedItem = menuScreen.MenuItems[count - 1]; // Select the last item on the new menu screen when scrolling up
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
