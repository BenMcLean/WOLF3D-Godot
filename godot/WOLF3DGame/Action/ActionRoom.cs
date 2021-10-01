using Godot;
using WOLF3DModel;
using WOLF3D.WOLF3DGame.OPL;
using System.Xml.Linq;

namespace WOLF3D.WOLF3DGame.Action
{
	public class ActionRoom : Room, ISavable
	{
		public override ARVROrigin ARVROrigin
		{
			get => ARVRPlayer.ARVROrigin;
			set => ARVRPlayer.ARVROrigin = value;
		}
		public override FadeCamera ARVRCamera
		{
			get => ARVRPlayer.ARVRCamera;
			set => ARVRPlayer.ARVRCamera = value;
		}
		public override ARVRController LeftController
		{
			get => ARVRPlayer.LeftController;
			set => ARVRPlayer.LeftController = value;
		}
		public override ARVRController RightController
		{
			get => ARVRPlayer.RightController;
			set => ARVRPlayer.RightController = value;
		}
		public ARVRPlayer ARVRPlayer { get; set; }
		public Level Level { get; set; } = null;
		public static Line3D Line3D { get; set; }
		public ushort NextMap => (ushort)(Map.Number + 1 >= Assets.Maps.Length ? 0 : Map.Number + 1);
		public byte Difficulty { get; set; }
		public byte Episode { get; set; }
		public GameMap Map => Level.Map;

		public ActionRoom(GameMap map)
		{
			Name = "ActionRoom";
			AddChild(ARVRPlayer = new ARVRPlayer());
			if (Main.VR)
			{
				Spatial controller = (Spatial)GD.Load<PackedScene>("res://OQ_Toolkit/OQ_ARVRController/models3d/OculusQuestTouchController_Left.gltf").Instance();
				controller.Rotate(controller.Transform.basis.x.Normalized(), -Mathf.Pi / 4f);
				ARVRPlayer.LeftController.AddChild(controller);
				controller = (Spatial)GD.Load<PackedScene>("res://OQ_Toolkit/OQ_ARVRController/models3d/OculusQuestTouchController_Right.gltf").Instance();
				controller.Rotate(controller.Transform.basis.x.Normalized(), -Mathf.Pi / 4f);
				ARVRPlayer.RightController.AddChild(controller);
			}
			ARVRCamera.AddChild(new MeshInstance()
			{
				Name = "StatusBarTest",
				Mesh = new QuadMesh()
				{
					Size = new Vector2(Assets.Foot, Assets.Foot / Main.StatusBar.Size.x * Main.StatusBar.Size.y * 1.2f),
				},
				MaterialOverride = new SpatialMaterial()
				{
					AlbedoTexture = Main.StatusBar.GetTexture(),
					FlagsUnshaded = true,
					FlagsDoNotReceiveShadows = true,
					FlagsDisableAmbientLight = true,
					FlagsTransparent = false,
					ParamsCullMode = SpatialMaterial.CullMode.Back,
					ParamsSpecularMode = SpatialMaterial.SpecularMode.Disabled,
				},
				Transform = new Transform(Basis.Identity, Vector3.Forward / 6 + Vector3.Down / 12),
			});

			AddChild(Level = new Level(map));
			ARVRPlayer.GlobalTransform = Assets.StartTransform(map);
		}

		public override void _Ready()
		{
			VisualServer.SetDefaultClearColor(Color.Color8(0, 0, 0, 255));
			ARVRPlayer.RightController.Connect("button_pressed", this, nameof(ButtonPressed));

			AddChild(Line3D = new Line3D()
			{
				Name = "Line3D",
				Color = Color.Color8(255, 0, 0, 255),
			});

			AddChild(LeftTarget = new MeshInstance()
			{
				Name = "LeftTarget",
				Mesh = TargetMesh,
			});

			AddChild(RightTarget = new MeshInstance()
			{
				Name = "RightTarget",
				Mesh = TargetMesh,
			});
		}

		public static Vector3 BillboardRotation { get; set; }

		public override void _PhysicsProcess(float delta)
		{
			if (Paused)
				PausedProcess(delta);
			if (GetViewport() is Viewport viewport
				&& viewport.GetCamera() is Camera camera
				&& camera.GlobalTransform is Transform globalTransform)
				BillboardRotation = new Vector3(0f, globalTransform.basis.GetEuler().y, 0f);
		}

		public override void _Input(InputEvent @event)
		{
			if (@event.IsActionPressed("toggle_fullscreen"))
				OS.WindowFullscreen = !OS.WindowFullscreen;
			if (!Main.Room.Paused)
			{
				if (@event.IsActionPressed("ui_cancel"))
				{
					Main.MenuRoom.MenuScreen = Assets.Menu("Main");
					ChangeRoom(Main.MenuRoom);
				}
				if (@event is InputEventKey inputEventKey && inputEventKey.Pressed && !inputEventKey.Echo)
					switch (inputEventKey.Scancode)
					{
						case (uint)KeyList.X:
							Print();
							break;
						case (uint)KeyList.Z:
							Paused = true;
							Main.MenuRoom.MenuScreen = Assets.Menu("FloorComplete");
							ChangeRoom(Main.MenuRoom);
							break;
					}
			}
		}

		public void ButtonPressed(int buttonIndex)
		{
			if (buttonIndex == (int)JoystickList.OculusAx)
				Print();
			if (buttonIndex == (int)JoystickList.OculusBy)
				ChangeRoom(new LoadingRoom((GameMap)Assets.NextMap(Level.Map)));
		}

		public void Print()
		{
			GD.Print("Left joystick: {" + ARVRPlayer.LeftController.GetJoystickAxis(0) + ", " + ARVRPlayer.LeftController.GetJoystickAxis(1) + "} Right joystick: " + ARVRPlayer.RightController.GetJoystickAxis(0) + ", " + ARVRPlayer.RightController.GetJoystickAxis(1) + "}");
			//StringBuilder stringBuilder = new StringBuilder().Append("Squares occupied: {");
			//foreach (ushort square in Level.SquaresOccupied(ARVRPlayer.PlayerPosition))
			//    stringBuilder.Append("[")
			//        .Append(Level.Map.X(square))
			//        .Append(", ")
			//        .Append(Level.Map.Z(square))
			//        .Append("] ");
			//GD.Print(stringBuilder.Append("}").ToString());
		}

		public ActionRoom PlayASound()
		{
			AudioStreamPlayer audioStreamPlayer = new AudioStreamPlayer()
			{
				Stream = Assets.DigiSounds[32],
				VolumeDb = 0.01f
			};
			AddChild(audioStreamPlayer);
			audioStreamPlayer.Play();
			return this;
		}

		public override void Enter()
		{
			base.Enter();
			Main.Color = Assets.Palettes[0][Map.Border];
			if (!Settings.MusicMuted
				&& Map.Song is string songName
				&& Assets.AudioT.Songs.TryGetValue(songName, out AudioT.Song song)
				&& SoundBlaster.Song != song)
				SoundBlaster.Song = song;
			if (Main.Pancake)
				Input.SetMouseMode(Input.MouseMode.Captured);
			ARVRPlayer.Enter();
		}

		public override void Exit()
		{
			base.Exit();
			Main.NextLevelStats = Main.StatusBar.NextLevelStats();
			Input.SetMouseMode(Input.MouseMode.Hidden);
		}

		public bool Pickup(Pickup pickup)
		{
			if (pickup.IsIn(ARVRPlayer.Position) && XMLScript.Run(pickup.XML, pickup))
			{
				if (pickup.Flash is Godot.Color color)
					ARVRPlayer.FadeCameraController.Flash(color);
				Level.RemoveChild(pickup);
				Level.Pickups.Remove(pickup);
				return true;
			}
			return false;
		}

		public readonly static SphereMesh TargetMesh = new SphereMesh()
		{
			ResourceName = "Target",
			Radius = Assets.PixelHeight / 2f,
			Height = Assets.PixelHeight,
			Material = new SpatialMaterial()
			{
				AlbedoColor = Color.Color8(255, 0, 0, 255),
				FlagsUnshaded = true,
				FlagsDoNotReceiveShadows = true,
				FlagsDisableAmbientLight = true,
				FlagsTransparent = false,
				ParamsCullMode = SpatialMaterial.CullMode.Disabled,
				ParamsSpecularMode = SpatialMaterial.SpecularMode.Disabled,
			},
		};

		public MeshInstance LeftTarget { get; set; }
		public MeshInstance RightTarget { get; set; }
		public MeshInstance Target(bool left) => left ? LeftTarget : RightTarget;
		public MeshInstance Target(int which) => Target(which == 0);
		public XElement Save()
		{
			XElement e = new XElement(XName.Get("SaveGame"));
			e.SetAttributeValue(XName.Get("Difficulty"), Difficulty);
			e.SetAttributeValue(XName.Get("Episode"), Episode);
			e.Add(ARVRPlayer.Save());
			e.Add(Main.StatusBar.Save());
			e.Add(Level.Save());
			return e;
		}
	}
}
