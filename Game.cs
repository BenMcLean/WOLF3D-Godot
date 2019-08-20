using Godot;
using System.IO;
using WOLF3D;

public class Game : Node2D
{
    public static VSwap vswap = new VSwap();

    /// Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        WOLF3D.DownloadSharewareWOLF3D.Main();

        vswap.LoadPalette("Palettes\\Wolf3D.pal");

        Godot.Image image = new Image();
        image.CreateFromData(16, 16, false, Image.Format.Rgba8, VSwap.Int2ByteArray(vswap.Palette));
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

        //VswapFileData data;
        //using (FileStream file = new FileStream("WOLF3D\\VSWAP.WL1", FileMode.Open))
        //    data = VswapFileReader.Read(file, 64);

        using (FileStream file = new FileStream(@"WOLF3D\VSWAP.WL1", FileMode.Open))
            vswap.Read(file);

        Godot.Image imageWall = new Image();
        imageWall.CreateFromData(64, 64, false, Image.Format.Rgba8, vswap.Index2ByteArray(vswap.Graphics[vswap.Graphics.Count - 1]));
        ImageTexture itWall = new ImageTexture();
        itWall.CreateFromImage(imageWall, 0);

        Sprite sprite2 = new Sprite
        {
            Name = "Sprite2",
            Texture = itWall,
            Position = new Vector2(600, 200),
            Scale = new Vector2(5, 5)
        };
        AddChild(sprite2);

        Maps maps = new Maps().Read(@"WOLF3D\MAPHEAD.WL1", "");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    //  public override void _Process(float delta)
    //  {
    //      
    //  }
}
