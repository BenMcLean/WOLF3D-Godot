using Godot;
using System.IO;
using WOLF3D.Graphics;
using static WOLF3D.Graphics.VswapFileReader;

public class Game : Node2D
{
    /// Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        WOLF3D.DownloadSharewareWOLF3D.Main();

        Godot.Image image = new Image();
        image.CreateFromData(16, 16, false, Image.Format.Rgba8, Palette());
        ImageTexture it = new ImageTexture();
        it.CreateFromImage(image, 0);

        Sprite sprite = new Sprite
        {
            Name = "Sprite1",
            Texture = it,
            Position = new Vector2(200, 200),
            Scale = new Vector2(20, 20)
        };
        AddChild(sprite);

        VswapFileData data;
        using (FileStream file = new FileStream("WOLF3D\\VSWAP.WL1", FileMode.Open))
            data = VswapFileReader.Read(file, 64);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    //  public override void _Process(float delta)
    //  {
    //      
    //  }

    public static byte[] Index2ByteArray(byte[] index)
    {
        byte[] bytes = new byte[index.Length * 4];
        for (int i = 0; i < index.Length; i++)
        {
            bytes[i * 4] = (byte)(palette[index[i]] >> 24);
            bytes[i * 4 + 1] = (byte)(palette[index[i]] >> 16);
            bytes[i * 4 + 2] = (byte)(palette[index[i]] >> 8);
            bytes[i * 4 + 3] = (byte)palette[index[i]];
        }
        return bytes;
    }

    /// <returns>A byte array of the palette, ready to make a 16x16 image.</returns>
    public static byte[] Palette()
    {
        byte[] bytes = Int2ByteArray(palette);
        bytes[bytes.Length-1] = 0;
        return bytes;
    }

    public static byte[] Int2ByteArray(uint[] ints)
    {
        byte[] bytes = new byte[ints.Length * 4];
        for (int i = 0; i < ints.Length; i++)
        {
            bytes[i * 4] = (byte)(ints[i] >> 24);
            bytes[i * 4 + 1] = (byte)(ints[i] >> 16);
            bytes[i * 4 + 2] = (byte)(ints[i] >> 8);
            bytes[i * 4 + 3] = (byte)ints[i];
        }
        return bytes;
    }

    public readonly static uint[] palette = WOLF3D.Graphics.PaletteFileReader.ColorModelFromPAL("Palettes\\Wolf3D.pal");
}
