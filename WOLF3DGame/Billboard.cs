using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using WOLF3DGame.Model;

namespace WOLF3DGame
{
    public class Billboard : Spatial
    {
        public Billboard()
        {
            AddChild(MeshInstance = new MeshInstance()
            {
                Mesh = Assets.WallMesh,
                Transform = Assets.BillboardTransform,
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
            base._Process(delta);
            if (Visible)
                Rotation = Game.BillboardRotation;
        }

        public static Billboard[] MakeBillboards(GameMap map)
        {
            XElement objects = Game.Assets?.XML?.Element("VSwap")?.Element("Objects");
            if (objects == null)
                throw new NullReferenceException("objects was null!");
            List<Billboard> billboards = new List<Billboard>();
            XElement spawn;
            for (uint i = 0; i < map.ObjectData.Length; i++)
                if (uint.TryParse(
                    (from e in objects.Elements("Billboard")
                     where (uint)e.Attribute("Number") == map.ObjectData[i]
                     select e.Attribute("Page")).FirstOrDefault()?.Value,
                    out uint page
                    ))
                    billboards.Add(new Billboard(Game.Assets.VSwapMaterials[page])
                    {
                        GlobalTransform = new Transform(Basis.Identity, new Vector3(Assets.CenterSquare(map.X(i)), 0f, Assets.CenterSquare(map.Z(i)))),
                    });
                else if ((spawn = (from e in objects.Elements("Spawn")
                                   where (uint)e.Attribute("Number") == map.ObjectData[i]
                                   select e).FirstOrDefault()) != null)
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
