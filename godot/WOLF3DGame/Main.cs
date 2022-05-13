using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Techsola;
using WOLF3D.WOLF3DGame.Action;
using WOLF3D.WOLF3DGame.Menu;
using WOLF3D.WOLF3DGame.MiniMap;
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
			//AmbientTasks.BeginContext(ex => GlobalExceptionHandler(ex));
		}
		public static Main I { get; private set; } = null;
		public static RNG RNG = new RNG();
		public static void GlobalExceptionHandler(Exception ex)
		{
			try
			{
				string message = DateTime.Now + ", " + ex.GetType().Name + ": \"" + ex.Message + "\"" + System.Environment.NewLine + ex.StackTrace;
				Console.Error.WriteLine(message);
				if (SetupRoom is SetupRoom)
				{
					SetupRoom.WriteLine(message);
					SetupRoom.State = SetupRoom.LoadingState.EXCEPTION;
					Room = SetupRoom;
				}
				System.IO.File.AppendAllText(System.IO.Path.Combine(Path, "exception.log"), message);
			}
			catch (Exception) { } // Writing to exception.log is an absolute last resort to capture what went wrong. Can't have it stopping the program if it fails to write.
			if (!Android) throw ex; // Android just quits without showing exceptions. Don't want that.
		}
		/// <returns>a random number between 0-255 inclusive</returns>
		public static int US_RndT() => RNG.Next(0, 256);
		public enum PlatformEnum
		{
			ANDROID, PC
		}
		public static PlatformEnum Platform;
		public static bool Android => Platform == PlatformEnum.ANDROID;
		public static bool PC => Platform == PlatformEnum.PC;

		public static bool VR = false;
		public static bool Pancake
		{
			get => !VR;
			set => VR = !value;
		}
		/// <summary>
		/// The WOLF3D root folder
		/// </summary>
		public static string Path { get; set; }

		/// <summary>
		/// The folder of the currently loaded game
		/// </summary>
		public static string Folder { get; set; }
		public static StatusBar StatusBar
		{
			get => I.statusBar;
			set
			{
				if (I.statusBar != null)
					I.RemoveChild(I.statusBar);
				I.statusBar = value;
				if (I.statusBar != null)
					I.AddChild(I.statusBar);
			}
		}
		private StatusBar statusBar;
		public static bool InGame => ActionRoom != null;
		public static bool InGameMatch(XElement xElement) =>
			xElement?.Attribute("InGame") == null ||
			(xElement.IsTrue("InGame") == InGame);
		public static IEnumerable<StatusNumber.Stat> NextLevelStats { get; set; } = null;
		public static ARVRInterface ARVRInterface { get; set; }
		public static readonly WorldEnvironment WorldEnvironment = new WorldEnvironment()
		{
			Name = "WorldEnvironment",
			Environment = new Godot.Environment()
			{
				BackgroundColor = Color.Color8(0, 0, 0, 255),
				BackgroundMode = Godot.Environment.BGMode.Color,
				AdjustmentEnabled = true,
				AdjustmentBrightness = 1f,
			},
		};
		public static float Brightness
		{
			get => WorldEnvironment.Environment.AdjustmentBrightness;
			set => WorldEnvironment.Environment.AdjustmentBrightness = value;
		}
		public static Color Color
		{
			get => WorldEnvironment.Environment.BackgroundColor;
			set => WorldEnvironment.Environment.BackgroundColor = value;
		}
		public static ActionRoom ActionRoom { get; set; }
		public static MenuRoom MenuRoom { get; set; }
		public static SetupRoom SetupRoom { get; set; }
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

		public Node OpenXRConfig
		{
			get => openXRConfig;
			set
			{
				if (openXRConfig != null)
					RemoveChild(openXRConfig);
				openXRConfig = value;
				if (openXRConfig != null)
					AddChild(openXRConfig);
			}
		}
		private Node openXRConfig = null;
		public override void _Ready()
		{
			Input.SetMouseMode(Input.MouseMode.Hidden);
			Platform = OS.GetName().Equals("Android", StringComparison.InvariantCultureIgnoreCase) ?
				PlatformEnum.ANDROID
				: PlatformEnum.PC;
			Path = System.IO.Path.Combine(Android ? "/storage/emulated/0/" : System.IO.Directory.GetCurrentDirectory(), "WOLF3D");
			VisualServer.SetDefaultClearColor(Color.Color8(0, 0, 0, 255));
			AddChild(WorldEnvironment);
			if (OS.GetCmdlineArgs().Any(e => e.StartsWith("minimap", StringComparison.InvariantCultureIgnoreCase)))
			{
				GD.Print("MINI MAP TEST APP!");
				Assets.Load(
					Folder = System.IO.Path.Combine(Path, "WL1"),
					Assets.LoadXML(Folder)
					);
				Settings.Load();
				MapTestRoom mapTestRoom = new MapTestRoom(0);
				AddChild(mapTestRoom);
				mapTestRoom.Camera.MakeCurrent();
				AddChild(SoundBlaster.OplPlayer);
				AddChild(SoundBlaster.MidiPlayer);
				AddChild(SoundBlaster.AudioStreamPlayer);
				return;
			}
			else if (!OS.GetCmdlineArgs().Any(e => e.EndsWith("pancake", StringComparison.InvariantCultureIgnoreCase)))
			{
				if (Platform == PlatformEnum.PC)
					try
					{
						if (GD.Load<Script>("res://addons/godot-openxr/OpenXRConfig.gdns") is Script script)
						{
							Node openXRConfig = new Node();
							openXRConfig.SetScript(script);
							OpenXRConfig = openXRConfig;
							GD.Print("Initialized OpenXRConfig.");
						}
					}
					catch (Exception ex)
					{
						GD.Print("Failed to initialize OpenXRConfig.", ex.ToString());
					}
				ARVRInterface = ARVRServer.FindInterface("OpenXR");
				if (VR = ARVRInterface?.Initialize() ?? false)
				{
					GetViewport().Arvr = true;
					OS.VsyncEnabled = false;
					Engine.TargetFps = 90;
				}
				else
					GD.Print("ARVRInterface failed to initialize!");
			}
			else
				GD.Print("Skipping ARVRInterface initialization because of PANCAKE command parameter.");
			AddChild(SoundBlaster.OplPlayer);
			AddChild(SoundBlaster.MidiPlayer);
			AddChild(SoundBlaster.AudioStreamPlayer);
			Room = SetupRoom = new SetupRoom();
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
			Room.ChangeRoom(MenuRoom);
		}
	}
}
