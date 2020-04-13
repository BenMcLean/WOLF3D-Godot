using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using WOLF3DModel;

namespace WOLF3D.WOLF3DGame.Action
{
    public class Billboard : StaticBody
    {
        public static readonly BoxShape BillboardShape = new BoxShape()
        {
            Extents = new Vector3(Assets.HalfWallWidth, Assets.HalfWallHeight, Assets.PixelWidth / 2f),
        };

        public CollisionShape Shape { get; private set; }

        public Billboard()
        {
            AddChild(Shape = new CollisionShape()
            {
                Shape = BillboardShape,
                Transform = new Transform(Basis.Identity, new Vector3(0f, Assets.HalfWallHeight, -Assets.PixelWidth)),
            });
            Shape.AddChild(MeshInstance = new MeshInstance()
            {
                Mesh = Assets.WallMesh,
                Transform = new Transform(Basis.Identity, new Vector3(0f, 0f, Assets.PixelWidth)),
            });
        }

        public Billboard(Material material) : this()
        {
            MeshInstance.MaterialOverride = material;
            /*
            // Cube for debugging purposes
            AddChild(new MeshInstance()
            {
                Mesh = new CubeMesh()
                {
                    Size = new Vector3(Assets.PixelWidth, Assets.PixelHeight, Assets.PixelWidth),
                    Material = new SpatialMaterial()
                    {
                        AlbedoColor = Color.Color8(0, 0, 255, 255),
                        FlagsUnshaded = true,
                        FlagsDoNotReceiveShadows = true,
                        FlagsDisableAmbientLight = true,
                        FlagsTransparent = false,
                        ParamsCullMode = SpatialMaterial.CullMode.Disabled,
                        ParamsSpecularMode = SpatialMaterial.SpecularMode.Disabled,
                    },
                }
            });
            */
        }

        public MeshInstance MeshInstance { get; set; }

        public override void _Process(float delta)
        {
            if (Visible)
                Rotation = ActionRoom.BillboardRotation;
        }

        public static Billboard[] Billboards(GameMap map, byte difficulty = 4)
        {
            XElement objects = Assets.XML?.Element("VSwap")?.Element("Objects");
            if (objects == null)
                throw new NullReferenceException("objects was null!");
            List<Billboard> billboards = new List<Billboard>();
            for (uint i = 0; i < map.ObjectData.Length; i++)
                if (uint.TryParse(
                    (from e in objects.Elements("Billboard")
                     where (uint)e.Attribute("Number") == map.ObjectData[i]
                     select e.Attribute("Page")).FirstOrDefault()?.Value,
                    out uint page
                    ))
                    billboards.Add(new Billboard(Assets.VSwapMaterials[page])
                    {
                        GlobalTransform = new Transform(Basis.Identity, new Vector3(Assets.CenterSquare(map.X(i)), 0f, Assets.CenterSquare(map.Z(i)))),
                    });
                else if (
                    (from e in objects.Elements("Spawn")
                     where ushort.TryParse(e.Attribute("Number")?.Value, out ushort @ushort) && @ushort == map.ObjectData[i]
                     select e).FirstOrDefault() is XElement spawn &&
                     spawn != null &&
                     (!byte.TryParse(spawn.Attribute("Difficulty")?.Value, out byte @byte) || @byte <= difficulty)
                     )
                    billboards.Add(new Actor()
                    {
                        GlobalTransform = new Transform(Basis.Identity, new Vector3(Assets.CenterSquare(map.X(i)), 0f, Assets.CenterSquare(map.Z(i)))),
                        Direction = Direction8.From(spawn.Attribute("Direction").Value),
                        ActorName = spawn.Attribute("Actor").Value,
                    });
            return billboards.ToArray();
        }
    }
}
