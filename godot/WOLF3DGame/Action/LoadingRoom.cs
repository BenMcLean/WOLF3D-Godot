using Godot;
using System.Threading.Tasks;
using System.Xml.Linq;
using Techsola;
using WOLF3D.WOLF3DGame.Menu;
using WOLF3D.WOLF3DGame.OPL;
using WOLF3DModel;

namespace WOLF3D.WOLF3DGame.Action
{
	public class LoadingRoom : Room
	{
		private LoadingRoom()
		{
			AddChild(ARVROrigin = new ARVROrigin());
			ARVROrigin.AddChild(ARVRCamera = new FadeCamera()
			{
				Current = true,
			});
			ARVROrigin.AddChild(LeftController = new ARVRController()
			{
				ControllerId = 1,
			});
			ARVROrigin.AddChild(RightController = new ARVRController()
			{
				ControllerId = 2,
			});
			//if (Assets.LoadingPic is AtlasTexture pic && pic != null)
			//{
			//	ARVRCamera.AddChild(new MeshInstance()
			//	{
			//		Mesh = new QuadMesh()
			//		{
			//			Size = new Vector2(pic.GetWidth() * Assets.PixelWidth, pic.GetHeight() * Assets.PixelHeight),
			//		},
			//		MaterialOverride = new SpatialMaterial()
			//		{
			//			AlbedoTexture = pic,
			//			FlagsUnshaded = true,
			//			FlagsDoNotReceiveShadows = true,
			//			FlagsDisableAmbientLight = true,
			//			FlagsTransparent = false,
			//			ParamsCullMode = SpatialMaterial.CullMode.Back,
			//			ParamsSpecularMode = SpatialMaterial.SpecularMode.Disabled,
			//		},
			//		Transform = new Transform(Basis.Identity, Vector3.Forward * pic.GetWidth() * Assets.PixelWidth),
			//	});
			//}
		}
		public LoadingRoom(ushort mapNumber) : this()
		{
			MapNumber = mapNumber;
			Name = "LoadingRoom for map " + MapNumber;
		}
		public LoadingRoom(XElement xml) : this()
		{
			XML = xml;
			MapNumber = ushort.Parse(xml.Element("Level").Attribute("MapNumber")?.Value);
			Name = "LoadingRoom for map " + MapNumber;
		}
		public ushort MapNumber { get; set; }
		public XElement XML { get; set; }
		public void Loading()
		{
			MenuRoom.LastPushedTile = 0;
			if (XML is null)
			{
				if (Main.NextLevelStats != null)
				{
					Main.StatusBar.Set(Main.NextLevelStats);
					Main.NextLevelStats = null;
				}
				Main.StatusBar["Episode"].Value = Assets.MapAnalysis[MapNumber].Episode;
				Main.StatusBar["Floor"].Value = Assets.MapAnalysis[MapNumber].Floor;
				Main.ActionRoom = new ActionRoom(MapNumber);
			}
			else
			{
				foreach (XElement stat in XML.Element("StatusBar").Elements("Number"))
					Main.StatusBar[stat.Attribute("Name").Value].Set(stat);
				Main.ActionRoom = new ActionRoom(XML);
			}
			ChangeRoom(Main.ActionRoom);
		}
		public override void Enter()
		{
			base.Enter();
			Main.StatusBar["Floor"].Value = Assets.MapAnalysis[MapNumber].Floor;
			Main.Color = Assets.Palettes[0][Assets.MapAnalysis[MapNumber].Border];
			if (Assets.MapAnalysis[MapNumber].Song is string songName
				&& Assets.AudioT.Songs.TryGetValue(songName, out AudioT.Song song)
				&& SoundBlaster.Song != song)
				SoundBlaster.Song = song;
			AmbientTasks.Add(Task.Run(Loading));
			//Loading();
		}
		public override void _Process(float delta)
		{
			if (Paused)
				PausedProcess(delta);
		}
	}
}
