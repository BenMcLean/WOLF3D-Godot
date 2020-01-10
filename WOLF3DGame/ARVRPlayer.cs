using Godot;
using WOLF3DGame.Model;

namespace WOLF3DGame
{
    public class ARVRPlayer : KinematicBody
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
        }

        public override void _PhysicsProcess(float delta)
        {
            Vector2 here = PlayerPosition, // where we are
                there = ARVRCameraPosition, // where we're going
                forward = ARVRCameraDirection; // which way we're facing

            if (RightController.GetJoystickAxis(1) > Assets.DeadZone || Input.IsKeyPressed((int)KeyList.Up) || Input.IsKeyPressed((int)KeyList.W))
                there += forward * Assets.RunSpeed * delta;

            if (CanWalk(there))
                PlayerPosition = there;

            // Joystick and keyboard rotation
            float axis0 = RightController.GetJoystickAxis(0);
            if (Input.IsKeyPressed((int)KeyList.Left))
                axis0 -= 1;
            if (Input.IsKeyPressed((int)KeyList.Right))
                axis0 += 1;
            if (Mathf.Abs(axis0) > Assets.DeadZone)
            {
                Vector3 origHeadPos = ARVRCamera.GlobalTransform.origin;
                ARVROrigin.Rotate(Godot.Vector3.Up, Mathf.Pi * delta * (axis0 > 0f ? -1f : 1f));
                ARVROrigin.GlobalTransform = new Transform(ARVROrigin.GlobalTransform.basis, ARVROrigin.GlobalTransform.origin + origHeadPos - ARVRCamera.GlobalTransform.origin).Orthonormalized();
            }

            // Move ARVROrigin so that camera global position matches player global position
            ARVROrigin.Transform = new Transform(
                ARVROrigin.Transform.basis,
                new Vector3(
                    -ARVRCamera.Transform.origin.x,
                    Height,
                    -ARVRCamera.Transform.origin.z
                )
            );
        }

        public float Height => Roomscale ?
            0f
            : (float)Assets.HalfWallHeight - ARVRCamera.Transform.origin.y;

        public static Vector2 Vector2(Vector3 vector3) => new Vector2(vector3.x, vector3.z);
        public static Vector3 Vector3(Vector2 vector2) => new Vector3(vector2.x, 0f, vector2.y);
        public Vector2 ARVROriginPosition => Vector2(ARVROrigin.GlobalTransform.origin);
        public Vector2 ARVRCameraPosition => Vector2(ARVRCamera.GlobalTransform.origin);
        public Vector2 ARVRCameraDirection => -Vector2(ARVRCamera.GlobalTransform.basis.z).Normalized();
        public Vector2 ARVRCameraMovement => ARVRCameraPosition - Vector2(GlobalTransform.origin);

        public delegate bool CanWalkDelegate(Vector2 there);
        public CanWalkDelegate CanWalk { get; set; } = (Vector2 there) => true;

        public Vector2 PlayerPosition
        {
            get => Vector2(GlobalTransform.origin);
            set => GlobalTransform = new Transform(
                    GlobalTransform.basis,
                    new Vector3(
                        value.x,
                        0f,
                        value.y
                    )
                );
        }
    }
}
