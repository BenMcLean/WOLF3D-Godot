using Godot;
using WOLF3DGame.Model;

namespace WOLF3DGame.Menu
{
    public class MenuRoom : Spatial
    {
        public ARVROrigin ARVROrigin { get; set; }
        public ARVRCamera ARVRCamera { get; set; }
        public ARVRController LeftController { get; set; }
        public ARVRController RightController { get; set; }
        public MenuScreen MenuScreen { get; set; }
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
            ARVROrigin.AddChild(RightController = new ARVRController()
            {
                ControllerId = 2,
            });
            AddChild(MenuScreen = new MenuScreen()
            {
                Transform = new Transform(Basis.Identity, new Vector3(0f, 0f, -1.5f)),
            });
            if (Assets.OplPlayer != null && Assets.OplPlayer.ImfPlayer != null)
                Assets.OplPlayer.ImfPlayer.Song = Assets.Song(Assets.XML.Element("VgaGraph").Element("Menus").Attribute("MenuSong").Value);
        }

        public override void _PhysicsProcess(float delta)
        {
            base._PhysicsProcess(delta);
            ARVROrigin.Transform = new Transform(
                Basis.Identity,
                new Vector3(
                    -ARVRCamera.Transform.origin.x,
                    Assets.HalfWallHeight - ARVRCamera.Transform.origin.y,
                    -ARVRCamera.Transform.origin.z
                )
            );
        }
    }
}
