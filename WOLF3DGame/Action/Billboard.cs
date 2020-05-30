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

        public XElement XML { get; set; }
        public CollisionShape Shape { get; private set; }

        public Billboard()
        {
            Name = "Billboard";
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
            if (material != null)
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
                if (objects?.Elements("Billboard")
                    ?.Where(e => uint.TryParse(e.Attribute("Number")?.Value, out uint number) && number == map.ObjectData[i])
                    ?.FirstOrDefault() is XElement bx && bx != null)
                {
                    billboards.Add(new Billboard(
                        ushort.TryParse(bx?.Attribute("Page")?.Value, out ushort page) && page < Assets.VSwapMaterials.Length ?
                        Assets.VSwapMaterials[page]
                        : null
                        )
                    {
                        Name = bx?.Attribute("Name")?.Value,
                        XML = bx,
                        GlobalTransform = new Transform(Basis.Identity, new Vector3(Assets.CenterSquare(map.X(i)), 0f, Assets.CenterSquare(map.Z(i)))),
                    });
                }
                else if (Assets.Spawn.Where(
                    e => ushort.TryParse(e.Attribute("Number")?.Value, out ushort @ushort) && @ushort == map.ObjectData[i]
                    ).FirstOrDefault() is XElement spawn
                    && spawn != null
                    && (!byte.TryParse(spawn.Attribute("Difficulty")?.Value, out byte @byte) || @byte <= difficulty)
                            )
                    billboards.Add(new Actor()
                    {
                        Name = spawn?.Attribute("Actor")?.Value,
                        XML = spawn,
                        GlobalTransform = new Transform(Basis.Identity, new Vector3(Assets.CenterSquare(map.X(i)), 0f, Assets.CenterSquare(map.Z(i)))),
                        Direction = Direction8.From(spawn?.Attribute("Direction")?.Value),
                    });
            return billboards.ToArray();
        }
    }
}
