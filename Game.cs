using Godot;
using NScumm.Audio.OPL.Woody;
using NScumm.Core.Audio.OPL.DosBox;
using OPL;
using WOLF3D;

public class Game : Spatial
{
    public static Assets Assets;
    public static string Folder = "WOLF3D";

    public override void _Ready()
    {
        DownloadShareware.Main(new string[] { Folder });
        Assets = new Assets(Folder);

        AddChild(Assets.OplPlayer = new OplPlayer(
            new WoodyEmulatorOpl(NScumm.Core.Audio.OPL.OplType.Opl3)
        ));

        GameMaps.Map map = Assets.GameMaps.Maps[0];

        MapWalls = new MapWalls(map);
        foreach (Sprite3D sprite in MapWalls.Walls)
            AddChild(sprite);

        map.StartPosition(out ushort x, out ushort z);
        GetViewport().GetCamera().GlobalTranslate(new Vector3((x + 0.5f) * Assets.WallWidth, (float)Assets.WallHeight / 2f, (z + 4.5f) * Assets.WallWidth));

        foreach (Billboard billboard in Billboard.MakeBillboards(map))
            AddChild(billboard);

        Assets.OplPlayer.ImfPlayer.Song = Assets.AudioT.Songs[14];
        //Assets.OplPlayer.AdlPlayer.Adl = Assets.AudioT.Sounds[31];

        Godot.Image image = new Image();
        //uint pic = 0;
        //image.CreateFromData(Assets.VgaGraph.Sizes[pic][0], Assets.VgaGraph.Sizes[pic][1], false, Image.Format.Rgba8, Assets.VgaGraph.Pic[pic]);
        uint font = 1;
        uint character = 48;
        image.CreateFromData(Assets.VgaGraph.Fonts[font].Width[character], Assets.VgaGraph.Fonts[font].Height, false, Image.Format.Rgba8, VgaGraph.Font.White(Assets.VgaGraph.Fonts[font].Character[character]));
        ImageTexture imageTexture = new ImageTexture();
        imageTexture.CreateFromImage(image, 0);

        Sprite sprite1 = new Sprite
        {
            Name = "Sprite1",
            Texture = imageTexture,
            Position = new Vector2(200, 300),
            Scale = new Vector2(4f, 4.8f),
        };
        AddChild(sprite1);
    }

    public MapWalls MapWalls;

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
