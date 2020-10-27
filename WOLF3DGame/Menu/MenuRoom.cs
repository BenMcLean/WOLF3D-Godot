using Godot;
using System;
using System.Linq;
using System.Xml.Linq;
using WOLF3D.WOLF3DGame.Action;
using WOLF3DModel;

namespace WOLF3D.WOLF3DGame.Menu
{
    public class MenuRoom : Room
    {
        public ARVRController ActiveController { get; set; }
        public ARVRController InactiveController => ActiveController == RightController ? LeftController : RightController;

        public static byte Episode { get; set; } = 1;
        public static byte Difficulty { get; set; } = 1;

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

        public MenuRoom() : this(Assets.Menu(Assets.XML?.Element("VgaGraph")?.Element("Menus")?.Attribute("Start")?.Value ?? "Main")) { }

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
            if (Main.VR)
            {
                Spatial controller = (Spatial)GD.Load<PackedScene>("res://OQ_Toolkit/OQ_ARVRController/models3d/OculusQuestTouchController_Left.gltf").Instance();
                controller.Rotate(controller.Transform.basis.x.Normalized(), -Mathf.Pi / 4f);
                LeftController.AddChild(controller);
                controller = (Spatial)GD.Load<PackedScene>("res://OQ_Toolkit/OQ_ARVRController/models3d/OculusQuestTouchController_Right.gltf").Instance();
                controller.Rotate(controller.Transform.basis.x.Normalized(), -Mathf.Pi / 4f);
                RightController.AddChild(controller);
            }
            ActiveController = RightController;
            AddChild(Body = new MenuBody(menuScreen)
            {
                Transform = new Transform(Basis.Identity, new Vector3(0f, 0f, -1.5f)),
            });
        }

        public override void Enter()
        {
            base.Enter();
            MenuScreen?.OnSet();
            LeftController.Connect("button_pressed", this, nameof(ButtonPressedLeft));
            RightController.Connect("button_pressed", this, nameof(ButtonPressedRight));
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

                if (Main.VR)
                {
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
        }

        public override void _Input(InputEvent @event)
        {
            if (!Paused) Body?.MenuScreen?.DoInput(@event);
        }

        public MenuScreen MenuScreen
        {
            get => Body?.MenuScreen;
            set => Body.MenuScreen = value.OnSet();
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
                : MenuScreen.IsIn(localPosition == null ? MenuScreen.OffScreen
                    : new Vector2(
                        (((Vector3)localPosition).x + (Width / 2f)) / Width * MenuScreen.Width,
                         MenuScreen.Height - (((Vector3)localPosition).y - Assets.HalfWallHeight + Height / 2f) / Height * MenuScreen.Height
                      ));
        }

        public static ushort LastPushedTile { get; set; } = 0;

    }
}
