using Godot;
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
		DOWNLOAD_SHAREWARE
	};

	private LoadingState state;
	public LoadingState State
	{
		get
		{
			return state;
		}
		set
		{
			state = value;
			switch (State)
			{
				case LoadingState.ASK_PERMISSION:
					DosScreen.Screen.WriteLine("This application requires permission to both read and write to your device's external storage.");
					DosScreen.Screen.WriteLine("Press any button to open permission request.");
					break;
				case LoadingState.DOWNLOAD_SHAREWARE:
					DosScreen.Screen.WriteLine("Downloading shareware!");
					string folder = System.IO.Path.Combine(Path, "WOLF3D", "SHARE");
					DownloadShareware.Main(new string[] { folder });
					PackedScene game = new PackedScene();
					game.Pack(new Game()
					{
						Folder = folder,
					});
					GetTree().ChangeSceneTo(game);
					break;
			}
		}
	}

	private DosScreen DosScreen;

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
		LeftController.AddChild(GD.Load<PackedScene>("res://OQ_Toolkit/OQ_ARVRController/models3d/OculusQuestTouchController_Left.gltf").Instance());
		ARVROrigin.AddChild(RightController = new ARVRController()
		{
			ControllerId = 2,
		});
		RightController.AddChild(GD.Load<PackedScene>("res://OQ_Toolkit/OQ_ARVRController/models3d/OculusQuestTouchController_Right.gltf").Instance());

		AddChild(new WorldEnvironment()
		{
			Environment = new Godot.Environment()
			{
				BackgroundColor = Color.Color8(0, 0, 0, 255),
				BackgroundMode = Godot.Environment.BGMode.Color,
			},
		});

		AddChild(DosScreen = new DosScreen()
		{
			GlobalTransform = new Transform(Basis.Identity, new Vector3(0, 0, -2)),
		});

		DosScreen.Screen.WriteLine("Platform detected: " + OS.GetName());

		switch (OS.GetName())
		{
			case "Android":
				Path = "/storage/emulated/0/";
				ARVRInterface = ARVRServer.FindInterface("OVRMobile");
				State = PermissionsGranted ? LoadingState.DOWNLOAD_SHAREWARE : LoadingState.ASK_PERMISSION;
				break;
			default:
				ARVRInterface = ARVRServer.FindInterface("OpenVR");
				State = LoadingState.DOWNLOAD_SHAREWARE;
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
						State = LoadingState.DOWNLOAD_SHAREWARE;
					else
						OS.RequestPermissions();
					break;
			}
	}

	//public override void _Process(float delta)
	//{
	//}

	/*
	public override void _Input(InputEvent @event)
	{
		base._Input(@event);

		DosScreen.Screen.WriteLine(
			"InputEvent: \"" + @event + "\""
			);
	}
	*/
}
