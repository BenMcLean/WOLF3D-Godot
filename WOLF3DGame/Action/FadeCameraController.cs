using Godot;

namespace WOLF3D.WOLF3DGame.Action
{
    public class FadeCameraController : Node
    {
        public const float Duration = 0.25f;
        public const float HalfDuration = Duration / 2f;
        public const byte MaxIntensity = 64;
        public Color Color = Color.Color8(255, 255, 0);
        public float Time = 0f;
        public bool Flashing
        {
            get => flashing;
            set
            {
                flashing = value;
                Time = 0f;
            }
        }
        private bool flashing = false;
        public IFadeCamera FadeCamera { get; set; }
        public FadeCameraController() => Name = "FadeCameraController";
        public override void _Process(float delta)
        {
            if (FadeCamera == null || !Flashing) return;
            Time += delta;
            if (Time > Duration)
            {
                Time = 0f;
                Flashing = false;
                FadeCamera.Color = Color.Color8(0, 0, 0, 0);
                return;
            }
            FadeCamera.Color = Color.Color8((byte)Color.r8, (byte)Color.g8, (byte)Color.b8,
                Time > HalfDuration ?
                (byte)(MaxIntensity - (byte)(((Time - HalfDuration) / HalfDuration) * MaxIntensity))
                : (byte)((Time / HalfDuration) * MaxIntensity)
                );
        }
    }
}
