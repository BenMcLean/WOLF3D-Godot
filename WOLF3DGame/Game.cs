using Godot;
using NScumm.Audio.OPL.Woody;
using OPL;
using System.Text;
using WOLF3DGame.Model;

namespace WOLF3DGame
{
    public class Game : Spatial
    {
        public static Assets Assets { get; set; }
        public static string Folder { get; set; }
        public ARVRInterface ARVRInterface { get; set; }
        public ARVRPlayer ARVRPlayer { get; set; }
        public Level Level { get; set; }
        public static Line3D Line3D { get; set; }

        public override void _Ready()
        {
            VisualServer.SetDefaultClearColor(Color.Color8(0, 0, 0, 255));
            AddChild(ARVRPlayer = new ARVRPlayer()
            {
                //Roomscale = false,
            }
            );
            Spatial controller = (Spatial)GD.Load<PackedScene>("res://OQ_Toolkit/OQ_ARVRController/models3d/OculusQuestTouchController_Left.gltf").Instance();
            controller.Rotate(controller.Transform.basis.x.Normalized(), -Mathf.Pi / 4f);
            ARVRPlayer.LeftController.AddChild(controller);
            controller = (Spatial)GD.Load<PackedScene>("res://OQ_Toolkit/OQ_ARVRController/models3d/OculusQuestTouchController_Right.gltf").Instance();
            controller.Rotate(controller.Transform.basis.x.Normalized(), -Mathf.Pi / 4f);
            ARVRPlayer.RightController.AddChild(controller);

            Assets = new Assets(Folder);

            AddChild(Assets.OplPlayer = new OplPlayer()
            {
                Opl = new WoodyEmulatorOpl(NScumm.Core.Audio.OPL.OplType.Opl3)
            });

            GameMap map = Assets.Maps[0];

            AddChild(Level = new Level(map));

            ARVRPlayer.GlobalTransform = Level.StartTransform;

            //Assets.OplPlayer.ImfPlayer.Song = Assets.AudioT.Songs[14];
            //Assets.OplPlayer.AdlPlayer.Adl = Assets.AudioT.Sounds[31];
            //PlayASound();
            ARVRPlayer.RightController.Connect("button_pressed", this, nameof(ButtonPressed));

            ARVRPlayer.CanWalk = Level.CanWalk;

            AddChild(Line3D = new Line3D()
            {
                Color = Color.Color8(255, 0, 0, 255),
            });

            //StringBuilder stringBuilder = new StringBuilder();
            //for (int sx = 0; sx < Level.Map.Width; sx++)
            //{
            //    for (int sz = Level.Map.Depth - 1; sz >= 0; sz--)
            //        stringBuilder.Append(Level.CollisionShapes[sx][sz] == null ? " " :
            //        Level.CollisionShapes[sx][sz].Disabled ? "_" : "X");
            //    stringBuilder.Append("\n");
            //}
            //GD.Print(stringBuilder.ToString());
        }

        public static Vector3 BillboardRotation { get; set; }

        public override void _PhysicsProcess(float delta)
        {
            base._Process(delta);
            BillboardRotation = new Vector3(0f, GetViewport().GetCamera().GlobalTransform.basis.GetEuler().y, 0f);
        }

        public override void _Input(InputEvent @event)
        {
            base._Input(@event);
            if (@event is InputEventKey inputEventKey && inputEventKey.Pressed && !inputEventKey.Echo && inputEventKey.Scancode == (uint)KeyList.X)
                print();
        }

        public void ButtonPressed(int buttonIndex)
        {
            if (buttonIndex == (int)JoystickList.OculusAx)
                print();
        }

        public void print()
        {
            StringBuilder stringBuilder = new StringBuilder().Append("Squares occupied: {");
            foreach (ushort square in Level.SquaresOccupied(ARVRPlayer.PlayerPosition))
                stringBuilder.Append("[")
                    .Append(Level.Map.X(square))
                    .Append(", ")
                    .Append(Level.Map.Z(square))
                    .Append("] ");
            GD.Print(stringBuilder.Append("}").ToString());
        }

        public Game PlayASound()
        {
            AudioStreamPlayer audioStreamPlayer = new AudioStreamPlayer()
            {
                Stream = Assets.DigiSounds[32],
                VolumeDb = 0.01f
            };
            AddChild(audioStreamPlayer);
            audioStreamPlayer.Play();
            return this;
        }
    }
}
