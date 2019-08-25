using Godot;
using WOLF3DSim;

public class Game : Spatial
{
    public static VSwap vswap = new VSwap();

    /// Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        DownloadShareware.Main(new string[] { "" });

        vswap.SetPalette(@"Wolf3DSim\Palettes\Wolf3D.pal");
        vswap.Read(@"WOLF3D\VSWAP.WL1");
        //GameMaps maps = new GameMaps().Read(@"WOLF3D\MAPHEAD.WL1", @"WOLF3D\GAMEMAPS.WL1");

        Godot.Image imageWall = new Image(); ;
        imageWall.CreateFromData(64, 64, false, Image.Format.Rgba8, vswap.Pages[0]);
        ImageTexture itWall = new ImageTexture();
        itWall.CreateFromImage(imageWall, (int)Texture.FlagsEnum.ConvertToLinear);

        AudioStreamSample audioStreamSample = new AudioStreamSample()
        {
            Data = VSwap.ConcatArrays(
                vswap.Pages[vswap.SoundPage],
                vswap.Pages[vswap.SoundPage + 1]
            ),
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

        SpatialMaterial spatialMaterial = new SpatialMaterial()
        {
            FlagsUnshaded = true,
            FlagsDoNotReceiveShadows = true,
            FlagsDisableAmbientLight = true,
            ParamsSpecularMode = SpatialMaterial.SpecularMode.Disabled,
            ParamsCullMode = SpatialMaterial.CullMode.Disabled
        };

        Sprite3D sprite3D = new Sprite3D
        {
            Name = "Sprite3",
            Texture = itWall,
            Scale = new Vector3(5, 5, 5),
            MaterialOverride = spatialMaterial
        };

        AddChild(sprite3D);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    //  public override void _Process(float delta)
    //  {
    //      
    //  }
}
