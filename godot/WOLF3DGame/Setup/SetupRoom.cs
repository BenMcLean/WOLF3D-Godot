using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;
using Techsola;
using WOLF3D.WOLF3DGame.Menu;

namespace WOLF3D.WOLF3DGame.Setup
{
	public class SetupRoom : Room
	{
		#region Data members
		public static string Load = null;
		private DosScreen DosScreen;
		public enum LoadingState
		{
			READY,
			ASK_PERMISSION,
			GET_SHAREWARE,
			LOAD_ASSETS,
			EXCEPTION
		};
		private LoadingState state = LoadingState.READY;
		public LoadingState State
		{
			get => state;
			set
			{
				state = value;
				switch (State)
				{
					case LoadingState.ASK_PERMISSION:
						WriteLine("This application requires permission to both read and write to your device's")
							.WriteLine("external storage.")
							.WriteLine("Press any button to continue.");
						break;
					case LoadingState.GET_SHAREWARE:
						if (OS.GetCmdlineArgs()
							?.Where(e => !e.StartsWith("-") && System.IO.File.Exists(e))
							?.FirstOrDefault() is string load)
							Load = load;
						WriteLine("Installing Wolfenstein 3-D Shareware!");
						try
						{
							Shareware();
						}
						catch (Exception ex)
						{
							WriteLine(ex.GetType().Name + ": " + ex.Message + "\n" + ex.StackTrace);
							break;
						}
						State = LoadingState.LOAD_ASSETS;
						break;
					case LoadingState.LOAD_ASSETS:
						LoadAssets();
						break;
				}
			}
		}
		#endregion Data members
		#region Godot
		public SetupRoom()
		{
			Name = "SetupRoom";
			Fading = false;
			AddChild(ARVROrigin = new ARVROrigin());
			ARVROrigin.AddChild(ARVRCamera = new FadeCamera.FadeCamera()
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
			ARVRCamera.AddChild(DosScreen = new DosScreen()
			{
				Transform = new Transform(Basis.Identity, Vector3.Forward * 3f),
			});

			WriteLine("Platform detected: " + OS.GetName())
				.WriteLine("VR mode: " + Main.VR);
		}
		public override void _Ready()
		{
			if (Main.ARVRInterface != null && Main.ARVRInterface.Initialize())
				GetViewport().Arvr = true;
		}
		public override void _Process(float delta)
		{
			if (State == LoadingState.READY)
				switch (OS.GetName())
				{
					case "Android":
						State = PermissionsGranted ? LoadingState.GET_SHAREWARE : LoadingState.ASK_PERMISSION;
						break;
					default:
						State = LoadingState.GET_SHAREWARE;
						break;
				}
		}
		#endregion Godot
		#region Android
		public static bool PermissionsGranted =>
			OS.GetGrantedPermissions() is string[] permissions &&
			permissions != null &&
			permissions.Contains("android.permission.READ_EXTERNAL_STORAGE", StringComparer.InvariantCultureIgnoreCase) &&
			permissions.Contains("android.permission.WRITE_EXTERNAL_STORAGE", StringComparer.InvariantCultureIgnoreCase);
		#endregion Android
		#region VR
		public void ButtonPressed(int buttonIndex)
		{
			if (IsVRButton(buttonIndex))
				switch (State)
				{
					case LoadingState.ASK_PERMISSION:
						if (PermissionsGranted)
							State = LoadingState.GET_SHAREWARE;
						else
							OS.RequestPermissions();
						break;
				}
		}
		public SetupRoom WriteLine(string message)
		{
			GD.Print(message);
			DosScreen.WriteLine(message);
			return this;
		}
		#endregion VR
		#region Room
		public override void Enter()
		{
			base.Enter();
			Main.Color = Color.Color8(0, 0, 0, 255);
			LeftController.Connect("button_pressed", this, nameof(ButtonPressed));
			RightController.Connect("button_pressed", this, nameof(ButtonPressed));
			Fading = false;
			Main.Brightness = 1f;
			if (State == LoadingState.LOAD_ASSETS)
				LoadAssets();
		}
		public override void Exit()
		{
			base.Exit();
			if (LeftController.IsConnected("button_pressed", this, nameof(ButtonPressed)))
				LeftController.Disconnect("button_pressed", this, nameof(ButtonPressed));
			if (RightController.IsConnected("button_pressed", this, nameof(ButtonPressed)))
				RightController.Disconnect("button_pressed", this, nameof(ButtonPressed));
		}
		#endregion Room
		#region Shareware
		public void Shareware()
		{
			Godot.Directory res = new Godot.Directory();
			res.Open("res://");
			System.IO.Directory.CreateDirectory(Main.Path);
			foreach (string file in ListFiles(null, "*.xml"))
			{
				string xml = System.IO.Path.Combine(
					System.IO.Directory.CreateDirectory(
						System.IO.Path.Combine(
							Main.Path,
							System.IO.Path.GetFileNameWithoutExtension(file)
							)
						).FullName,
					"game.xml"
					);
				res.Copy(System.IO.Path.Combine("res://", file), xml);
				Godot.GD.Print("Copied \"" + file + "\" to \"" + xml + "\".");
			}
			if (!System.IO.File.Exists(System.IO.Path.Combine(Main.Path, "WL1", "WOLF3D.EXE")))
			{
				// I'm including a complete and unmodified copy of Wolfenstein 3-D Shareware v1.4 retrieved from https://archive.org/download/Wolfenstein3d/Wolfenstein3dV14sw.ZIP in this game's resources which is used not only to play the shareware levels but also to render the game selection menu.
				// I would very much prefer to use the official URL ftp://ftp.3drealms.com/share/1wolf14.zip
				// However, that packs the shareware episode inside it's original installer, and extracting files from that is a pain.
				// To avoid that, I'll probably just use a zip of a fully installed shareware version instead.
				// In case I ever want to revisit the issue of extracting from the shareware installer, I found some C code to extract the shareware files here: https://github.com/rpmfusion/wolf3d-shareware
				// That code seems to depend on this library here: https://github.com/twogood/dynamite
				string zip = System.IO.Path.Combine(Main.Path, "WL1", "Wolfenstein3dV14sw.ZIP");
				res.Copy("res://Wolfenstein3dV14sw.ZIP", zip);
				System.IO.Compression.ZipFile.ExtractToDirectory(zip, System.IO.Path.GetDirectoryName(zip));
				System.IO.File.Delete(zip);
			}
		}
		public static System.Collections.Generic.IEnumerable<string> ListFiles(string path = null, string filter = "*.*")
		{
			filter = WildCardToRegular(filter);
			Godot.Directory dir = new Godot.Directory();
			dir.Open(path ?? "res://");
			dir.ListDirBegin();
			while (dir.GetNext() is string file && !string.IsNullOrWhiteSpace(file))
				if (file[0] != '.' && System.Text.RegularExpressions.Regex.IsMatch(file, filter))
					yield return file;
			dir.ListDirEnd();
		}
		public static string WildCardToRegular(string value) => "^" + System.Text.RegularExpressions.Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$";
		#endregion Shareware
		#region LoadAssets
		public void LoadAssets()
		{
			WriteLine(Load is string ? "Loading \"" + Load + "\"..." : "Loading game selection menu...");
			AmbientTasks.Add(Task.Run(LoadAssets2));
		}
		public static void LoadAssets2()
		{
			if (Load is string)
			{
				Main.Folder = System.IO.Path.GetDirectoryName(Load);
				Assets.Load(Main.Folder, System.IO.Path.GetFileName(Load));
				Settings.Load();
				Main.StatusBar = new StatusBar();
				Main.MenuRoom = new MenuRoom();
			}
			else
			{
				Assets.Load(
					Main.Folder = System.IO.Path.Combine(Main.Path, "WL1"),
					Assets.LoadXML(Main.Folder).InsertGameSelectionMenu(),
					true
					);
				Settings.Load();
				Main.MenuRoom = new MenuRoom("_GameSelect0");
			}
			Main.Room = Main.MenuRoom;
		}
		#endregion LoadAssets
	}
}
