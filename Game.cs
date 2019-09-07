using Godot;
using System;
using System.IO;
using System.Text;
using System.Xml.Linq;
using WOLF3D;
using WOLF3DSim;
using static WOLF3DSim.GameMaps;

public class Game : Spatial
{
    public static Assets Assets;
    public static string Folder = "WOLF3D";

    public override void _Ready()
    {
        DownloadShareware.Main(new string[] { Folder });
        XElement xml;
        using (FileStream game = new FileStream(System.IO.Path.Combine(Folder, "game.xml"), FileMode.Open))
            xml = XElement.Load(game);
        using (MemoryStream palette = new MemoryStream(Encoding.ASCII.GetBytes(xml.Element("Palette").Value)))
        using (FileStream vswap = new FileStream(System.IO.Path.Combine(Folder, "VSWAP.WL1"), FileMode.Open))
        using (FileStream mapHead = new FileStream(System.IO.Path.Combine(Folder, "MAPHEAD.WL1"), FileMode.Open))
        using (FileStream gameMaps = new FileStream(System.IO.Path.Combine(Folder, "GAMEMAPS.WL1"), FileMode.Open))
        {
            Assets = new Assets
            {
                Game = xml,
                VSwap = new VSwap(palette, vswap),
                GameMaps = new GameMaps(mapHead, gameMaps),
            };
        }

        Map map = Assets.GameMaps.Maps[0];

        MapWalls = new MapWalls().Load(map);
        foreach (Sprite3D sprite in MapWalls.Walls)
            AddChild(sprite);

        map.StartPosition(out ushort x, out ushort z);

        GetViewport().GetCamera().GlobalTranslate(new Vector3((x + 0.5f) * Assets.WallWidth, (float)Assets.WallHeight / 2f, (z + 4.5f) * Assets.WallWidth));

        foreach (Billboard billboard in Billboard.MakeBillboards(map))
            AddChild(billboard);

        XElement pal = new XElement("Palette");
        using (FileStream palette = new FileStream(@"Wolf3DSim\Palettes\Wolf3D.pal", FileMode.Open))
        using (StreamReader streamReader = new StreamReader(palette))
            pal.Value = streamReader.ReadToEnd();
        pal.Save("palette.xml");
        //Billboard billboard = new Billboard()
        //{
        //    GlobalTransform = new Transform(Basis.Identity, new Vector3((x + 0.5f) * Assets.WallWidth, 0f, (z + 2.5f) * Assets.WallWidth)),
        //};
        //billboard.Sprite3D.Texture = Assets.Textures[201];
        //AddChild(billboard);
    }

    public MapWalls MapWalls;

    ///// <summary>
    ///// Called every frame.
    ///// </summary>
    ///// <param name="delta">'delta' is the elapsed time since the previous frame.</param>
    //public override void _Process(float delta)
    //{
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
