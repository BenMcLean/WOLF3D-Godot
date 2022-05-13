using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WOLF3DModel;

namespace WOLF3D.WOLF3DGame.MiniMap
{
	public class MapTestRoom : Node2D
	{
		public Camera2DFreeLook Camera;
		public MapTestRoom(GameMap map)
		{
			AddChild(Camera = new Camera2DFreeLook());
			AddChild(new MiniMap(map));
		}
	}
}
