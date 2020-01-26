using Godot;
using System;
using WOLF3DGame.Model;

namespace WOLF3DGame
{
    public class ARVRPlayer : Spatial
    {
        public bool Roomscale { get; set; } = true;
        public ARVROrigin ARVROrigin { get; set; }
        public ARVRCamera ARVRCamera { get; set; }
        public ARVRController LeftController { get; set; }
        public ARVRController RightController { get; set; }

        public override void _Ready()
        {
            AddChild(ARVROrigin = new ARVROrigin());
            ARVROrigin.AddChild(LeftController = new ARVRController()
            {
                ControllerId = 1,
            });
            ARVROrigin.AddChild(RightController = new ARVRController()
            {
                ControllerId = 2,
            });
            ARVROrigin.AddChild(ARVRCamera = new ARVRCamera()
            {
                Current = true,
            });

            /*
            ARVROrigin.AddChild(new MeshInstance()
            {
                Mesh = new CubeMesh()
                {
                    Size = new Vector3(Assets.HeadXZ, Assets.HeadXZ, Assets.HeadXZ),
                },
                MaterialOverride = new SpatialMaterial()
                {
                    AlbedoColor = Color.Color8(255, 165, 0, 255), // Orange
                    FlagsUnshaded = true,
                    FlagsDoNotReceiveShadows = true,
                    FlagsDisableAmbientLight = true,
                    FlagsTransparent = false,
                    ParamsCullMode = SpatialMaterial.CullMode.Disabled,
                    ParamsSpecularMode = SpatialMaterial.SpecularMode.Disabled,
                },
            });
            AddChild(new MeshInstance()
            {
                Mesh = new CubeMesh()
                {
                    Size = new Vector3(Assets.HeadXZ, Assets.HeadXZ, Assets.HeadXZ),
                },
                MaterialOverride = new SpatialMaterial()
                {
                    AlbedoColor = Color.Color8(255, 0, 255, 255), // Purple
                    FlagsUnshaded = true,
                    FlagsDoNotReceiveShadows = true,
                    FlagsDisableAmbientLight = true,
                    FlagsTransparent = false,
                    ParamsCullMode = SpatialMaterial.CullMode.Disabled,
                    ParamsSpecularMode = SpatialMaterial.SpecularMode.Disabled,
                },
            });
            */
        }

        public static float Strength(float input) =>
            Mathf.Abs(input) < Assets.DeadZone ? 0f
            : (Mathf.Abs(input) - Assets.DeadZone) / (1f - Assets.DeadZone) * Mathf.Sign(input);

        public override void _PhysicsProcess(float delta)
        {
            base._PhysicsProcess(delta);
            Vector2 forward = ARVRCameraDirection, // which way we're facing
                movement = Vector2.Zero; // movement vector from joystick and keyboard input
            bool keyPressed = false; // if true then we go max speed and ignore what the joysticks say.
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

            PlayerPosition = Walk(PlayerPosition, Walk(
                    PlayerPosition,
                    PlayerPosition + ARVRCameraMovement + movement * delta * (Input.IsKeyPressed((int)KeyList.Shift) ? Assets.WalkSpeed : Assets.RunSpeed)
                    ));

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
                Rotate(Godot.Vector3.Up, Mathf.Pi * delta * axis0);

            if (RightController.IsButtonPressed((int)Godot.JoystickList.VrTrigger) > 0)
            {
                if (!Shooting)
                {
                    Game.Line3D.Vertices = new Vector3[] {
                        RightController.GlobalTransform.origin,
                        RightController.GlobalTransform.origin + RightControllerDirection * ShotRange
                    };
                    Godot.Collections.Dictionary result = GetWorld().DirectSpaceState.IntersectRay(
                        Game.Line3D.Vertices[0],
                        Game.Line3D.Vertices[1]
                        );

                    GD.Print("Shooting! Range: " + ShotRange + " Time: " + DateTime.Now);
                    if (result.Count > 0)
                    {
                        CollisionObject collider = (CollisionObject)result["collider"];
                        GD.Print(
                            ((CollisionShape)collider.ShapeOwnerGetOwner(collider.ShapeFindOwner((int)result["shape"]))).Name
                            );
                    }
                    else
                        GD.Print("Hit nothing! :(");
                    Shooting = true;
                }
            }
            else
                Shooting = false;

            if (Input.IsKeyPressed((int)KeyList.Space))
            {
                if (!Pushing)
                {
                    Push(new Vector2(
                        PlayerPosition.x - Direction8.CardinalFrom(ARVRCameraDirection).X * Assets.WallWidth,
                        PlayerPosition.y - Direction8.CardinalFrom(ARVRCameraDirection).Z * Assets.WallWidth
                        ));
                    Pushing = true;
                }
            }
            else if (RightController.IsButtonPressed((int)Godot.JoystickList.VrGrip) > 0)
            {
                if (!Pushing)
                {
                    Push(Assets.Vector2(RightController.GlobalTransform.origin));
                    Pushing = true;
                }
            }
            else if (LeftController.IsButtonPressed((int)Godot.JoystickList.VrGrip) > 0)
            {
                if (!Pushing)
                {
                    Push(Assets.Vector2(LeftController.GlobalTransform.origin));
                    Pushing = true;
                }
            }
            else
                Pushing = false;
        }

        public bool Shooting { get; set; } = false;
        public bool Pushing { get; set; } = false;
        public float ShotRange { get; set; } = Mathf.Sqrt(Mathf.Pow(64 * Assets.WallWidth, 2) * 2f + Mathf.Pow((float)Assets.WallHeight, 2));

        public float Height => Roomscale ?
            0f
            : (float)Assets.HalfWallHeight - ARVRCamera.Transform.origin.y;

        public Vector2 ARVROriginPosition => Assets.Vector2(ARVROrigin.GlobalTransform.origin);
        public Vector2 ARVRCameraPosition => Assets.Vector2(ARVRCamera.GlobalTransform.origin);
        public Vector2 ARVRCameraDirection => -Assets.Vector2(ARVRCamera.GlobalTransform.basis.z).Normalized();
        public Vector2 ARVRCameraMovement => ARVRCameraPosition - Assets.Vector2(GlobalTransform.origin);

        public static Vector3 ARVRControllerDirection(Basis basis) => -basis.z.Rotated(basis.x.Normalized(), -Mathf.Pi * 3f / 16f).Normalized();
        public Vector3 LeftControllerDirection => ARVRControllerDirection(LeftController.GlobalTransform.basis);
        public Vector3 RightControllerDirection => ARVRControllerDirection(RightController.GlobalTransform.basis);

        public delegate Vector2 WalkDelegate(Vector2 here, Vector2 there);
        public WalkDelegate Walk { get; set; } = (Vector2 here, Vector2 there) => here;
        public delegate bool PushDelegate(Vector2 where);
        public PushDelegate Push { get; set; }

        public Vector2 PlayerPosition
        {
            get => Assets.Vector2(GlobalTransform.origin);
            set => GlobalTransform = new Transform(
                    GlobalTransform.basis,
                    Assets.Vector3(value)
                );
        }
    }
}
