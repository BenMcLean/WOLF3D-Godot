using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class DosScreen : Spatial
{
    public class VirtualScreenText
    {
        private readonly Queue<string> lines = new Queue<string>();

        public string Text
        {
            get
            {
                StringBuilder stringBuilder = new StringBuilder();
                foreach (string line in lines)
                    stringBuilder.Append(line).Append("\n");
                return stringBuilder.ToString();
            }
            set
            {
                lines.Clear();
                WriteLine(value);
            }
        }

        private Godot.Label label;
        public Godot.Label Label
        {
            get { return label; }
            set
            {
                label = value;
                if (label != null)
                    label.Text = Text;
            }
        }

        private Godot.ColorRect cursor;
        public Godot.ColorRect Cursor
        {
            get { return cursor; }
            set
            {
                cursor = value;
                if (cursor != null)
                    SetCursor();
            }
        }

        public VirtualScreenText SetCursor()
        {
            uint x = (uint)(lines.Count == 0 ? 0 : lines.Last().Length > 79 ? 0 : lines.Last().Length);
            return SetCursor(x, (uint)(lines.Count - (lines.Count > 0 ? 1 : 0)));
        }

        public VirtualScreenText SetCursor(uint x, uint y)
        {
            if (Cursor != null)
                Cursor.RectGlobalPosition = new Godot.Vector2(x * 9, y * 16 + 12);
            return this;
        }

        public uint Height { get; set; } = 25;
        public uint Width { get; set; } = 80;

        public float BlinkRate { get; set; } = 0.25f;
        private float Blink { get; set; } = 0f;

        public bool ShowCursor
        {
            get
            {
                return Cursor == null ? false : Cursor.Visible;
            }
            set
            {
                if (Cursor != null)
                    Cursor.Visible = value;
            }
        }

        public VirtualScreenText UpdateCursor(float delta)
        {
            Blink += delta;
            while (Blink > BlinkRate)
            {
                Blink -= BlinkRate;
                ShowCursor = !ShowCursor;
            }
            return this;
        }

        public override string ToString()
        {
            return Text;
        }

        public VirtualScreenText CLS()
        {
            Text = string.Empty;
            return this;
        }

        public VirtualScreenText WriteLine(string value)
        {
            foreach (string line in Wrap(value).Split('\n'))
                lines.Enqueue(line);
            if (Height > 0)
                while (lines.Count() > Height)
                    lines.Dequeue();
            if (Label != null)
                Label.Text = Text;
            SetCursor();
            return this;
        }

        public string Wrap(string value)
        {
            return Wrap(value, Width);
        }

        public static string Wrap(string value, uint width)
        {
            if (width <= 0)
                return value;
            StringBuilder stringBuilder = new StringBuilder();
            foreach (string a in value.Split('\n'))
                foreach (string b in ChunksUpto(a, width))
                    stringBuilder.Append(b).Append("\n");
            return TrimLastCharacter(stringBuilder.ToString());
        }

        public static IEnumerable<string> ChunksUpto(string str, uint maxChunkSize)
        {
            for (int i = 0; i < str.Length; i += (int)maxChunkSize)
                yield return str.Substring(i, Math.Min((int)maxChunkSize, str.Length - i));
        }

        public static string TrimLastCharacter(string str)
        {
            return string.IsNullOrEmpty(str) ? str : str.Substring(0, (str.Length - 1));
        }
    }
    public readonly VirtualScreenText Screen = new VirtualScreenText();
    private Viewport Viewport;
    private Sprite3D Sprite3D;

    public DosScreen()
    {
        AddChild(Viewport = new Viewport()
        {
            Size = new Vector2(720, 400),
            Disable3d = true,
            RenderTargetClearMode = Viewport.ClearMode.OnlyNextFrame,
            RenderTargetVFlip = true,
        });

        Viewport.AddChild(new ColorRect()
        {
            Color = Color.Color8(0, 0, 0, 255),
            RectSize = Viewport.Size,
        });

        AddChild(Sprite3D = new Sprite3D()
        {
            Texture = Viewport.GetTexture(),
            PixelSize = 0.00338666666f,
            Scale = new Vector3(1f, 1.35f, 1f),
            MaterialOverride = new SpatialMaterial()
            {
                FlagsUnshaded = true,
                FlagsDoNotReceiveShadows = true,
                FlagsDisableAmbientLight = true,
                ParamsSpecularMode = SpatialMaterial.SpecularMode.Disabled,
                ParamsCullMode = SpatialMaterial.CullMode.Back,
                FlagsTransparent = false,
            },
            //GlobalTransform = new Transform(Basis.Identity, new Vector3(2.4384f / -2f, 0f, 0f)),
        });

        BitmapFont font = new BitmapFont();
        font.CreateFromFnt("res://Bm437_IBM_VGA9.fnt");
        Label label = new Label()
        {
            Theme = new Theme()
            {
                DefaultFont = font,
            },
            RectPosition = new Vector2(0, 0),
        };
        label.Set("custom_constants/line_spacing", 0);
        label.Set("custom_colors/font_color", Color.Color8(170, 170, 170, 255));
        Viewport.AddChild(label);
        Screen.Label = label;

        ColorRect cursor = new ColorRect
        {
            Color = Color.Color8(170, 170, 170, 255),
            RectSize = new Vector2(9, 2),
        };
        Viewport.AddChild(cursor);
        Screen.Cursor = cursor;
    }

    public override void _Process(float delta)
    {
        base._Process(delta);
        Screen.UpdateCursor(delta);
        if (Sprite3D.Visible)
            Rotation = new Vector3(0f, GetViewport().GetCamera().GlobalTransform.basis.GetEuler().y, 0f);
    }
}
