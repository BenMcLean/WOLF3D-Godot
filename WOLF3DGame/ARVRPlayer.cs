using Godot;
using System.Collections;
using System.Collections.Generic;
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
        }

        public override void _PhysicsProcess(float delta)
        {
            Vector2 here = PlayerPosition, // where we are
                there = ARVRCameraPosition, // where we're going
                forward = ARVRCameraDirection; // which way we're facing

            if (RightController.GetJoystickAxis(1) > Assets.DeadZone || Input.IsKeyPressed((int)KeyList.Up) || Input.IsKeyPressed((int)KeyList.W))
                there += forward * Assets.RunSpeed * delta;

            if (CanReallyWalk(there))
                PlayerPosition = there;

            // Move ARVROrigin so that camera global position matches player global position
            ARVROrigin.Transform = new Transform(
                ARVROrigin.Transform.basis,
                new Vector3(
                    -ARVRCamera.Transform.origin.x,
                    Height,
                    -ARVRCamera.Transform.origin.z
                )
            );

            // Joystick and keyboard rotation
            float axis0 = RightController.GetJoystickAxis(0);
            if (Input.IsKeyPressed((int)KeyList.Left))
                axis0 -= 1;
            if (Input.IsKeyPressed((int)KeyList.Right))
                axis0 += 1;
            if (Mathf.Abs(axis0) > Assets.DeadZone)
                Rotate(Godot.Vector3.Up, Mathf.Pi * delta * (axis0 > 0f ? -1f : 1f));
            /*
            {
                Vector3 origHeadPos = ARVRCamera.GlobalTransform.origin;
                ARVROrigin.Rotate(Godot.Vector3.Up, Mathf.Pi * delta * (axis0 > 0f ? -1f : 1f));
                ARVROrigin.GlobalTransform = new Transform(ARVROrigin.GlobalTransform.basis, ARVROrigin.GlobalTransform.origin + origHeadPos - ARVRCamera.GlobalTransform.origin).Orthonormalized();
            }
            */
        }

        public float Height => Roomscale ?
            0f
            : (float)Assets.HalfWallHeight - ARVRCamera.Transform.origin.y;

        public Vector2 ARVROriginPosition => Assets.Vector2(ARVROrigin.GlobalTransform.origin);
        public Vector2 ARVRCameraPosition => Assets.Vector2(ARVRCamera.GlobalTransform.origin);
        public Vector2 ARVRCameraDirection => -Assets.Vector2(ARVRCamera.GlobalTransform.basis.z).Normalized();
        public Vector2 ARVRCameraMovement => ARVRCameraPosition - Assets.Vector2(GlobalTransform.origin);

        public delegate bool CanWalkDelegate(Vector2 there);
        public CanWalkDelegate CanWalk { get; set; } = (Vector2 there) => true;

        public bool CanReallyWalk(Vector2 there)
        {
            foreach (Direction8 direction in Direction8.Diagonals)
                if (!CanWalk(there + direction.Vector2 * Assets.HeadDiagonal))
                    return false;
            return CanWalk(there);
        }

        public Vector2 PlayerPosition
        {
            get => Assets.Vector2(GlobalTransform.origin);
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
