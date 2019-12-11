using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using WOLF3DGame.Model;

namespace WOLF3DGame
{
    class Billboard : Spatial
    {
        public Billboard(Material material)
        {
            AddChild(MeshInstance = new MeshInstance()
            {
                Mesh = Assets.Wall,
                MaterialOverride = material,
                Transform = Assets.BillboardTransform,
            });
        }

        public MeshInstance MeshInstance;

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
            for (uint i = 0; i < map.ObjectData.Length; i++)
                if (uint.TryParse(
                    (from e in objects.Elements("Billboard")
                     where (uint)e.Attribute("Number") == map.ObjectData[i]
                     select e.Attribute("Page")).FirstOrDefault()?.Value
                     ?? string.Empty,
                    out uint page
                    ))
                {
                    Billboard billboard = new Billboard(Game.Assets.WallMaterials[page])
                    {
                        GlobalTransform = new Transform(Basis.Identity, new Vector3((map.X(i) + 0.5f) * Assets.WallWidth, 0f, (map.Z(i) - 0.5f) * Assets.WallWidth)),
                    };
                    billboards.Add(billboard);
                }
            return billboards.ToArray();
        }
    }
}
