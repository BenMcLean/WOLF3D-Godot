using Godot;
using System;
using System.Xml.Linq;
using WOLF3D.WOLF3DGame.Action;
using WOLF3D.WOLF3DGame.OPL;

namespace WOLF3D.WOLF3DGame.Menu
{
    public class MenuRoom : Room
    {
        public static byte Episode { get; set; } = 0;
        public static byte Difficulty { get; set; } = 0;

        public ARVRController ActiveController { get; set; }
        public ARVRController InactiveController => ActiveController == RightController ? LeftController : RightController;

        public MenuBody MenuBody { get; set; }
        public MenuScreen Menu
        {
            get => MenuBody?.MenuScreen;
            set
            {
                if (MenuBody != null)
                    MenuBody.MenuScreen = value;
            }
        }

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

        public void ButtonPressedRight(int buttonIndex) => MenuBody.MenuScreen.ButtonPressed(this, buttonIndex, true);
        public void ButtonPressedLeft(int buttonIndex) => MenuBody.MenuScreen.ButtonPressed(this, buttonIndex, false);

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

        public override void _Input(InputEvent @event) => MenuBody?.MenuScreen?.DoInput(@event);

        public MenuRoom Action(XElement xml)
        {
            if (xml == null || !Main.InGameMatch(xml))
                return this;
            if (byte.TryParse(xml.Attribute("Episode")?.Value, out byte episode))
                Episode = episode;
            if (byte.TryParse(xml.Attribute("Difficulty")?.Value, out byte difficulty))
                Difficulty = difficulty;
            if ((xml.Attribute("Action")?.Value.Equals("Menu", StringComparison.InvariantCultureIgnoreCase) ?? false) &&
                Assets.Menu(xml.Attribute("Argument").Value) is MenuScreen menuScreen &&
                menuScreen != null)
                MenuBody.MenuScreen = menuScreen;
            if (xml.Attribute("Action")?.Value.Equals("Modal", StringComparison.InvariantCultureIgnoreCase) ?? false)
                MenuBody.MenuScreen.AddModal(xml.Attribute("Argument").Value);
            if (xml.Attribute("Action")?.Value.Equals("NewGame", StringComparison.InvariantCultureIgnoreCase) ?? false)
                Main.Room = new LoadingRoom(0, Episode, Difficulty);
            if (xml.Attribute("Action")?.Value.Equals("Resume", StringComparison.InvariantCultureIgnoreCase) ?? false)
                Main.Room = Main.ActionRoom;
            if (xml.Attribute("Action")?.Value.Equals("Quit", StringComparison.InvariantCultureIgnoreCase) ?? false)
            {
                MenuScreen.AddModal(Main.RNG.RandomElement(Assets.EndStrings));
                MenuScreen.Question = Modal.Question.QUIT;
                MenuScreen.Modal.YesNo = true;
            }
            return this;
        }

        MenuScreen MenuScreen => MenuBody.MenuScreen;
    }
}
