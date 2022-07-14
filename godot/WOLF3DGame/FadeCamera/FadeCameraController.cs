using Godot;

namespace WOLF3D.WOLF3DGame.FadeCamera
{
	public class FadeCameraController : Node
	{
		#region FadeCameraController
		public IFadeCamera FadeCamera { get; set; }
		public FadeCameraController() => Name = "FadeCameraController";
		#endregion FadeCameraController
		#region Node
		public override void _Process(float delta)
		{
			ProcessColor(delta);
			ProcessBlack(delta);
		}
		#endregion Node
		#region Black
		public const float BlackDuration = 0.5f;
		public const float BlackFactor = 255f / BlackDuration;
		public float BlackTime = 0f;
		public enum BlackStateEnum
		{
			NONE, FADEOUT, BLACK, FADEIN
		}
		public BlackStateEnum BlackState
		{
			get => blackState;
			set
			{
				BlackTime = 0f;
				blackState = value;
				if (!(FadeCamera is null))
					FadeCamera.Black = (byte)(value == BlackStateEnum.NONE || value == BlackStateEnum.FADEOUT ? 0 : 255);
			}
		}
		private BlackStateEnum blackState = BlackStateEnum.NONE;
		public void ProcessBlack(float delta)
		{
			if (FadeCamera is null || BlackState == BlackStateEnum.NONE || BlackState == BlackStateEnum.BLACK)
				return;
			BlackTime += delta;
			if (BlackTime > BlackDuration)
				BlackState = BlackState == BlackStateEnum.FADEOUT ? BlackStateEnum.BLACK : BlackStateEnum.NONE;
			else
				FadeCamera.Black = (byte)(BlackState == BlackStateEnum.FADEOUT ?
					BlackTime * BlackFactor
					: 255 - BlackTime * BlackFactor);
		}
		#endregion Black
		#region Color
		public const float ColorDuration = 0.25f;
		public const float ColorHalfDuration = ColorDuration / 2f;
		public const byte ColorMaxIntensity = 64;
		public const float ColorFactor = ColorHalfDuration * ColorMaxIntensity;
		public Color Color = Color.Color8(255, 255, 0);
		public float ColorTime = 0f;
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
		public FadeCameraController Flash(Color color)
		{
			Color = color;
			ColorFlashing = true;
			return this;
		}
		public void ProcessColor(float delta)
		{
			if (FadeCamera is null || !ColorFlashing)
				return;
			ColorTime += delta;
			if (ColorTime > ColorDuration)
			{
				ColorTime = 0f;
				ColorFlashing = false;
				FadeCamera.Color = Color.Color8(0, 0, 0, 0);
				return;
			}
			FadeCamera.Color = Color.Color8((byte)Color.r8, (byte)Color.g8, (byte)Color.b8,
				(byte)(ColorTime > ColorHalfDuration ?
					ColorMaxIntensity - (ColorTime - ColorHalfDuration) / ColorFactor
					: ColorTime / ColorFactor)
				);
		}
		#endregion Color
	}
}
