using Godot;
using NScumm.Core.Audio.OPL.DosBox;
using OPL;
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

        AddChild(Assets.OplPlayer = new OPL.OplPlayer(
            Assets.Opl = new DosBoxOPL(NScumm.Core.Audio.OPL.OplType.Opl3)
            ));

        using (FileStream audioHed = new FileStream(System.IO.Path.Combine(Folder, "AUDIOHED.WL1"), FileMode.Open))
        using (FileStream audioTFile = new FileStream(System.IO.Path.Combine(Folder, "AUDIOT.WL1"), FileMode.Open))
            Assets.AudioT = new AudioT(audioHed, audioTFile);

        using (MemoryStream song = new MemoryStream(Assets.AudioT.AudioTFile[273]))
            Assets.OplPlayer.ImfPlayer.Song = Imf.ReadImf(song);
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
