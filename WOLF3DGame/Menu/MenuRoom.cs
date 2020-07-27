using Godot;
using System;
using System.Xml.Linq;
using WOLF3D.WOLF3DGame.Action;
using WOLF3D.WOLF3DGame.OPL;

namespace WOLF3D.WOLF3DGame.Menu
{
    public class MenuRoom : Room
    {
        public ARVRController ActiveController { get; set; }
        public ARVRController InactiveController => ActiveController == RightController ? LeftController : RightController;

        public static byte Episode { get; set; } = 0;
        public static byte Difficulty { get; set; } = 0;

        public MenuBody Body { get; set; }
        public MenuScreen Menu
        {
            get => Body?.MenuScreen;
            set
            {
                if (Body != null)
                    Body.MenuScreen = value;
            }
        }

        public MenuRoom() : this(Assets.Menu("Main")) { }

        public MenuRoom(MenuScreen menuScreen)
        {
            Name = "MenuRoom";
            AddChild(ARVROrigin = new ARVROrigin());
            ARVROrigin.AddChild(ARVRCamera = new FadeCamera()
            {
                Current = true,
            });
            ARVROrigin.AddChild(LeftController = new ARVRController()
            {
                ControllerId = 1,
            });
            ARVROrigin.AddChild(RightController = new ARVRController()
            {
                ControllerId = 2,
            });
            Spatial controller = (Spatial)GD.Load<PackedScene>("res://OQ_Toolkit/OQ_ARVRController/models3d/OculusQuestTouchController_Left.gltf").Instance();
            controller.Rotate(controller.Transform.basis.x.Normalized(), -Mathf.Pi / 4f);
            LeftController.AddChild(controller);
            controller = (Spatial)GD.Load<PackedScene>("res://OQ_Toolkit/OQ_ARVRController/models3d/OculusQuestTouchController_Right.gltf").Instance();
            controller.Rotate(controller.Transform.basis.x.Normalized(), -Mathf.Pi / 4f);
            RightController.AddChild(controller);
            ActiveController = RightController;
            AddChild(Body = new MenuBody(menuScreen)
            {
                Transform = new Transform(Basis.Identity, new Vector3(0f, 0f, -1.5f)),
            });
        }

        public override void Enter()
        {
            base.Enter();
            StartMusic();
            if (Body != null && Body.MenuScreen != null && Body.MenuScreen.Color != null)
                Main.Color = Body.MenuScreen.Color;
            LeftController.Connect("button_pressed", this, nameof(ButtonPressedLeft));
            RightController.Connect("button_pressed", this, nameof(ButtonPressedRight));
        }

        public MenuRoom StartMusic()
        {
            if (Assets.XML?.Element("VgaGraph")?.Element("Menus")?.Attribute("MenuSong") is XAttribute menuSong)
                SoundBlaster.Song = Assets.Song(menuSong.Value);
            return this;
        }

        public override void Exit()
        {
            base.Exit();
            if (LeftController.IsConnected("button_pressed", this, nameof(ButtonPressedLeft)))
                LeftController.Disconnect("button_pressed", this, nameof(ButtonPressedLeft));
            if (RightController.IsConnected("button_pressed", this, nameof(ButtonPressedRight)))
                RightController.Disconnect("button_pressed", this, nameof(ButtonPressedRight));
        }

        public void ButtonPressedRight(int buttonIndex) => Body.MenuScreen.ButtonPressed(this, buttonIndex, true);
        public void ButtonPressedLeft(int buttonIndex) => Body.MenuScreen.ButtonPressed(this, buttonIndex, false);

        public override void _PhysicsProcess(float delta)
        {
            if (Paused)
                PausedProcess(delta);
            else
            {
                ARVROrigin.Transform = new Transform(
                    Basis.Identity,
                    new Vector3(
                        -ARVRCamera.Transform.origin.x,
                        Assets.HalfWallHeight - ARVRCamera.Transform.origin.y,
                        -ARVRCamera.Transform.origin.z
                    )
                );

                Godot.Collections.Dictionary CastRay(ARVRController controller) => GetWorld()
                    .DirectSpaceState.IntersectRay(
                        controller.GlobalTransform.origin,
                        controller.GlobalTransform.origin + ARVRPlayer.ARVRControllerDirection(controller.GlobalTransform.basis) * Assets.ShotRange
                    );
                if (CastRay(ActiveController) is Godot.Collections.Dictionary result &&
                    result.Count > 0 &&
                    result["position"] is Vector3 position &&
                    position != null)
                    Body.Target(position);
                else if ((CastRay(InactiveController) is Godot.Collections.Dictionary result2 &&
                    result2.Count > 0 &&
                    result2["position"] is Vector3 position2 &&
                    position2 != null))
                {
                    ActiveController = InactiveController;
                    Body.Target(position2);
                }
                else
                    Body.Target();
            }
        }

        public override void _Input(InputEvent @event)
        {
            if (!Paused) Body?.MenuScreen?.DoInput(@event);
        }

        public MenuScreen MenuScreen
        {
            get => Body.MenuScreen;
            set => Body.MenuScreen = value;
        }

        public class MenuBody : StaticBody
        {
            public const float Width = Assets.WallWidth;
            public const float Height = Width / 4f * 3f;
            public static readonly BoxShape MenuScreenShape = new BoxShape()
            {
                Extents = new Vector3(Width / 2f, Height / 2f, Assets.PixelWidth / 2f),
            };
            public MenuScreen MenuScreen
            {
                get => menuScreen;
                set
                {
                    if (menuScreen != null)
                        RemoveChild(menuScreen);
                    AddChild(menuScreen = value);
                    ((SpatialMaterial)(MeshInstance.MaterialOverride)).AlbedoTexture = menuScreen.GetTexture();
                }
            }
            private MenuScreen menuScreen = null;
            public CollisionShape Shape { get; private set; }
            public MeshInstance MeshInstance { get; private set; }

            public MenuBody(MenuScreen menuScreen)
            {
                Name = "MenuBody";
                AddChild(Shape = new CollisionShape()
                {
                    Shape = MenuScreenShape,
                    Transform = new Transform(Basis.Identity, new Vector3(0f, Assets.HalfWallHeight, -Assets.PixelWidth)),
                });
                Shape.AddChild(MeshInstance = new MeshInstance()
                {
                    Mesh = new QuadMesh()
                    {
                        Size = new Vector2(Width, Height),
                    },
                    MaterialOverride = new SpatialMaterial()
                    {
                        FlagsUnshaded = true,
                        FlagsDoNotReceiveShadows = true,
                        FlagsDisableAmbientLight = true,
                        ParamsSpecularMode = SpatialMaterial.SpecularMode.Disabled,
                        ParamsCullMode = SpatialMaterial.CullMode.Back,
                        FlagsTransparent = false,
                    },
                    Transform = new Transform(Basis.Identity, new Vector3(0f, 0f, Assets.PixelWidth)),
                });
                MenuScreen = menuScreen;
            }

            public bool Target(Vector3? position = null) => position is Vector3 vector3 ? TargetLocal(ToLocal(vector3)) : TargetLocal();

            public bool TargetLocal(Vector3? localPosition = null) =>
                MenuScreen == null ? false
                : MenuScreen.Target(localPosition == null ? MenuScreen.OffScreen
                    : new Vector2(
                        (((Vector3)localPosition).x + (Width / 2f)) / Width * MenuScreen.Width,
                         MenuScreen.Height - (((Vector3)localPosition).y - Assets.HalfWallHeight + Height / 2f) / Height * MenuScreen.Height
                      ));
        }

        public MenuRoom Action(XElement xml)
        {
            if (xml == null || !Main.InGameMatch(xml))
                return this;
            if (byte.TryParse(xml.Attribute("Episode")?.Value, out byte episode))
                Episode = episode;
            if (byte.TryParse(xml.Attribute("Difficulty")?.Value, out byte difficulty))
                Difficulty = difficulty;
            if (xml.Attribute("VRMode")?.Value is string vrMode && !string.IsNullOrWhiteSpace(vrMode))
                Settings.SetVrMode(vrMode);
            if (xml.Attribute("FX")?.Value is string fx && !string.IsNullOrWhiteSpace(fx))
                Settings.SetFX(fx);
            if (xml.Attribute("DigiSound")?.Value is string d && !string.IsNullOrWhiteSpace(d))
                Settings.SetDigiSound(d);
            if (xml.Attribute("Music")?.Value is string m && !string.IsNullOrWhiteSpace(m))
                Settings.SetMusic(m);
            if (xml.Attribute("Action")?.Value.Equals("Cancel", StringComparison.InvariantCultureIgnoreCase) ?? false)
                MenuScreen.Cancel();
            if ((xml.Attribute("Action")?.Value.Equals("Menu", StringComparison.InvariantCultureIgnoreCase) ?? false) &&
                Assets.Menu(xml.Attribute("Argument").Value) is MenuScreen menuScreen &&
                menuScreen != null)
            {
                MenuScreen = menuScreen;
                if (Main.Room != Main.MenuRoom)
                    Main.Room.ChangeRoom(Main.MenuRoom);
            }
            if (xml.Attribute("Action")?.Value.Equals("Modal", StringComparison.InvariantCultureIgnoreCase) ?? false)
                Body.MenuScreen.AddModal(xml.Attribute("Argument").Value);
            if (xml.Attribute("Action")?.Value.Equals("Update", StringComparison.InvariantCultureIgnoreCase) ?? false)
                MenuScreen.Update();
            if (xml.Attribute("Action")?.Value.Equals("NewGame", StringComparison.InvariantCultureIgnoreCase) ?? false)
            {
                Settings.Episode = Episode;
                Settings.Difficulty = Difficulty;
                Main.NextLevelStats = null;
                ChangeRoom(new LoadingRoom(0));
            }
            if (xml.Attribute("Action")?.Value.Equals("NextFloor", StringComparison.InvariantCultureIgnoreCase) ?? false)
                ChangeRoom(new LoadingRoom(Main.ActionRoom.NextMap));
            if (xml.Attribute("Action")?.Value.Equals("End", StringComparison.InvariantCultureIgnoreCase) ?? false)
            {
                MenuScreen.AddModal(xml.Attribute("Argument")?.Value ?? "Are you sure you want\nto end the game you\nare currently playing?");
                MenuScreen.Question = Modal.QuestionEnum.END;
                MenuScreen.Modal.YesNo = true;
            }
            if (xml.Attribute("Action")?.Value.Equals("Resume", StringComparison.InvariantCultureIgnoreCase) ?? false)
                ChangeRoom(Main.ActionRoom);
            if (xml.Attribute("Action")?.Value.Equals("Quit", StringComparison.InvariantCultureIgnoreCase) ?? false)
            {
                MenuScreen.AddModal(xml.Attribute("Argument")?.Value ?? Main.RNG.RandomElement(Assets.EndStrings));
                MenuScreen.Question = Modal.QuestionEnum.QUIT;
                MenuScreen.Modal.YesNo = true;
            }
            return this;
        }
    }
}
