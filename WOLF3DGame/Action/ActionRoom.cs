using Godot;
using NScumm.Audio.OPL.Woody;
using WOLF3DGame.Model;
using WOLF3DGame.OPL;

namespace WOLF3DGame.Action
{
    public class ActionRoom : Spatial
    {
        public ARVRInterface ARVRInterface { get; set; }
        public ARVRPlayer ARVRPlayer { get; set; }
        public Level Level { get; set; } = null;
        public static Line3D Line3D { get; set; }
        public ushort NextMap => (ushort)(MapNumber + 1 >= Assets.Maps.Length ? 0 : MapNumber + 1);
        public GameMap Map => Assets.Maps[MapNumber];
        public ushort MapNumber
        {
            get => mapNumber;
            set
            {
                mapNumber = value;
                if (Level != null)
                    RemoveChild(Level);

                AddChild(Level = new Level(Map)
                {
                    ARVRPlayer = ARVRPlayer,
                });

                ARVRPlayer.GlobalTransform = Level.StartTransform;
                ARVRPlayer.Walk = Level.Walk;
                ARVRPlayer.Push = Level.Push;

                Assets.OplPlayer.ImfPlayer.Song = Assets.AudioT.Songs[Map.Song];
            }
        }
        private ushort mapNumber = 0;

        public override void _Ready()
        {
            VisualServer.SetDefaultClearColor(Color.Color8(0, 0, 0, 255));
            AddChild(Assets.OplPlayer = new OplPlayer()
            {
                Opl = new WoodyEmulatorOpl(NScumm.Core.Audio.OPL.OplType.Opl3)
            });
            AddChild(ARVRPlayer = new ARVRPlayer()
            {
                Roomscale = false,
            }
            );
            Spatial controller = (Spatial)GD.Load<PackedScene>("res://OQ_Toolkit/OQ_ARVRController/models3d/OculusQuestTouchController_Left.gltf").Instance();
            controller.Rotate(controller.Transform.basis.x.Normalized(), -Mathf.Pi / 4f);
            ARVRPlayer.LeftController.AddChild(controller);
            controller = (Spatial)GD.Load<PackedScene>("res://OQ_Toolkit/OQ_ARVRController/models3d/OculusQuestTouchController_Right.gltf").Instance();
            controller.Rotate(controller.Transform.basis.x.Normalized(), -Mathf.Pi / 4f);
            ARVRPlayer.RightController.AddChild(controller);

            MapNumber = 0;

            //Assets.OplPlayer.AdlPlayer.Adl = Assets.AudioT.Sounds[31];
            //PlayASound();
            ARVRPlayer.RightController.Connect("button_pressed", this, nameof(ButtonPressed));

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
            base._PhysicsProcess(delta);
            BillboardRotation = new Vector3(0f, GetViewport().GetCamera().GlobalTransform.basis.GetEuler().y, 0f);
        }

        public override void _Input(InputEvent @event)
        {
            base._Input(@event);
            if (@event.IsActionPressed("toggle_fullscreen"))
                OS.WindowFullscreen = !OS.WindowFullscreen;
            if (@event.IsActionPressed("ui_cancel"))
                Main.Scene = Main.MenuRoom;
            if (@event is InputEventKey inputEventKey && inputEventKey.Pressed && !inputEventKey.Echo)
                switch (inputEventKey.Scancode)
                {
                    case (uint)KeyList.X:
                        print();
                        break;
                    case (uint)KeyList.Z:
                        MapNumber = NextMap;
                        break;
                }
        }

        public void ButtonPressed(int buttonIndex)
        {
            if (buttonIndex == (int)JoystickList.OculusAx)
                print();
            if (buttonIndex == (int)JoystickList.OculusBy)
                MapNumber = NextMap;
        }

        public void print()
        {
            GD.Print("Left joystick: {" + ARVRPlayer.LeftController.GetJoystickAxis(0) + ", " + ARVRPlayer.LeftController.GetJoystickAxis(1) + "} Right joystick: " + ARVRPlayer.RightController.GetJoystickAxis(0) + ", " + ARVRPlayer.RightController.GetJoystickAxis(1) + "}");
            //StringBuilder stringBuilder = new StringBuilder().Append("Squares occupied: {");
            //foreach (ushort square in Level.SquaresOccupied(ARVRPlayer.PlayerPosition))
            //    stringBuilder.Append("[")
            //        .Append(Level.Map.X(square))
            //        .Append(", ")
            //        .Append(Level.Map.Z(square))
            //        .Append("] ");
            //GD.Print(stringBuilder.Append("}").ToString());
        }

        public ActionRoom PlayASound()
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
