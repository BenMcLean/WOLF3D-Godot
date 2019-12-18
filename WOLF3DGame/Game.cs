using Godot;
using NScumm.Audio.OPL.Woody;
using NScumm.Core.Audio.OPL.DosBox;
using OPL;
using WOLF3DGame.Model;

namespace WOLF3DGame
{
    public class Game : Spatial
    {
        public static Assets Assets { get; set; }
        public static string Folder { get; set; }
        public ARVRInterface ARVRInterface { get; set; }
        public ARVROrigin ARVROrigin { get; set; }
        public ARVRCamera ARVRCamera { get; set; }
        public ARVRController LeftController { get; set; }
        public ARVRController RightController { get; set; }
        public MeshInstance Floor { get; set; }
        public MeshInstance Ceiling { get; set; }

        public override void _Ready()
        {
            VisualServer.SetDefaultClearColor(Color.Color8(0, 0, 0, 255));

            AddChild(ARVROrigin = new ARVROrigin());
            ARVROrigin.AddChild(ARVRCamera = new ARVRCamera()
            {
                Current = true,
            });
            ARVROrigin.AddChild(LeftController = new ARVRController()
            {
                ControllerId = 1,
            });
            LeftController.AddChild(GD.Load<PackedScene>("res://OQ_Toolkit/OQ_ARVRController/models3d/OculusQuestTouchController_Left.gltf").Instance());
            ARVROrigin.AddChild(RightController = new ARVRController()
            {
                ControllerId = 2,
            });
            RightController.AddChild(GD.Load<PackedScene>("res://OQ_Toolkit/OQ_ARVRController/models3d/OculusQuestTouchController_Right.gltf").Instance());

            Assets = new Assets(Folder);

            AddChild(Assets.OplPlayer = new OplPlayer()
            {
                Opl = new WoodyEmulatorOpl(NScumm.Core.Audio.OPL.OplType.Opl3)
            });

            GameMap map = Assets.Maps[0];

            AddChild(new Level(map));

            map.StartPosition(out ushort x, out ushort z);
            ARVROrigin.GlobalTranslate(new Vector3((x + 0.5f) * Assets.WallWidth, 0f, (z + 4.5f) * Assets.WallWidth));

            //Assets.OplPlayer.ImfPlayer.Song = Assets.AudioT.Songs[14];
            //Assets.OplPlayer.AdlPlayer.Adl = Assets.AudioT.Sounds[31];
            //PlayASound();
        }

        public static Vector3 BillboardRotation { get; set; }

        public override void _Process(float delta)
        {
            base._Process(delta);
            BillboardRotation = new Vector3(0f, GetViewport().GetCamera().GlobalTransform.basis.GetEuler().y, 0f);

            Vector3 forward = ARVRCamera.GlobalTransform.basis.z * -1f;
            forward.y = 0f;
            forward = forward.Normalized();
            if (RightController.GetJoystickAxis(1) > Assets.DeadZone)
                ARVROrigin.Translation += forward * Assets.RunSpeed * delta;

            float axis0 = RightController.GetJoystickAxis(0);
            if (Mathf.Abs(axis0) > Assets.DeadZone)
            {
                Vector3 origHeadPos = ARVRCamera.GlobalTransform.origin;
                ARVROrigin.Rotate(Vector3.Up, Mathf.Pi * delta * (axis0 > 0f ? -1f : 1f));
                ARVROrigin.GlobalTransform = new Transform(ARVROrigin.GlobalTransform.basis, ARVROrigin.GlobalTransform.origin + origHeadPos - ARVRCamera.GlobalTransform.origin).Orthonormalized();
            }
            //ARVROrigin.GlobalTransform = new Transform(ARVROrigin.GlobalTransform.basis, new Vector3(ARVROrigin.GlobalTransform.origin.x, 0f, ARVROrigin.GlobalTransform.origin.z));
        }

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
