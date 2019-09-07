using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using WOLF3DSim;

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

        public static Billboard[] MakeBillboards(GameMaps.Map map)
        {
            XElement objects = Game.Assets.Game.Element("VSwap").Element("Objects");
            if (objects == null)
                throw new NullReferenceException("objects was null!");
            List<Billboard> billboards = new List<Billboard>();
            for (uint i = 0; i < map.ObjectData.Length; i++)
                if (uint.TryParse(
                    (from e in objects.Elements("Billboard")
                     where (uint)e.Attribute("Number") == map.ObjectData[i]
                     select e.Attribute("Pages")).FirstOrDefault()?.Value
                     ?? string.Empty,
                    out uint page
                    ))
                {
                    Billboard billboard = new Billboard()
                    {
                        GlobalTransform = new Transform(Basis.Identity, new Vector3((map.X(i) + 0.5f) * Assets.WallWidth, 0f, (map.Z(i) - 0.5f) * Assets.WallWidth)),
                    };
                    billboard.Sprite3D.Texture = Game.Assets.Textures[page];
                    billboards.Add(billboard);
                }
            return billboards.ToArray();
        }
    }
}
