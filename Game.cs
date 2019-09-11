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
        Assets = new Assets(Folder);

        AddChild(Assets.OplPlayer = new OplPlayer(
            new DosBoxOPL(NScumm.Core.Audio.OPL.OplType.Opl3)
        ));

        Map map = Assets.GameMaps.Maps[0];

        MapWalls = new MapWalls(map);
        foreach (Sprite3D sprite in MapWalls.Walls)
            AddChild(sprite);

        map.StartPosition(out ushort x, out ushort z);
        GetViewport().GetCamera().GlobalTranslate(new Vector3((x + 0.5f) * Assets.WallWidth, (float)Assets.WallHeight / 2f, (z + 4.5f) * Assets.WallWidth));

        foreach (Billboard billboard in Billboard.MakeBillboards(map))
            AddChild(billboard);

        using (MemoryStream song = new MemoryStream(Assets.AudioT.AudioTFile[264]))
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
