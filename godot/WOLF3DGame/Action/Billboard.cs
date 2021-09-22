using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using WOLF3DModel;

namespace WOLF3D.WOLF3DGame.Action
{
	public class Billboard : Target3D, ISavable
	{
		#region Data
		public XElement XML { get; set; }
		public CollisionShape CollisionShape { get; private set; }
		public MeshInstance MeshInstance { get; set; } = null;
		public ushort? Page
		{
			get => page;
			set
			{
				if (value is ushort @ushort && Assets.VSwapMaterials != null && @ushort < Assets.VSwapMaterials.Length)
				{
					page = @ushort;
					MeshInstance.MaterialOverride = Assets.VSwapMaterials[@ushort];
					MeshInstance.Visible = true;
				}
				else
				{
					page = null;
					MeshInstance.MaterialOverride = null;
					MeshInstance.Visible = false;
				}
			}
		}
		private ushort? page = null;
		public virtual XElement Serialize()
		{
			XElement e = new XElement(XName.Get(GetType().Name));
			e.SetAttributeValue(XName.Get("Page"), Page);
			e.SetAttributeValue(XName.Get("X"), Transform.origin.x);
			e.SetAttributeValue(XName.Get("Z"), Transform.origin.z);
			e.SetAttributeValue(XName.Get("XML"), XML.ToString());
			return e;
		}
		#endregion Data
		#region Constructors
		public Billboard()
		{
			Name = "Billboard";
			AddChild(CollisionShape = new CollisionShape()
			{
				Shape = BillboardShape,
				Transform = new Transform(Basis.Identity, new Vector3(0f, Assets.HalfWallHeight, -Assets.PixelWidth)),
			});
			CollisionShape.AddChild(MeshInstance = new MeshInstance()
			{
				Mesh = Assets.WallMesh,
				Transform = new Transform(Basis.Identity, new Vector3(0f, 0f, Assets.PixelWidth)),
				Visible = false,
			});
			Size = new Vector2(Assets.WallWidth, Assets.WallWidth);
			Offset = new Vector2(-Assets.HalfWallWidth, -Assets.HalfWallWidth);
		}
		public Billboard(XElement xml) : this()
		{
			if (xml?.Attribute("Name")?.Value is string name)
			{
				Name = name;
				CollisionShape.Name = "Collision " + name;
			}
			if (ushort.TryParse(xml?.Attribute("Page")?.Value, out ushort page))
				Page = page;
			if (float.TryParse(xml?.Attribute("X")?.Value, out float x)
				&& float.TryParse(xml?.Attribute("Z")?.Value, out float z))
				Transform = new Transform(Transform.basis, new Vector3(x, 0f, z));
			XML = xml.Attribute("XML")?.Value is string a ? XElement.Parse(a) : xml;
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
					billboards.Add(new Actor(spawn)
					{
						GlobalTransform = new Transform(Basis.Identity, new Vector3(Assets.CenterSquare(map.X(i)), 0f, Assets.CenterSquare(map.Z(i)))),
					});
			return billboards.ToArray();
		}
		#endregion Constructors
		public bool IsHit(Vector3 vector3) => IsHitLocal(ToLocal(vector3));
		public bool IsHitLocal(Vector3 vector3) =>
			Assets.VSwap.IsTransparent(
				Page ?? 0,
				(ushort)((vector3.x + Assets.HalfWallWidth) / Assets.WallWidth * Assets.VSwap.TileSqrt),
				(ushort)(Assets.VSwap.TileSqrt - (vector3.y / Assets.WallHeight * Assets.VSwap.TileSqrt))
				);
		public static readonly BoxShape BillboardShape = new BoxShape()
		{
			Extents = new Vector3(Assets.HalfWallWidth, Assets.HalfWallHeight, Assets.PixelWidth / 2f),
		};
		public override void _Process(float delta)
		{
			if (Visible)
				Rotation = ActionRoom.BillboardRotation;
		}
		public int X => Assets.IntCoordinate(Transform.origin.x);
		public int Z => Assets.IntCoordinate(Transform.origin.z);
	}
}
