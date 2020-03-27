using Godot;
using WOLF3D.WOLF3DGame.Action;
using WOLF3D.WOLF3DGame.OPL;

namespace WOLF3D.WOLF3DGame.Menu
{
    public class MenuRoom : Spatial
    {
        public ARVROrigin ARVROrigin { get; set; }
        public ARVRCamera ARVRCamera { get; set; }
        public ARVRController LeftController { get; set; }
        public ARVRController RightController { get; set; }
        public MenuBody MenuBody { get; set; }

        public MenuRoom() : this(Assets.Menu("Main")) { }

        public MenuRoom(MenuScreen menuScreen)
        {
            AddChild(ARVROrigin = new ARVROrigin());
            ARVROrigin.AddChild(ARVRCamera = new ARVRCamera()
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
            AddChild(MenuBody = new MenuBody(menuScreen)
            {
                Transform = new Transform(Basis.Identity, new Vector3(0f, 0f, -1.5f)),
            });
        }

        public override void _Ready()
        {
            VisualServer.SetDefaultClearColor(Color.Color8(0, 0, 0, 255));
            SoundBlaster.Song = Assets.Song(Assets.XML.Element("VgaGraph").Element("Menus").Attribute("MenuSong").Value);
        }

        public override void _PhysicsProcess(float delta)
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
            if (CastRay(RightController) is Godot.Collections.Dictionary result &&
                result.Count > 0 &&
                result["position"] is Vector3 position &&
                position != null)
            {
                MenuBody.TargetLocal(MenuBody.ToLocal(position));
                GD.Print("Targeting " + position);
            }
        }

        public override void _Input(InputEvent @event)
        {
            if (@event.IsActionPressed("ui_cancel"))
                Main.Scene = Main.ActionRoom;
        }
    }
}
