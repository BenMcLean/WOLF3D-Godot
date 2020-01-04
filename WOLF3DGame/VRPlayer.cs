using Godot;
using WOLF3DGame.Model;

namespace WOLF3D
{
    public class VRPlayer : KinematicBody
    {
        public bool Roomscale { get; set; } = false;
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

        public override void _PhysicsProcess(float delta) =>
            ARVROrigin.Transform = new Transform(
                ARVROrigin.Transform.basis,
                new Vector3(
                    -ARVRCamera.Transform.origin.x,
                    Roomscale ?
                        0f
                        : (float)Assets.HalfWallHeight - ARVRCamera.Transform.origin.y,
                    -ARVRCamera.Transform.origin.z
                )
            );
    }
}
