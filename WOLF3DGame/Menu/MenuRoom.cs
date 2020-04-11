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
            ActiveController = RightController;
            AddChild(MenuBody = new MenuBody(menuScreen)
            {
                Transform = new Transform(Basis.Identity, new Vector3(0f, 0f, -1.5f)),
            });
        }

        public override void Enter()
        {
            base.Enter();
            if (Assets.XML?.Element("VgaGraph")?.Element("Menus")?.Attribute("MenuSong") is XAttribute menuSong && menuSong != null)
                SoundBlaster.Song = Assets.Song(menuSong.Value);
            if (MenuBody != null && MenuBody.MenuScreen != null && MenuBody.MenuScreen.Color != null)
                Main.Color = MenuBody.MenuScreen.Color;
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
            if (CastRay(ActiveController) is Godot.Collections.Dictionary result &&
                result.Count > 0 &&
                result["position"] is Vector3 position &&
                position != null)
                MenuBody.Target(position);
            else if ((CastRay(InactiveController) is Godot.Collections.Dictionary result2 &&
                result2.Count > 0 &&
                result2["position"] is Vector3 position2 &&
                position2 != null))
            {
                ActiveController = InactiveController;
                MenuBody.Target(position2);
            }
            else
                MenuBody.Target();
        }

        public override void _Input(InputEvent @event)
        {
            if (@event.IsActionPressed("ui_cancel"))
                Main.Room = Main.ActionRoom;
            else
                MenuBody?.MenuScreen?._Input(@event);
        }

        public MenuRoom Action(XElement xml)
        {
            if (xml == null)
                return this;
            if (byte.TryParse(xml.Attribute("Episode")?.Value, out byte episode))
                Main.Episode = episode;
            if (xml.Attribute("Action")?.Value.Equals("Menu", StringComparison.InvariantCultureIgnoreCase) ?? false)
                if (Assets.Menu(xml.Attribute("Argument").Value) is MenuScreen menuScreen && menuScreen != null)
                    MenuBody.MenuScreen = menuScreen;
            return this;
        }
    }
}
