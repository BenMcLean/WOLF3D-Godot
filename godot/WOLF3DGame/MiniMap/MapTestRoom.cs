using Godot;

namespace WOLF3D.WOLF3DGame.MiniMap
{
	public class MapTestRoom : Node2D
	{
		public Camera2DFreeLook Camera;
		public MapTestRoom(ushort map)
		{
			AddChild(Camera = new Camera2DFreeLook());
			MiniMap miniMap = new MiniMap(Assets.MapAnalysis[map], new MiniMap.MapView(Assets.MapAnalysis[map]));
			miniMap.Illuminate(29, 57);
			AddChild(miniMap);
		}
	}
}
