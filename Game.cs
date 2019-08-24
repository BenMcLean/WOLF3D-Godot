using Godot;
using WOLF3D;

public class Game : Node2D
{
    public static VSwap vswap = new VSwap();

    /// Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        WOLF3D.DownloadShareware.Main(new string[] { "" });

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

        vswap.Read(@"WOLF3D\VSWAP.WL1");

        Godot.Image imageWall = new Image();
        imageWall.CreateFromData(64, 64, false, Image.Format.Rgba8, vswap.Index2ByteArray(vswap.Graphics[vswap.Graphics.Length - 1]));
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

        GameMaps maps = new GameMaps().Read(@"WOLF3D\MAPHEAD.WL1", @"WOLF3D\GAMEMAPS.WL1");

        AudioStreamSample audioStreamSample = new AudioStreamSample()
        {
            Data = vswap.Sounds[0],
            Format = AudioStreamSample.FormatEnum.Format8Bits,
            MixRate = 7000,
            Stereo = false
        };

        AudioStreamPlayer audioStreamPlayer = new AudioStreamPlayer()
        {
            Stream = audioStreamSample,
            VolumeDb = 0.01f
        };

        AddChild(audioStreamPlayer);

        audioStreamPlayer.Play();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    //  public override void _Process(float delta)
    //  {
    //      
    //  }
}
