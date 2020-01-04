using Godot;
using NScumm.Audio.OPL.Woody;
using OPL;
using WOLF3DGame.Model;

namespace WOLF3DGame
{
    public class Game : Spatial
    {
        public static Assets Assets { get; set; }
        public static string Folder { get; set; }
        public ARVRInterface ARVRInterface { get; set; }
        public ARVRPlayer ARVRPlayer { get; set; }
        public CollisionShape PlayerHead { get; set; }
        public MeshInstance Floor { get; set; }
        public MeshInstance Ceiling { get; set; }

        public bool Roomscale = false;

        public override void _Ready()
        {
            VisualServer.SetDefaultClearColor(Color.Color8(0, 0, 0, 255));
            AddChild(ARVRPlayer = new ARVRPlayer());
            ARVRPlayer.LeftController.AddChild(GD.Load<PackedScene>("res://OQ_Toolkit/OQ_ARVRController/models3d/OculusQuestTouchController_Left.gltf").Instance());
            ARVRPlayer.RightController.AddChild(GD.Load<PackedScene>("res://OQ_Toolkit/OQ_ARVRController/models3d/OculusQuestTouchController_Right.gltf").Instance());
            ARVRPlayer.ARVRCamera.AddChild(PlayerHead = new CollisionShape()
            {
                Name = "Player's head",
                Shape = new SphereShape()
                {
                    Radius = 0.5f,
                },
            });

            Assets = new Assets(Folder);

            AddChild(Assets.OplPlayer = new OplPlayer()
            {
                Opl = new WoodyEmulatorOpl(NScumm.Core.Audio.OPL.OplType.Opl3)
            });

            GameMap map = Assets.Maps[0];

            AddChild(new Level(map));

            map.StartPosition(out ushort x, out ushort z);
            ARVRPlayer.GlobalTranslate(new Vector3((x + 0.5f) * Assets.WallWidth, 0f, (z + 4.5f) * Assets.WallWidth));

            //Assets.OplPlayer.ImfPlayer.Song = Assets.AudioT.Songs[14];
            //Assets.OplPlayer.AdlPlayer.Adl = Assets.AudioT.Sounds[31];
            //PlayASound();
            ARVRPlayer.RightController.Connect("button_pressed", this, nameof(ButtonPressed));
        }

        public static Vector3 BillboardRotation { get; set; }

        public override void _PhysicsProcess(float delta)
        {
            base._Process(delta);
            BillboardRotation = new Vector3(0f, GetViewport().GetCamera().GlobalTransform.basis.GetEuler().y, 0f);

            Vector3 forward = ARVRPlayer.ARVRCamera.GlobalTransform.basis.z * -1f;
            forward.y = 0f;
            forward = forward.Normalized();
            if (ARVRPlayer.RightController.GetJoystickAxis(1) > Assets.DeadZone || Input.IsKeyPressed((int)KeyList.Up) || Input.IsKeyPressed((int)KeyList.W))
                ARVRPlayer.Translation += forward * Assets.RunSpeed * delta;

            float axis0 = ARVRPlayer.RightController.GetJoystickAxis(0);
            if (Input.IsKeyPressed((int)KeyList.Left))
                axis0 -= 1;
            if (Input.IsKeyPressed((int)KeyList.Right))
                axis0 += 1;

            if (Mathf.Abs(axis0) > Assets.DeadZone)
            {
                Vector3 origHeadPos = ARVRPlayer.ARVRCamera.GlobalTransform.origin;
                ARVRPlayer.ARVROrigin.Rotate(Vector3.Up, Mathf.Pi * delta * (axis0 > 0f ? -1f : 1f));
                ARVRPlayer.ARVROrigin.GlobalTransform = new Transform(ARVRPlayer.ARVROrigin.GlobalTransform.basis, ARVRPlayer.ARVROrigin.GlobalTransform.origin + origHeadPos - ARVRPlayer.ARVRCamera.GlobalTransform.origin).Orthonormalized();
            }
            ARVRPlayer.ARVROrigin.GlobalTransform = new Transform(ARVRPlayer.ARVROrigin.GlobalTransform.basis, new Vector3(
                ARVRPlayer.ARVROrigin.GlobalTransform.origin.x,
                Roomscale ?
                0f
                : (float)Assets.HalfWallHeight - ARVRPlayer.ARVRCamera.Transform.origin.y,
                ARVRPlayer.ARVROrigin.GlobalTransform.origin.z
                ));
        }

        public override void _Input(InputEvent @event)
        {
            base._Input(@event);
            if (@event is InputEventKey inputEventKey && inputEventKey.Pressed && !inputEventKey.Echo && inputEventKey.Scancode == (uint)KeyList.X)
                GD.Print("you pressed a key!");
        }

        public void ButtonPressed(int buttonIndex)
        {
            if (buttonIndex == (int)JoystickList.OculusAx)
                print();
        }

        public void print()
        {
            Vector2 playerPosition = PlayerPosition;
            GD.Print("You are at: " + playerPosition.x + ", " + playerPosition.y);
        }

        public Vector2 PlayerPosition
        {
            get => new Vector2(ARVRPlayer.ARVRCamera.GlobalTransform.origin.x, ARVRPlayer.ARVRCamera.GlobalTransform.origin.z);
            set => ARVRPlayer.GlobalTransform = new Transform(
                    ARVRPlayer.GlobalTransform.basis,
                    new Vector3(
                        value.x,
                        Roomscale ?
                        0f
                        : (float)Assets.HalfWallHeight - ARVRPlayer.ARVRCamera.Transform.origin.y,
                        value.y
                    )
                );
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
