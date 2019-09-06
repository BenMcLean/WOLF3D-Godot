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
                MaterialOverride = BillboardMaterial,
                Centered = false,
                GlobalTransform = new Transform(Basis.Identity, Assets.BillboardLocal),
            };
            AddChild(Sprite3D);
        }

        public Sprite3D Sprite3D;

        public override void _Process(float delta)
        {
            base._Process(delta);
            if (Sprite3D.Visible)
                Rotation = new Vector3(0f, GetViewport().GetCamera().GlobalTransform.basis.GetEuler().y, 0f);
        }

        public static readonly SpatialMaterial BillboardMaterial = new SpatialMaterial()
        {
            FlagsUnshaded = true,
            FlagsDoNotReceiveShadows = true,
            FlagsDisableAmbientLight = true,
            ParamsSpecularMode = SpatialMaterial.SpecularMode.Disabled,
            ParamsCullMode = SpatialMaterial.CullMode.Back,
            FlagsTransparent = true,
        };
    }
}
