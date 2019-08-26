using Godot;
using WOLF3D;
using WOLF3DSim;

public class Game : Spatial
{
    public static Assets Assets;

    /// Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        DownloadShareware.Main(new string[] { "" });
        Assets = new Assets(new VSwap()
            .SetPalette(@"Wolf3DSim\Palettes\Wolf3D.pal")
            .Read(@"WOLF3D\VSWAP.WL1")
        );

        GameMaps maps = new GameMaps().Read(@"WOLF3D\MAPHEAD.WL1", @"WOLF3D\GAMEMAPS.WL1");

        MapWalls mapWalls = new MapWalls().Load(maps.Maps[0]);
        foreach (Sprite3D sprite in mapWalls.Walls)
            AddChild(sprite);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    //  public override void _Process(float delta)
    //  {
    //      
    //  }

    public Game PlayASound()
    {
        AudioStreamSample audioStreamSample = new AudioStreamSample()
        {
            Data = VSwap.ConcatArrays(
                Assets.VSwap.Pages[Assets.VSwap.SoundPage],
                Assets.VSwap.Pages[Assets.VSwap.SoundPage + 1]
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
        return this;
    }
}
