using Godot;
using System;
using System.Xml.Linq;
using WOLF3D.WOLF3DGame.Action;
using WOLF3D.WOLF3DGame.Menu;
using WOLF3D.WOLF3DGame.OPL;
using WOLF3D.WOLF3DGame.Setup;

namespace WOLF3D.WOLF3DGame
{
	public class Main : Node
	{
		public Main()
		{
			if (I != null)
				throw new InvalidOperationException("Only one instance of Main is allowed!");
			I = this;
		}
		public static Main I { get; private set; } = null;
		public static RNG RNG = new RNG();
		public static string Path { get; set; }
		public static string Folder { get; set; }
		public static bool InGame => ActionRoom != null;
		public static bool InGameMatch(XElement xElement) =>
			xElement?.Attribute("InGame") == null ||
			(Assets.IsTrue(xElement, "InGame") == InGame);

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
		public static Color Color
		{
			get => WorldEnvironment.Environment.BackgroundColor;
			set => WorldEnvironment.Environment.BackgroundColor = value;
		}

		public static ActionRoom ActionRoom { get; set; }
		public static MenuRoom MenuRoom { get; set; }
		public static Room Room
		{
			get => I.room;
			set
			{
				if (I.room != null)
				{
					I.room.Exit();
					I.RemoveChild(I.room);
				}
				I.AddChild(I.room = value);
				I.room.Enter();
			}
		}
		private Room room = null;

		public override void _Ready()
		{
			bool android = OS.GetName().Equals("Android", StringComparison.InvariantCultureIgnoreCase);
			Path = android ? "/storage/emulated/0/" : System.IO.Directory.GetCurrentDirectory();
			ARVRInterface = ARVRServer.FindInterface(android ? "OVRMobile" : "OpenVR");
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
			Room = new SetupRoom();
		}

		public static void Load()
		{
			Assets.Load();
			Settings.Load();
			SoundBlaster.Start();
			Room = MenuRoom = new MenuRoom();
		}

		/// <summary>
		/// Immediately quits, no questions asked
		/// </summary>
		public static void Quit()
		{
			I.GetTree().Quit();
			System.Environment.Exit(0);
		}

		public static void End()
		{
			Settings.Load();
			ActionRoom = null;
			MenuRoom.MenuScreen = Assets.Menu("Main");
			Room = MenuRoom;
		}
	}
}
