using Godot;
using System;
using System.Linq;

namespace WOLF3D.WOLF3DGame.Setup
{
    public class SetupRoom : Room
    {
        public enum LoadingState
        {
            READY,
            ASK_PERMISSION,
            GET_SHAREWARE
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
                        DosScreen.Screen.WriteLine("This application requires permission to both read and write to your device's")
                            .WriteLine("external storage.")
                            .WriteLine("Press any button to continue.");
                        break;
                    case LoadingState.GET_SHAREWARE:
                        DosScreen.Screen.WriteLine("Installing Wolfenstein 3-D Shareware!");
                        try
                        {
                            DownloadShareware.Main(new string[] { Main.Folder = System.IO.Path.Combine(Main.Path, "WOLF3D", "WL1") });
                        }
                        catch (Exception ex)
                        {
                            DosScreen.Screen.WriteLine(ex.GetType().Name + ": " + ex.Message);
                        }
                        Main.Load();
                        break;
                }
            }
        }

        private DosScreen DosScreen;

        public SetupRoom()
        {
            Name = "SetupRoom";
            Paused = false;
            AddChild(ARVROrigin = new ARVROrigin());
            ARVROrigin.AddChild(ARVRCamera = new FadeCamera()
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
        }

        public override void _Ready()
        {
            if (Main.ARVRInterface != null && Main.ARVRInterface.Initialize())
                GetViewport().Arvr = true;
        }

        public override void Enter()
        {
            base.Enter();
            Main.Color = Color.Color8(0, 0, 0, 255);
            LeftController.Connect("button_pressed", this, nameof(ButtonPressed));
            RightController.Connect("button_pressed", this, nameof(ButtonPressed));
            Paused = false;
            Main.Brightness = 1f;
        }

        public override void Exit()
        {
            base.Exit();
            if (LeftController.IsConnected("button_pressed", this, nameof(ButtonPressed)))
                LeftController.Disconnect("button_pressed", this, nameof(ButtonPressed));
            if (RightController.IsConnected("button_pressed", this, nameof(ButtonPressed)))
                RightController.Disconnect("button_pressed", this, nameof(ButtonPressed));
        }

        public override void _PhysicsProcess(float delta)
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

        public static bool PermissionsGranted =>
            OS.GetGrantedPermissions() is string[] permissions &&
            permissions != null &&
            permissions.Contains("android.permission.READ_EXTERNAL_STORAGE", StringComparer.InvariantCultureIgnoreCase) &&
            permissions.Contains("android.permission.WRITE_EXTERNAL_STORAGE", StringComparer.InvariantCultureIgnoreCase);

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
}
