using Godot;
using System.Linq;
using System.Xml.Linq;
using WOLF3D.WOLF3DGame.Action;

namespace WOLF3D.WOLF3DGame.Menu
{
	public class Counter : Label
	{
		public XElement XML { get; set; } = null;
		public uint Digits { get; set; } = 3;
		public uint FinalValue { get; set; } = 0;
		public float SinceLast { get; set; } = 0f;
		public float Interval { get; set; } = 0.03f;
		public bool Started
		{
			get => started;
			set
			{
				if (started = value)
				{
					SinceLast = 0f;
					Value = 0;
				}
			}
		}
		private bool started = false;
		public bool Finished { get; private set; } = false;

		public Counter(XElement xml)
		{
			XML = xml;
			RectPosition = new Vector2(
				float.TryParse(XML?.Attribute("X")?.Value, out float x) ? x : 0,
				float.TryParse(XML?.Attribute("Y")?.Value, out float y) ? y : 0
				);
			switch ((Name = XML?.Attribute("Name")?.Value is string name && !string.IsNullOrWhiteSpace(name) ? name : "Counter").ToUpperInvariant())
			{
				case "KILL":
					FinalValue = (uint)((double)Main.ActionRoom.Level.Actors.Cast<Actor>().Where(actor => actor.State.Dead).Count() / Main.ActionRoom.Level.Actors.Count * 100d);
					break;
				case "SECRET":
					FinalValue = (uint)((double)Main.ActionRoom.Level.PushWalls.Cast<PushWall>().Where(pushWall => pushWall.Pushed).Count() / Main.ActionRoom.Level.PushWalls.Count * 100d);
					break;
				case "TREASURE":
					int total = Assets.Treasure(Main.ActionRoom.Level.Map);
					FinalValue = (uint)((double)(total - Main.ActionRoom.Level.GetChildren().Cast<Node>().Where(node => node is Pickup pickup && pickup.Treasure).Count()) / total * 100d);
					break;
			}
			if (uint.TryParse(xml?.Attribute("Font")?.Value, out uint bitmapFont))
				Theme = Assets.FontThemes[bitmapFont];
			if (uint.TryParse(xml?.Attribute("Digits")?.Value, out uint digits))
				Digits = digits;
			Visible = !XML.IsFalse("Visible");
		}

		public uint? Value
		{
			get => val;
			set
			{
				uint? old = val;
				if ((val = value) != old)
					Text = val is uint v ?
						string.Format("{0," + Digits.ToString() + ":" + new string('#', (int)Digits - 1) + "0}", v)
						: "";
			}
		}
		private uint? val = null;

		public override void _Process(float delta)
		{
			if (Started && !Finished)
			{
				SinceLast += delta;
				while (Value < FinalValue && SinceLast > Interval)
				{
					SinceLast -= Interval;
					Value++;
				}
				if (Value >= FinalValue)
				{
					Value = FinalValue;
					Finished = true;
				}
			}
		}
	}
}
