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

        public Billboard(XElement xml) : this()
        {
            XML = xml;
            if (XML?.Attribute("Name")?.Value is string name && !string.IsNullOrWhiteSpace(name))
            {
                Name = name;
                Shape.Name = "Collision " + name;
            }
            if (ushort.TryParse(XML?.Attribute("Page")?.Value, out ushort page))
                Page = page;
        }

        public MeshInstance MeshInstance { get; set; } = null;

        public ImageTexture ImageTexture => MeshInstance?.MaterialOverride is SpatialMaterial spatialMaterial
                && spatialMaterial?.AlbedoTexture is ImageTexture imageTexture ?
                imageTexture
                : null;
        public Image Image => ImageTexture is ImageTexture imageTexture
            && imageTexture.GetData() is Image image ?
            image
            : null;

        public ushort? Page
        {
            get => page;
            set
            {
                if (value is ushort @ushort && Assets.VSwapMaterials != null && @ushort < Assets.VSwapMaterials.Length)
                {
                    page = @ushort;
                    MeshInstance.MaterialOverride = Assets.VSwapMaterials[@ushort];
                }
                else
                {
                    page = null;
                    MeshInstance.MaterialOverride = null;
                }
            }
        }
        private ushort? page = null;

        public bool IsHit(Vector3 vector3) => IsHitLocal(ToLocal(vector3));
        public bool IsHitLocal(Vector3 vector3) =>
            false && // TODO: remove this line
            Image is Image image
            && image.GetPixel(
                (int)((vector3.x - Assets.HalfWallWidth) / Assets.WallWidth * image.GetWidth()),
                image.GetHeight() - (int)(vector3.y / Assets.WallHeight * image.GetHeight())
                ).a < 0.5f;

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
                    billboards.Add(new Billboard(bx)
                    {
                        GlobalTransform = new Transform(Basis.Identity, new Vector3(Assets.CenterSquare(map.X(i)), 0f, Assets.CenterSquare(map.Z(i)))),
                    });
                else if (objects?.Elements("Pickup")
                        ?.Where(e => uint.TryParse(e.Attribute("Number")?.Value, out uint number) && number == map.ObjectData[i])
                        ?.FirstOrDefault() is XElement px && px != null)
                    billboards.Add(new Pickup(px)
                    {
                        GlobalTransform = new Transform(Basis.Identity, new Vector3(Assets.CenterSquare(map.X(i)), 0f, Assets.CenterSquare(map.Z(i)))),
                    });
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
