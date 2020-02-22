using Godot;
using NScumm.Audio.OPL.Woody;
using OPL;
using System;
using System.Linq;
using WOLF3DGame;
using WOLF3DGame.Model;

public class Main : Spatial
{
	public string Path { get; set; } = "";
	public ARVRInterface ARVRInterface { get; set; }
	public ARVROrigin ARVROrigin { get; set; }
	public ARVRCamera ARVRCamera { get; set; }
	public ARVRController LeftController { get; set; }
	public ARVRController RightController { get; set; }

	public enum LoadingState
	{
		ASK_PERMISSION,
		GET_SHAREWARE
	};

	private LoadingState state;
	public LoadingState State
	{
		get => state;
		set
		{
			state = value;
			switch (State)
			{
				case LoadingState.ASK_PERMISSION:
					DosScreen.Screen.WriteLine("This application requires permission to both read and write to your device's")
						.WriteLine("external storage.")
						.WriteLine("Press any button to open permission request.");
					break;
				case LoadingState.GET_SHAREWARE:
					DosScreen.Screen.WriteLine("Installing Wolfenstein 3-D Shareware!");
					try
					{
						Game.Folder = System.IO.Path.Combine(Path, "WOLF3D", "WL1");
						DownloadShareware.Main(new string[] { Game.Folder });
					}
					catch (Exception ex)
					{
						DosScreen.Screen.WriteLine(ex.GetType().Name + ": " + ex.Message);
					}
					Assets.LoadAssets(Game.Folder);
					AddChild(Assets.OplPlayer = new OplPlayer()
					{
						Opl = new WoodyEmulatorOpl(NScumm.Core.Audio.OPL.OplType.Opl3)
					});
					PackedScene game = new PackedScene();
					game.Pack(new Game());
					//game.Pack(new MenuRoom());
					GetTree().ChangeSceneTo(game);
					break;
			}
		}
	}

	private DosScreen DosScreen;

	public override void _Ready()
	{
		VisualServer.SetDefaultClearColor(Color.Color8(0, 0, 0, 255));
		AddChild(new WorldEnvironment()
		{
			Environment = new Godot.Environment()
			{
				BackgroundColor = Color.Color8(0, 0, 0, 255),
				BackgroundMode = Godot.Environment.BGMode.Color,
			},
		});
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
		ARVRCamera.AddChild(DosScreen = new DosScreen()
		{
			Transform = new Transform(Basis.Identity, Vector3.Forward * 3f),
		});

		DosScreen.Screen.WriteLine("Platform detected: " + OS.GetName());

		switch (OS.GetName())
		{
			case "Android":
				Path = "/storage/emulated/0/";
				ARVRInterface = ARVRServer.FindInterface("OVRMobile");
				State = PermissionsGranted ? LoadingState.GET_SHAREWARE : LoadingState.ASK_PERMISSION;
				break;
			default:
				Path = System.IO.Directory.GetCurrentDirectory();
				ARVRInterface = ARVRServer.FindInterface("OpenVR");
				State = LoadingState.GET_SHAREWARE;
				break;
		}

		if (ARVRInterface != null && ARVRInterface.Initialize())
			GetViewport().Arvr = true;

		LeftController.Connect("button_pressed", this, nameof(ButtonPressed));
		RightController.Connect("button_pressed", this, nameof(ButtonPressed));
	}

	public static bool IsVRButton(int buttonIndex)
	{
		switch (buttonIndex)
		{
			case (int)JoystickList.VrGrip:
			case (int)JoystickList.VrPad:
			case (int)JoystickList.VrAnalogGrip:
			case (int)JoystickList.VrTrigger:
			case (int)JoystickList.OculusAx:
			case (int)JoystickList.OculusBy:
			case (int)JoystickList.OculusMenu:
				return true;
			default:
				return false;
		}
	}

	public bool PermissionsGranted
	{
		get
		{
			string[] permissions = OS.GetGrantedPermissions();
			return permissions.Contains("android.permission.READ_EXTERNAL_STORAGE") && permissions.Contains("android.permission.WRITE_EXTERNAL_STORAGE");
		}
	}

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
}
