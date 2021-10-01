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
			if (Assets.LoadingPic is ImageTexture pic && pic != null)
			{
				ARVRCamera.AddChild(new MeshInstance()
				{
					Mesh = new QuadMesh()
					{
						Size = new Vector2(pic.GetWidth() * Assets.PixelWidth, pic.GetHeight() * Assets.PixelHeight),
					},
					MaterialOverride = new SpatialMaterial()
					{
						AlbedoTexture = pic,
						FlagsUnshaded = true,
						FlagsDoNotReceiveShadows = true,
						FlagsDisableAmbientLight = true,
						FlagsTransparent = false,
						ParamsCullMode = SpatialMaterial.CullMode.Back,
						ParamsSpecularMode = SpatialMaterial.SpecularMode.Disabled,
					},
					Transform = new Transform(Basis.Identity, Vector3.Forward * pic.GetWidth() * Assets.PixelWidth),
				});
			}
		}
		public LoadingRoom(GameMap map) : this()
		{
			Map = map;
			Name = "LoadingRoom for map " + Map.Number;
		}
		public LoadingRoom(XElement xml) : this()
		{
			XML = xml;
			Map = Assets.Maps[(int)xml.Element("Level").Attribute("MapNumber")];
			Name = "LoadingRoom for map " + Map.Number;
		}
		public GameMap Map { get; set; }
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
				Main.StatusBar["Episode"].Value = Map.Episode;
				Main.StatusBar["Floor"].Value = Map.Floor;
				Main.ActionRoom = new ActionRoom(Map);
			}
			else
			{
				Main.StatusBar = new StatusBar(XML.Element("StarusBar"));
				Main.ActionRoom = new ActionRoom(XML);
			}
			ChangeRoom(Main.ActionRoom);
		}
		public override void Enter()
		{
			base.Enter();
			Main.StatusBar["Floor"].Value = Map.Floor;
			Main.Color = Assets.Palettes[0][Map.Border];
			if (Map.Song is string songName
				&& Assets.AudioT.Songs.TryGetValue(songName, out AudioT.Song song)
				&& SoundBlaster.Song != song)
				SoundBlaster.Song = song;
			AmbientTasks.Add(Task.Run(Loading));
		}
		public override void _Process(float delta)
		{
			if (Paused)
				PausedProcess(delta);
		}
	}
}
