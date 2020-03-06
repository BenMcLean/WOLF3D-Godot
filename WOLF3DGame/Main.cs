using Godot;
using WOLF3DGame.Action;
using WOLF3DGame.Menu;
using WOLF3DGame.Setup;

namespace WOLF3DGame
{
	public class Main : Node
	{
		public Main() => I = this;
		public static Main I { get; private set; }
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
			Scene = new SetupRoom();
		}
	}
}
