using Godot;
using WOLF3D.WOLF3DGame.Action;
using WOLF3D.WOLF3DGame.Menu;
using WOLF3D.WOLF3DGame.OPL;
using WOLF3D.WOLF3DGame.Setup;

namespace WOLF3D.WOLF3DGame
{
	public class Main : Node
	{
		public Main() => I = this;
		public static Main I { get; private set; }
		public static string Folder { get; set; }
		public static ActionRoom ActionRoom { get; set; }
		public static MenuRoom MenuRoom { get; set; }
		public static Node Scene
		{
			get => I.scene;
			set
			{
				if (I.scene != null)
					I.RemoveChild(I.scene);
				I.AddChild(I.scene = value);
			}
		}
		private Node scene = null;

		public override void _Ready()
		{
			base._Ready();
			AddChild(SoundBlaster.OplPlayer);
			Scene = new SetupRoom();
		}

		public static void Load()
		{
			Assets.Load(Folder);
			ActionRoom = new ActionRoom();
			MenuRoom = new MenuRoom();
			Scene = ActionRoom;
		}
	}
}
