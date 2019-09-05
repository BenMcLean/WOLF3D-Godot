using Godot;
using System.IO;
using WOLF3D;
using WOLF3DSim;
using static WOLF3DSim.GameMaps;

public class Game : Spatial
{
    public static Assets Assets;

    public override void _Ready()
    {
        DownloadShareware.Main(new string[] { "" });
        using (FileStream palette = new FileStream(@"Wolf3DSim\Palettes\Wolf3D.pal", FileMode.Open))
        using (FileStream file = new FileStream(@"WOLF3D\VSWAP.WL1", FileMode.Open))
            Assets = new Assets(new VSwap(palette, file));

        GameMaps maps;
        using (FileStream mapHead = new FileStream(@"WOLF3D\MAPHEAD.WL1", FileMode.Open))
        using (FileStream gameMaps = new FileStream(@"WOLF3D\GAMEMAPS.WL1", FileMode.Open))
            maps = new GameMaps(mapHead, gameMaps);

        Map map = maps.Maps[0];

        MapWalls = new MapWalls().Load(map);
        foreach (Sprite3D sprite in MapWalls.Walls)
            AddChild(sprite);

        map.StartPosition(out ushort x, out ushort z);

        GetViewport().GetCamera().GlobalTranslate(new Vector3((x + 0.5f) * Assets.WallWidth, (float)Assets.WallHeight / 2f, (z + 4.5f) * Assets.WallWidth));

        Billboard billboard = new Billboard()
        {
            GlobalTransform = new Transform(Basis.Identity, new Vector3((x + 0.5f) * Assets.WallWidth, 0f, (z + 4.5f) * Assets.WallWidth)),
        };
        billboard.Sprite3D.Texture = Assets.Textures[24];
        AddChild(billboard);
    }

    public MapWalls MapWalls;

    ///// <summary>
    ///// Called every frame.
    ///// </summary>
    ///// <param name="delta">'delta' is the elapsed time since the previous frame.</param>
    //public override void _Process(float delta)
    //{
    //    foreach (Sprite3D wall in MapWalls.Walls)
    //        wall.LookAt(CameraFloor, Vector3.Up);
    //}

    public Game PlayASound()
    {
        AudioStreamSample audioStreamSample = new AudioStreamSample()
        {
            Data = VSwap.ConcatArrays(
                Assets.VSwap.Pages[Assets.VSwap.SoundPage],
                Assets.VSwap.Pages[Assets.VSwap.SoundPage + 1]
            ),
            Format = AudioStreamSample.FormatEnum.Format8Bits,
            MixRate = 7042, // Adam Biser said 7042 Hz is the correct frequency
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
