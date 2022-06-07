using Godot;
using System.Xml.Linq;

namespace WOLF3D.WOLF3DGame
{
	public class StatusNumber : Node2D, ISavable
	{
		public XElement XML { get; set; }
		public uint Digits => uint.TryParse(XML?.Attribute("Digits")?.Value, out uint digits) ? digits : 0;
		public string StatusNumberName = null;
		public XElement Save()
		{
			XElement e = new XElement(XName.Get("Number"));
			e.SetAttributeValue(XName.Get("Name"), StatusNumberName);
			e.SetAttributeValue(XName.Get("Value"), Value);
			e.SetAttributeValue(XName.Get("Max"), Max);
			e.SetAttributeValue(XName.Get("Digits"), Label);
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
				AtlasTexture texture = Empty ?? Have;
				Item = new TextureRect()
				{
					Name = "Item",
					Texture = texture,
					RectPosition = Vector2.Zero,
				};
			}
			if (uint.TryParse(xml?.Attribute("Max")?.Value, out uint max))
				Max = max;
			if (uint.TryParse(xml?.Attribute("Init")?.Value, out uint init))
				Value = init;
			Visible = !xml.IsFalse("Visible");
		}
		public TextureRect Item
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
		private TextureRect item = null;
		public AtlasTexture Have { get; set; } = null;
		public AtlasTexture Empty { get; set; } = null;
		public StatusNumber(uint digits = 0)
		{
			Name = "StatusNumber";
			if (digits > 0)
			{
				Label = new Label()
				{
					Theme = Assets.StatusBarTheme,
				};
				Label.Set("custom_constants/line_spacing", 0);
				AddChild(Label);
				Value = Value;
			}
		}
		public StatusNumber Blank()
		{
			if (Label != null)
				Label.Text = string.Empty;
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
					if (Item == null && Digits is uint digits && digits > 0 && Label is Label)
					{
						Label.Text = Value.ToString(new string('0', (int)digits));
						if (Label.Theme.DefaultFont is Font font)
							Label.RectPosition = new Vector2(-font.Width(Label.Text), 0f);
					}
					else if (Item is Sprite)
						Item.Texture = val > 0 ? Have : Empty;
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
		public Label Label { get; set; }
		public uint NextLevel => uint.TryParse(XML?.Attribute("LevelReset")?.Value, out uint levelReset) ? levelReset : Value;
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
