using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WOLF3D
{
    class Billboard : Spatial
    {
        public Billboard()
        {
            Sprite3D = new Sprite3D()
            {
                PixelSize = Assets.PixelWidth,
                Scale = Assets.Scale,
                MaterialOverride = Assets.WallMaterial,
                Centered = false,
                GlobalTransform = new Transform(Basis.Identity, Assets.BillboardLocal),
            };
            AddChild(Sprite3D);
        }

        public Sprite3D Sprite3D;

        public Vector3 CameraFloor
        {
            get
            {
                cameraFloor.Set(
                    GetViewport().GetCamera().GlobalTransform.origin.x,
                    0f,
                    GetViewport().GetCamera().GlobalTransform.origin.z
                    );
                return cameraFloor;
            }
        }
        private static Vector3 cameraFloor = new Vector3(0f, 0f, 0f);

        public override void _Process(float delta)
        {
            base._Process(delta);
            if (Sprite3D.Visible)
                LookAt(CameraFloor, Vector3.Up);
        }
    }
}
