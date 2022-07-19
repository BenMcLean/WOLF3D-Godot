using Godot;

namespace WOLF3D.WOLF3DGame.FadeCamera
{
	public interface IFadeCamera
	{
		Color Color { get; set; }
		byte Black { get; set; }
		FadeCameraController.BlackStateEnum BlackState { get; set; }
		void Flash(Color color);
	}
}
