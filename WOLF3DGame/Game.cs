using Godot;
using NScumm.Audio.OPL.Woody;
using NScumm.Core.Audio.OPL.DosBox;
using OPL;
using WOLF3DGame.Model;

namespace WOLF3DGame
{
    public class Game : Spatial
    {
        public static Assets Assets;
        public string Folder { get; set; }

        public override void _Ready()
        {
            Assets = new Assets(Folder);

            AddChild(Assets.OplPlayer = new OplPlayer()
            {
                Opl = new WoodyEmulatorOpl(NScumm.Core.Audio.OPL.OplType.Opl3)
            });

            GameMap map = Assets.Maps[0];

            MapWalls = new MapWalls(map);
            foreach (Sprite3D sprite in MapWalls.Walls)
                AddChild(sprite);

            //GetViewport().GetCamera().GlobalTranslate(new Vector3((x + 0.5f) * Assets.WallWidth, (float)Assets.WallHeight / 2f, (z + 4.5f) * Assets.WallWidth));

            foreach (Billboard billboard in Billboard.MakeBillboards(map))
                AddChild(billboard);

            //Assets.OplPlayer.ImfPlayer.Song = Assets.AudioT.Songs[14];
            //Assets.OplPlayer.AdlPlayer.Adl = Assets.AudioT.Sounds[31];
            //PlayASound();

            var vr = ARVRServer.FindInterface("OpenVR");
            if (vr != null && vr.Initialize())
            {
                GetViewport().Arvr = true;
                GetViewport().Hdr = false;

                OS.VsyncEnabled = false;
                Engine.TargetFps = 90;
                // Also, the physics FPS in the project settings is also 90 FPS. This makes the physics
                // run at the same frame rate as the display, which makes things look smoother in VR!
            }
        }

        public MapWalls MapWalls;

        public Game PlayASound()
        {
            AudioStreamSample audioStreamSample = new AudioStreamSample()
            {
                Data = Assets.VSwap.DigiSounds[32],
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
}