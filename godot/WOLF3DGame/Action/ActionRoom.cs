using Godot;
using WOLF3DModel;
using WOLF3D.WOLF3DGame.OPL;
using System.Xml.Linq;
using System;

namespace WOLF3D.WOLF3DGame.Action
{
	public class ActionRoom : Room, ISavable
	{
		public override ARVROrigin ARVROrigin
		{
			get => ARVRPlayer.ARVROrigin;
			set => ARVRPlayer.ARVROrigin = value;
		}
		public override FadeCamera.FadeCamera ARVRCamera
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
		public ushort NextMap => (ushort)(Level.GameMap.Number + 1 >= Assets.Maps.Length ? 0 : Level.GameMap.Number + 1);
		public byte Difficulty { get; set; }
		public byte Episode { get; set; }
		private ActionRoom()
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
		}
		public ActionRoom(ushort mapNumber) : this()
		{
			Name = "ActionRoom " + Assets.Maps[mapNumber].Name;
			AddChild(Level = new Level(mapNumber));
			ARVRPlayer.Transform = Assets.StartTransform(Assets.Maps[mapNumber]);
		}
		public ActionRoom(XElement xml) : this()
		{
			AddChild(Level = new Level(xml.Element("Level")));
			Name = "ActionRoom " + Level.GameMap.Name;
			ARVRPlayer.Set(xml.Element("ARVRPlayer"));
			if (xml.Attribute("RNG")?.Value is string stateCode)
				Main.RNG.StateCode = stateCode;
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
			if (Fading)
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
			if (!Main.Room.Fading)
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
							Fading = true;
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
				ChangeRoom(new LoadingRoom(Assets.MapAnalyzer.MapNumber(Level.MapAnalysis.Episode, Level.MapAnalysis.ElevatorTo)));
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
			Main.Color = Assets.Palettes[0][Level.MapAnalysis.Border];
			if (!Settings.MusicMuted
				&& Level.MapAnalysis.Song is string songName
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
		public ActionRoom Screenshot(string path = null)
		{
			Godot.Image image = ARVRPlayer.ARVRCamera.GetViewport().GetTexture().GetData();
			image.FlipY();
			image.SavePng(path ?? System.IO.Path.Combine(Main.Folder, "screenshot.png"));
			return this;
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
			TimeSpan time = TimeSpan.FromSeconds(Level.Time);
			e.SetAttributeValue(XName.Get("Name"), (string.IsNullOrWhiteSpace(Level.GameMap.Name) ? "E" + Episode + "M" + Level.GameMap.Number : Level.GameMap.Name) + " at " + Math.Floor(time.TotalMinutes) + "m" + time.Seconds + "s on " + DateTime.Now);
			e.SetAttributeValue(XName.Get("Difficulty"), Difficulty);
			e.SetAttributeValue(XName.Get("Episode"), Episode);
			e.SetAttributeValue(XName.Get("RNG"), Main.RNG.StateCode);
			e.Add(ARVRPlayer.Save());
			e.Add(Main.StatusBar.Save());
			e.Add(Level.Save());
			return e;
		}
	}
}
