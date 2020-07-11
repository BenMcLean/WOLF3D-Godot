using Godot;
using WOLF3DModel;
using WOLF3D.WOLF3DGame.OPL;
using System.Xml.Linq;
using System.Linq;

namespace WOLF3D.WOLF3DGame.Action
{
    public class ActionRoom : Room
    {
        public override ARVROrigin ARVROrigin
        {
            get => ARVRPlayer.ARVROrigin;
            set => ARVRPlayer.ARVROrigin = value;
        }
        public override FadeCamera ARVRCamera
        {
            get => ARVRPlayer.ARVRCamera;
            set => ARVRPlayer.ARVRCamera = value;
        }
        public override ARVRController LeftController
        {
            get => ARVRPlayer.LeftController;
            set => ARVRPlayer.LeftController = value;
        }
        public override ARVRController RightController
        {
            get => ARVRPlayer.RightController;
            set => ARVRPlayer.RightController = value;
        }
        public ARVRPlayer ARVRPlayer { get; set; }
        public StatusBar StatusBar
        {
            get => statusBar;
            set
            {
                if (statusBar != null)
                    RemoveChild(statusBar);
                statusBar = value;
                if (statusBar != null)
                    AddChild(statusBar);
            }
        }
        private StatusBar statusBar;
        public Level Level { get; set; } = null;
        public static Line3D Line3D { get; set; }
        public ushort NextMap => (ushort)(MapNumber + 1 >= Assets.Maps.Length ? 0 : MapNumber + 1);
        public byte Difficulty { get; set; }
        public byte Episode { get; set; }
        public GameMap Map => Assets.Maps[MapNumber];
        public ushort MapNumber
        {
            get => mapNumber;
            set
            {
                mapNumber = value;
                if (Level != null)
                    RemoveChild(Level);
                AddChild(Level = new Level(Map, Difficulty)
                {
                    ARVRPlayer = ARVRPlayer,
                });
                ARVRPlayer.GlobalTransform = Level.StartTransform;
                ARVRPlayer.Walk = Level.Walk;
                ARVRPlayer.Push = Level.Push;
            }
        }
        private ushort mapNumber = 0;

        public ActionRoom()
        {
            Name = "ActionRoom";
            AddChild(ARVRPlayer = new ARVRPlayer());
            Spatial controller = (Spatial)GD.Load<PackedScene>("res://OQ_Toolkit/OQ_ARVRController/models3d/OculusQuestTouchController_Left.gltf").Instance();
            controller.Rotate(controller.Transform.basis.x.Normalized(), -Mathf.Pi / 4f);
            ARVRPlayer.LeftController.AddChild(controller);
            controller = (Spatial)GD.Load<PackedScene>("res://OQ_Toolkit/OQ_ARVRController/models3d/OculusQuestTouchController_Right.gltf").Instance();
            controller.Rotate(controller.Transform.basis.x.Normalized(), -Mathf.Pi / 4f);
            ARVRPlayer.RightController.AddChild(controller);
            StatusBar = new StatusBar();
            ARVRCamera.AddChild(new MeshInstance()
            {
                Name = "StatusBarTest",
                Mesh = new QuadMesh()
                {
                    Size = new Vector2(Assets.Foot, Assets.Foot / StatusBar.Size.x * StatusBar.Size.y * 1.2f),
                },
                MaterialOverride = new SpatialMaterial()
                {
                    AlbedoTexture = StatusBar.GetTexture(),
                    FlagsUnshaded = true,
                    FlagsDoNotReceiveShadows = true,
                    FlagsDisableAmbientLight = true,
                    FlagsTransparent = false,
                    ParamsCullMode = SpatialMaterial.CullMode.Back,
                    ParamsSpecularMode = SpatialMaterial.SpecularMode.Disabled,
                },
                Transform = new Transform(Basis.Identity, Vector3.Forward / 6 + Vector3.Down / 12),
            });
        }

        public override void _Ready()
        {
            VisualServer.SetDefaultClearColor(Color.Color8(0, 0, 0, 255));

            //SoundBlaster.Adl = Assets.AudioT.Sounds[31];
            //PlayASound();
            ARVRPlayer.RightController.Connect("button_pressed", this, nameof(ButtonPressed));

            AddChild(Line3D = new Line3D()
            {
                Color = Color.Color8(255, 0, 0, 255),
            });

            AddChild(LeftTarget = new MeshInstance()
            {
                Name = "LeftTarget",
                Mesh = TargetMesh,
            });

            AddChild(RightTarget = new MeshInstance()
            {
                Name = "RightTarget",
                Mesh = TargetMesh,
            });
        }

        public static Vector3 BillboardRotation { get; set; }

        public override void _PhysicsProcess(float delta)
        {
            if (Paused)
                PausedProcess(delta);
            if (GetViewport() is Viewport viewport
                && viewport.GetCamera() is Camera camera
                && camera.GlobalTransform is Transform globalTransform)
                BillboardRotation = new Vector3(0f, globalTransform.basis.GetEuler().y, 0f);
        }

        public override void _Input(InputEvent @event)
        {
            if (@event.IsActionPressed("toggle_fullscreen"))
                OS.WindowFullscreen = !OS.WindowFullscreen;
            if (!Main.Room.Paused)
            {
                if (@event.IsActionPressed("ui_cancel"))
                {
                    Main.MenuRoom.Menu = Assets.Menu("Main");
                    ChangeRoom(Main.MenuRoom);
                }
                if (@event is InputEventKey inputEventKey && inputEventKey.Pressed && !inputEventKey.Echo)
                    switch (inputEventKey.Scancode)
                    {
                        case (uint)KeyList.X:
                            Print();
                            break;
                        case (uint)KeyList.Z:
                            ChangeRoom(new LoadingRoom(NextMap));
                            break;
                    }
            }
        }

        public void ButtonPressed(int buttonIndex)
        {
            if (buttonIndex == (int)JoystickList.OculusAx)
                Print();
            if (buttonIndex == (int)JoystickList.OculusBy)
                Main.Room = new LoadingRoom(NextMap);
        }

        public void Print()
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

        Imf[] Song => Assets.AudioT.Songs[Assets.Maps[MapNumber].Song];

        public override void Enter()
        {
            base.Enter();
            Main.Color = Assets.Palette[Assets.Maps[MapNumber].Border];
            if (SoundBlaster.Song != Song)
                SoundBlaster.Song = Song;
        }

        public override void Exit()
        {
            base.Exit();
            Main.NextLevelStats = StatusBar.NextLevelStats();
        }

        public bool Pickup(Pickup pickup)
        {
            if (pickup.IsClose(ARVRPlayer.PlayerPosition) && Conditional(pickup.XML))
            {
                Effect(pickup.XML);
                Level.RemoveChild(pickup);
                return true;
            }
            return false;
        }

        public bool Conditional(XElement xml)
        {
            if (!ConditionalOne(xml))
                return false;
            foreach (XElement conditional in xml?.Elements("Conditional") ?? Enumerable.Empty<XElement>())
                if (!ConditionalOne(conditional))
                    return false;
            return true;
        }

        public bool ConditionalOne(XElement xml) =>
            xml?.Attribute("If")?.Value is string stat
                && !string.IsNullOrWhiteSpace(stat)
                && StatusBar.TryGetValue(stat, out StatusNumber statusNumber)
            ? (
            (
            uint.TryParse(xml?.Attribute("Equals")?.Value, out uint equals)
                    ? statusNumber.Value == equals : true
            )
            &&
            (
            uint.TryParse(xml?.Attribute("LessThan")?.Value, out uint less)
                    ? statusNumber.Value < less : true
            )
            &&
            (
            uint.TryParse(xml?.Attribute("GreaterThan")?.Value, out uint greater)
                    ? statusNumber.Value > greater : true
            )
            ) : true;

        public ActionRoom Effect(XElement xml)
        {
            EffectOne(xml);
            foreach (XElement effect in xml?.Elements("Effect") ?? Enumerable.Empty<XElement>())
                EffectOne(effect);
            return this;
        }

        public ActionRoom EffectOne(XElement xml)
        {
            if (xml?.Attribute("AddTo")?.Value is string stat
                && !string.IsNullOrWhiteSpace(stat)
                && StatusBar.TryGetValue(stat, out StatusNumber statusNumber)
                && uint.TryParse(xml?.Attribute("Add")?.Value, out uint add))
                statusNumber.Value += add;
            SoundBlaster.Play(xml);
            return this;
        }

        public readonly static SphereMesh TargetMesh = new SphereMesh()
        {
            ResourceName = "Target",
            Radius = Assets.PixelHeight / 2f,
            Height = Assets.PixelHeight,
            Material = new SpatialMaterial()
            {
                AlbedoColor = Color.Color8(255, 0, 0, 255),
                FlagsUnshaded = true,
                FlagsDoNotReceiveShadows = true,
                FlagsDisableAmbientLight = true,
                FlagsTransparent = false,
                ParamsCullMode = SpatialMaterial.CullMode.Disabled,
                ParamsSpecularMode = SpatialMaterial.SpecularMode.Disabled,
            },
        };

        public MeshInstance LeftTarget { get; set; }
        public MeshInstance RightTarget { get; set; }
        public MeshInstance Target(bool left) => left ? LeftTarget : RightTarget;
        public MeshInstance Target(int which) => Target(which == 0);
    }
}
