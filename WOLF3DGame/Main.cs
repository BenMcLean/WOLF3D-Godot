using Godot;
using WOLF3D.WOLF3DGame.Action;
using WOLF3D.WOLF3DGame.Menu;
using WOLF3D.WOLF3DGame.OPL;
using WOLF3D.WOLF3DGame.Setup;

namespace WOLF3D.WOLF3DGame
{
	public class Main : Node
	{
		public Main() => I = this;
		public static Main I { get; private set; }
		public static string Path { get; set; }
		public static string Folder { get; set; }
		public static ARVRInterface ARVRInterface { get; set; }
		public static readonly WorldEnvironment WorldEnvironment = new WorldEnvironment()
		{
			Name = "WorldEnvironment",
			Environment = new Godot.Environment()
			{
				BackgroundColor = Color.Color8(0, 0, 0, 255),
				BackgroundMode = Godot.Environment.BGMode.Color,
			},
		};
		public static Color BackgroundColor
		{
			get => WorldEnvironment.Environment.BackgroundColor;
			set => WorldEnvironment.Environment.BackgroundColor = value;
		}

		public static ActionRoom ActionRoom { get; set; }
		public static MenuRoom MenuRoom { get; set; }
		public static Node Scene
		{
			get => I.scene;
			set
			{
				if (I.scene != null)
					I.RemoveChild(I.scene);
				I.AddChild(I.scene = value);
			}
		}
		private Node scene = null;

		public override void _Ready()
		{
			Path = OS.GetName().Equals("Android") ? "/storage/emulated/0/" : System.IO.Directory.GetCurrentDirectory();
			ARVRInterface = ARVRServer.FindInterface(OS.GetName().Equals("Android") ? "OVRMobile" : "OpenVR");
			VisualServer.SetDefaultClearColor(Color.Color8(0, 0, 0, 255));
			AddChild(WorldEnvironment);

			if (ARVRInterface != null && ARVRInterface.Initialize())
			{
				GetViewport().Arvr = true;
				OS.VsyncEnabled = false;
				Engine.TargetFps = 90;
			}
			else
				GD.Print("ARVRInterface failed to initialize!");
			AddChild(SoundBlaster.OplPlayer);
			Scene = new SetupRoom();
		}

		public static void Load()
		{
			Assets.Load(Folder);
			SoundBlaster.Start();
			ActionRoom = new ActionRoom();
			//Scene = new LoadingRoom(0);
			Scene = MenuRoom = new MenuRoom();
		}
	}
}
