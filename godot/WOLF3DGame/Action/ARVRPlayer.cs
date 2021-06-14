﻿using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WOLF3D.WOLF3DGame.Action
{
	public class ARVRPlayer : Spatial, ITarget
	{
		public ARVROrigin ARVROrigin { get; set; }
		public FadeCamera ARVRCamera { get; set; }
		public FadeCameraPancake PancakeCamera { get; set; }
		public ARVRController LeftController { get; set; } = null;
		public ARVRController RightController { get; set; } = null;
		public ARVRController Controller(bool left) => left ? LeftController : RightController;
		public ARVRController Controller(int which) => Controller(which == 0);
		public IFadeCamera FadeCamera => PancakeCamera is IFadeCamera ? PancakeCamera : ARVRCamera is IFadeCamera f ? f : null;
		public FadeCameraController FadeCameraController;
		public IEnumerable<ARVRController> Controllers()
		{
			if (LeftController != null) yield return LeftController;
			if (RightController != null) yield return RightController;
		}
		public ARVRController OtherController(ARVRController aRVRController) => aRVRController == LeftController ? RightController : LeftController;
		public static readonly Vector3 PancakeCameraOrigin = new Vector3(0f, Assets.HalfWallHeight, 0f);

		public ARVRPlayer()
		{
			Name = "ARVRPlayer";
			if (Main.Pancake)
				AddChild(PancakeCamera = new FadeCameraPancake()
				{
					Name = "PancakeCamera",
					Current = Main.Pancake,
					Transform = new Transform(Basis.Identity, PancakeCameraOrigin),
				});
			AddChild(ARVROrigin = new ARVROrigin()
			{
				Name = "ARVROrigin",
			});
			ARVROrigin.AddChild(LeftController = new ARVRController()
			{
				Name = "LeftController",
				ControllerId = 1,
			});
			ARVROrigin.AddChild(RightController = new ARVRController()
			{
				Name = "RightController",
				ControllerId = 2,
			});
			ARVROrigin.AddChild(ARVRCamera = new FadeCamera()
			{
				Name = "FadeCamera",
				Current = Main.VR,
			});
			((Node)FadeCamera).AddChild(FadeCameraController = new FadeCameraController()
			{
				Name = "FadeCameraController",
				FadeCamera = FadeCamera,
			});
		}

		public static float Strength(float input) =>
			Mathf.Abs(input) < Assets.DeadZone ? 0f
			: (Mathf.Abs(input) - Assets.DeadZone) / (1f - Assets.DeadZone) * Mathf.Sign(input);

		public override void _PhysicsProcess(float delta)
		{
			#region Walking
			Vector2 forward = ARVRCameraDirection, // which way we're facing
				movement = Vector2.Zero; // movement vector from joystick and keyboard input
			bool keyPressed = false; // if true then we go max speed and ignore what the joysticks say.

			if (!Main.Room.Paused)
			{
				if (!(Input.IsKeyPressed((int)KeyList.Up) || Input.IsKeyPressed((int)KeyList.W)) || !(Input.IsKeyPressed((int)KeyList.Down) || Input.IsKeyPressed((int)KeyList.S)))
				{ // Don't want to move this way if both keys are pressed at once.
					if (Input.IsKeyPressed((int)KeyList.Up) || Input.IsKeyPressed((int)KeyList.W))
					{
						movement += forward;
						keyPressed = true;
					}
					if (Input.IsKeyPressed((int)KeyList.Down) || Input.IsKeyPressed((int)KeyList.S))
					{
						movement += forward.Rotated(Mathf.Pi);
						keyPressed = true;
					}
				}
				if (!Input.IsKeyPressed((int)KeyList.A) || !Input.IsKeyPressed((int)KeyList.D))
				{ // Don't want to move this way if both keys are pressed at once.
					if (Input.IsKeyPressed((int)KeyList.A))
					{
						movement += forward.Rotated(Mathf.Pi / -2f);
						keyPressed = true;
					}
					if (Input.IsKeyPressed((int)KeyList.D))
					{
						movement += forward.Rotated(Mathf.Pi / 2f);
						keyPressed = true;
					}
				}

				if (keyPressed)
					movement = movement.Normalized();
				else
				{
					Vector2 joystick = new Vector2(LeftController.GetJoystickAxis(1) + RightController.GetJoystickAxis(1), LeftController.GetJoystickAxis(0));
					float strength = Strength(joystick.Length());
					if (Mathf.Abs(strength) > 1)
						strength = Mathf.Sign(strength);
					if (Mathf.Abs(strength) > float.Epsilon)
						movement += (joystick.Normalized() * strength).Rotated(forward.Angle());
				}

				if (movement.Length() > 1f)
					movement = movement.Normalized();

				Position = Walk(Position, Walk(
						Position,
						Position + ARVRCameraMovement + movement * delta * (Input.IsKeyPressed((int)KeyList.Shift) ? Assets.WalkSpeed : Assets.RunSpeed)
						));
			}

			// Move ARVROrigin so that camera global position matches player global position
			ARVROrigin.Transform = new Transform(
				Basis.Identity,
				new Vector3(
					-ARVRCamera.Transform.origin.x,
					Height,
					-ARVRCamera.Transform.origin.z
				)
			);

			// Joystick and keyboard rotation
			float axis0 = -Strength(RightController.GetJoystickAxis(0));
			if (Input.IsKeyPressed((int)KeyList.Left))
				axis0 += 1;
			if (Input.IsKeyPressed((int)KeyList.Right))
				axis0 -= 1;
			if (Mathf.Abs(axis0) > float.Epsilon)
				Rotate(Vector3.Up, Mathf.Pi * delta * axis0);
			#endregion Walking

			#region Shooting
			if (!Main.Room.Paused)
				for (int control = 0; control < 2; control++)
				{
					Spatial controller = Main.VR ? Controller(control) : (Spatial)PancakeCamera;
					exclude.Clear();
					while (true)
					{ // Shooting while loop
						Godot.Collections.Dictionary ray = GetWorld().DirectSpaceState.IntersectRay(
								controller.GlobalTransform.origin,
								controller.GlobalTransform.origin + ARVRControllerDirection(controller.GlobalTransform.basis) * Assets.ShotRange,
								exclude
							);
						if (ray.Count > 0 && ray["collider"] is CollisionObject collider)
							if (collider is Billboard billboard && ray["position"] is Vector3 position && !billboard.IsHit(position))
								exclude.Add(ray["collider"]);
							else
							{
								Main.ActionRoom.Target(control).GlobalTransform = new Transform(Basis.Identity, (Vector3)ray["position"]);
								if (collider is Actor actor)
									SetTarget(control, actor);
								else
									SetTarget(control);
								break; // Shooting while loop
							}
						else
						{ // Nothing was hit
							Main.ActionRoom.Target(control).GlobalTransform = new Transform(Basis.Identity, controller.GlobalTransform.origin + ARVRControllerDirection(controller.GlobalTransform.basis) * Assets.ShotRange);
							SetTarget(control);
							break; // Shooting while loop
						}
					}
				}
			#endregion Shooting

			#region Pushing
			if (!Main.Room.Paused)
			{
				if (Input.IsKeyPressed((int)KeyList.Space))
				{
					if (!Pushing)
					{
						Push(new Vector2(
							Position.x - Direction8.CardinalFrom(ARVRCameraDirection).X * Assets.WallWidth,
							Position.y - Direction8.CardinalFrom(ARVRCameraDirection).Z * Assets.WallWidth
							));
						Pushing = true;
					}
				}
				else if (RightController.IsButtonPressed((int)Godot.JoystickList.VrGrip) > 0)
				{
					if (!Pushing)
					{
						if (!Push(Assets.Vector2(RightController.GlobalTransform.origin)))
							Push(new Vector2(
								Position.x - Direction8.CardinalFrom(ARVRCameraDirection).X * Assets.WallWidth,
								Position.y - Direction8.CardinalFrom(ARVRCameraDirection).Z * Assets.WallWidth
								));
						Pushing = true;
					}
				}
				else if (LeftController.IsButtonPressed((int)Godot.JoystickList.VrGrip) > 0)
				{
					if (!Pushing)
					{
						if (!Push(Assets.Vector2(LeftController.GlobalTransform.origin)))
							Push(new Vector2(
								Position.x - Direction8.CardinalFrom(ARVRCameraDirection).X * Assets.WallWidth,
								Position.y - Direction8.CardinalFrom(ARVRCameraDirection).Z * Assets.WallWidth
								));
						Pushing = true;
					}
				}
				else
					Pushing = false;
			}
			#endregion Pushing
		}

		private readonly Godot.Collections.Array exclude = new Godot.Collections.Array();

		public override void _Input(InputEvent @event)
		{
			base._Input(@event);
			if (Main.Pancake)
				if (@event is InputEventMouseButton button && button.IsPressed())
				{
					if (button.ButtonIndex == (int)ButtonList.Left)
					{
						if (Target() is Actor actor)
							actor.Kill();
					}
					else if (button.ButtonIndex == (int)ButtonList.Right)
						Push(new Vector2(
							Position.x - Direction8.CardinalFrom(ARVRCameraDirection).X * Assets.WallWidth,
							Position.y - Direction8.CardinalFrom(ARVRCameraDirection).Z * Assets.WallWidth
							));
				}
				else if (@event is InputEventMouseMotion motion)
				{
					float dx = motion.Relative.y * Settings.MouseYSensitivity / 180f,
						x = PancakeCamera.Transform.basis.GetEuler().x - dx;
					if (Mathf.Abs(x) < Assets.HalfPi)
						PancakeCamera.Rotate(Vector3.Left, dx);
					Rotate(Vector3.Down, motion.Relative.x * Settings.MouseYSensitivity / 180f);
				}
		}

		public bool Shooting { get; set; } = false;
		public bool Pushing { get; set; } = false;

		public float Height => Settings.Roomscale ?
			0f
			: Assets.HalfWallHeight - ARVRCamera.Transform.origin.y;

		public Vector2 ARVROriginPosition => Assets.Vector2(ARVROrigin.GlobalTransform.origin);
		public Vector2 ARVRCameraPosition => Assets.Vector2(ARVRCamera.GlobalTransform.origin);
		public Vector2 ARVRCameraDirection => -Assets.Vector2(ARVRCamera.GlobalTransform.basis.z).Normalized();
		public Vector2 ARVRCameraMovement => ARVRCameraPosition - Assets.Vector2(GlobalTransform.origin);

		public static Vector3 ARVRControllerDirection(Basis basis) => -basis.z.Rotated(basis.x.Normalized(), -(Main.VR && Main.PC ? Mathf.Pi * 3f / 16f : 0f)).Normalized();
		public Vector3 LeftControllerDirection => ARVRControllerDirection(LeftController.GlobalTransform.basis);
		public Vector3 RightControllerDirection => ARVRControllerDirection(RightController.GlobalTransform.basis);

		public Actor LeftTarget { get; set; } = null;
		public Actor RightTarget { get; set; } = null;
		public Actor Target(bool left = true) => left ? LeftTarget : RightTarget;
		public Actor Target(int which) => Target(which == 0);
		public ARVRPlayer SetTarget(bool left, Actor actor = null)
		{
			if (left)
				LeftTarget = actor;
			else
				RightTarget = actor;
			return this;
		}
		public ARVRPlayer SetTarget(int which, Actor billboard = null) => SetTarget(which == 0, billboard);

		public bool IsIn(Vector2 vector2) => IsInLocal(vector2 - Position);
		public bool IsIn(float x, float y) => IsInLocal(x - Position.x, y - Position.y);
		public bool IsInLocal(Vector2 vector2) => IsInLocal(vector2.x, vector2.y);
		public bool IsInLocal(float x, float y) =>
			x - Offset.x >= 0 && y - Offset.y >= 0 && x - Offset.x <= Size.x && y - Offset.y <= Size.y;
		public bool IsIn(Vector3 vector3) => IsIn(Assets.Vector2(vector3));
		public bool IsIn(float x, float y, float z) => IsIn(x, z);
		public bool IsInLocal(Vector3 vector3) => IsInLocal(Assets.Vector2(vector3));
		public bool IsInLocal(float x, float y, float z) => IsInLocal(x, z);
		public bool IsWithin(float x, float z, float distance) =>
			Math.Abs(GlobalTransform.origin.x - x) < distance && Math.Abs(GlobalTransform.origin.z - z) < distance;
		public Vector2 Size { get; set; } = new Vector2(Assets.WallWidth, Assets.WallWidth);
		public Vector2 Offset { get; set; } = new Vector2(Assets.HalfWallWidth, Assets.HalfWallWidth);
		public Vector2 Walk(Vector2 here, Vector2 there) => Main.ActionRoom.Level.Walk(here, there);
		public bool Push(Vector2 where) => Main.ActionRoom.Level.Push(where);
		public ushort? FloorCode => Main.ActionRoom.Level.Walls.IsNavigable(X, Z)
			&& Main.ActionRoom.Level.Walls.Map.GetMapData((ushort)X, (ushort)Z) is ushort floorCode
			&& floorCode >= Assets.FloorCodeFirst
			&& floorCode < Assets.FloorCodeFirst + Assets.FloorCodes ?
			(ushort)(floorCode - Assets.FloorCodeFirst)
			: (ushort?)null;
		public bool StandingOnOverride =>
			Main.ActionRoom.Level.Walls.Map.GetMapData((ushort)X, (ushort)Z) is ushort floorCode
			&& (Assets.XML?.Element("VSwap")?.Element("Walls")?.Elements("Override")?.Any(e => ushort.TryParse(e.Attribute("Number")?.Value, out ushort value) && value == floorCode) ?? false);

		public void Enter()
		{
			if (Main.Pancake)
				PancakeCamera.Current = true;
		}

		public Vector2 GlobalPosition
		{
			get => Assets.Vector2(GlobalTransform.origin);
			set => GlobalTransform = new Transform(GlobalTransform.basis, Assets.Vector3(value));
		}
		public Vector2 Position
		{
			get => Assets.Vector2(Transform.origin);
			set => Transform = new Transform(Transform.basis, Assets.Vector3(value));
		}
		public int X => Assets.IntCoordinate(GlobalTransform.origin.x);
		public int Z => Assets.IntCoordinate(GlobalTransform.origin.z);
	}
}
