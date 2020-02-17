using Godot;

namespace WOLF3DGame
{
    class MenuRoom : Spatial
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
                Transform = new Transform(Basis.Identity, new Vector3(0f, 0f, -3f)),
            });
            Game.Assets.OplPlayer.ImfPlayer.Song = Game.Assets.Song(Game.Assets.XML.Element("Menus").Attribute("MenuSong").Value);
        }
    }
}
