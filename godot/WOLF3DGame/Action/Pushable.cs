using Godot;

namespace WOLF3D.WOLF3DGame.Action
{
	public abstract class Pushable : Target3D
	{
		public virtual bool Push() => Push(Direction8.CardinalToPoint(
			Main.ActionRoom.ARVRPlayer.GlobalTransform.origin,
			GlobalTransform.origin + new Vector3(Assets.HalfWallWidth, 0, Assets.HalfWallWidth)
			));
		public abstract bool Push(Direction8 direction);
	}
}
