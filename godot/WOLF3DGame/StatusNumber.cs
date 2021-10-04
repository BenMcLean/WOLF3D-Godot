using Godot;
using System.Xml.Linq;

namespace WOLF3D.WOLF3DGame
{
	public class StatusNumber : Node2D, ISavable
	{
		public XElement XML { get; set; }
		public string StatusNumberName = null;
		public XElement Save()
		{
			XElement e = new XElement(XName.Get("Number"));
			e.SetAttributeValue(XName.Get("Name"), StatusNumberName);
			e.SetAttributeValue(XName.Get("Value"), Value);
			e.SetAttributeValue(XName.Get("Max"), Max);
			e.SetAttributeValue(XName.Get("Digits"), Digits);
			e.SetAttributeValue(XName.Get("Visible"), Visible);
			e.SetAttributeValue(XName.Get("XML"), XML.ToString());
			return e;
		}
		public StatusNumber(XElement xml) : this(
			uint.TryParse(xml?.Attribute("Digits")?.Value, out uint digits) ? digits : 0
			)
		{
			XML = xml.Attribute("XML")?.Value is string a ? XElement.Parse(a) : xml;
			Position = new Vector2(
				float.TryParse(XML?.Attribute("X")?.Value, out float x) ? x : 0,
				float.TryParse(XML?.Attribute("Y")?.Value, out float y) ? y : 0
				);
			if (XML?.Attribute("Name")?.Value is string name && !string.IsNullOrWhiteSpace(name))
				Name = StatusNumberName = name;
			if (XML?.Attribute("Have")?.Value is string have && !string.IsNullOrWhiteSpace(have))
				Have = Assets.PicTexture(have);
			if (XML?.Attribute("Empty")?.Value is string empty && !string.IsNullOrWhiteSpace(empty))
				Empty = Assets.PicTexture(empty);
			if (Empty != null || Have != null)
			{
				ImageTexture size = Empty ?? Have;
				Item = new Sprite()
				{
					Name = "Item",
					Position = new Vector2(size.GetWidth() / 2, size.GetHeight() / 2),
				};
			}
			if (uint.TryParse(xml?.Attribute("Max")?.Value, out uint max))
				Max = max;
			if (uint.TryParse(xml?.Attribute("Init")?.Value, out uint init))
				Value = init;
			Visible = !xml.IsFalse("Visible");
		}
		public Sprite Item
		{
			get => item;
			set
			{
				if (item != null)
					RemoveChild(item);
				item = value;
				if (item != null)
					AddChild(item);
			}
		}
		private Sprite item = null;
		public ImageTexture Have { get; set; } = null;
		public ImageTexture Empty { get; set; } = null;
		public StatusNumber(uint digits = 0)
		{
			Name = "StatusNumber";
			if (digits > 0)
			{
				Digits = new Sprite[digits];
				for (uint i = 0; i < digits; i++)
					AddChild(Digits[i] = new Sprite()
					{
						Name = "Digit " + i,
						Texture = Assets.StatusBarBlank,
						Position = new Vector2(
							Assets.StatusBarBlank.GetSize().x * (0.5f - i),
							Assets.StatusBarBlank.GetSize().y / 2
							),
					});
			}
		}
		public StatusNumber Blank()
		{
			for (int i = 0; i < (Digits?.Length ?? 0); i++)
				Digits[i].Texture = Assets.StatusBarBlank;
			if (Item != null)
				Item.Texture = Empty;
			return this;
		}
		public uint Value
		{
			get => val ?? 0;
			set
			{
				uint? old = val;
				val = value > Max ? Max : value;
				if (val != old)
					if (Item == null)
					{
						string s = val.ToString();
						for (int i = 0; i < (Digits?.Length ?? 0); i++)
							Digits[i].Texture = i >= s.Length ?
								Assets.StatusBarBlank
								: Assets.StatusBarDigits[uint.Parse(s[s.Length - 1 - i].ToString())];
					}
					else Item.Texture = val > 0 ? Have : Empty;
			}
		}
		private uint? val = null;
		public uint Max
		{
			get => max ?? uint.MaxValue;
			set
			{
				max = value;
				if (Value > Max)
					Value = Max;
			}
		}
		private uint? max = null;
		public Sprite[] Digits { get; set; }
		public uint NextLevel =>
			uint.TryParse(XML?.Attribute("LevelReset")?.Value, out uint levelReset) ? levelReset : Value;
		public struct Stat
		{
			public string Name;
			public uint Max;
			public uint Value;
			public bool Visible;
		}
		public Stat GetStat() => new Stat()
		{
			Name = Name,
			Max = Max,
			Value = Value,
			Visible = Visible,
		};
		public Stat GetNextLevelStat() => new Stat()
		{
			Name = Name,
			Max = Max,
			Value = NextLevel,
			Visible = Visible,
		};
		public StatusNumber Set(Stat stat)
		{
			Name = StatusNumberName = stat.Name;
			Max = stat.Max;
			Value = stat.Value;
			Visible = stat.Visible;
			return this;
		}
		public StatusNumber Set(XElement xml) => Set(new Stat()
		{
			Name = xml.Attribute("Name").Value,
			Max = (uint)xml.Attribute("Max"),
			Value = (uint)xml.Attribute("Value"),
			Visible = !xml.IsFalse("Visible"),
		});
	}
}
