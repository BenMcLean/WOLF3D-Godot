using Godot;

namespace WOLF3D.WOLF3DGame.FadeCamera
{
	public class FadeCameraController : Node
	{
		public const float Duration = 0.25f;
		public const float HalfDuration = Duration / 2f;
		public const byte MaxIntensity = 64;
		public Color Color = Color.Color8(255, 255, 0);
		public float ColorTime = 0f;
		public float FadeTime = 0f;
		public bool ColorFlashing
		{
			get => colorFlashing;
			set
			{
				colorFlashing = value;
				ColorTime = 0f;
			}
		}
		private bool colorFlashing = false;
		public IFadeCamera FadeCamera { get; set; }
		public FadeCameraController() => Name = "FadeCameraController";
		public FadeCameraController Flash(Color color)
		{
			Color = color;
			ColorFlashing = true;
			return this;
		}
		public override void _Process(float delta) => ProcessColor(delta);
		public void ProcessColor(float delta)
		{
			if (FadeCamera == null || !ColorFlashing) return;
			ColorTime += delta;
			if (ColorTime > Duration)
			{
				ColorTime = 0f;
				ColorFlashing = false;
				FadeCamera.Color = Color.Color8(0, 0, 0, 0);
				return;
			}
			FadeCamera.Color = Color.Color8((byte)Color.r8, (byte)Color.g8, (byte)Color.b8,
				ColorTime > HalfDuration ?
				(byte)(MaxIntensity - (byte)((ColorTime - HalfDuration) / HalfDuration * MaxIntensity))
				: (byte)(ColorTime / HalfDuration * MaxIntensity)
				);
		}
	}
}
