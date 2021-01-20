using Godot;

namespace WOLF3D.WOLF3DGame
{
    public class FadeCameraPancake : Camera, IFadeCamera
    {
        public FadeCameraPancake()
        {
            Veil = new MeshInstance()
            {
                Mesh = new QuadMesh()
                {
                    Size = new Vector2(1f, 1f),
                    Material = new ShaderMaterial()
                    {
                        Shader = FadeCamera.Shader,
                        RenderPriority = 2,
                    },
                },
                Transform = new Transform(Basis.Identity, Vector3.Forward),
            };
            Speaker = new AudioStreamPlayer3D();
        }

        #region IFadeCamera
        public Color Color
        {
            get => (Color)((ShaderMaterial)((QuadMesh)Veil.Mesh).Material).GetShaderParam("color");
            set => ((ShaderMaterial)((QuadMesh)Veil.Mesh).Material).SetShaderParam("color", value);
        }
        public MeshInstance Veil
        {
            get => veil;
            set
            {
                if (veil != null)
                    RemoveChild(veil);
                veil = value;
                if (veil != null)
                    AddChild(veil);
            }
        }
        private MeshInstance veil;
        #endregion IFadeCamera

        #region ISpeaker
        public AudioStreamSample Play
        {
            get => (AudioStreamSample)Speaker.Stream;
            set
            {
                Speaker.Stream = Settings.DigiSoundMuted ? null : value;
                if (value != null)
                    Speaker.Play();
            }
        }
        public AudioStreamPlayer3D Speaker
        {
            get => speaker;
            set
            {
                if (speaker != null)
                    RemoveChild(speaker);
                speaker = value;
                if (speaker != null)
                    AddChild(speaker);
            }
        }
        private AudioStreamPlayer3D speaker = null;
        #endregion ISpeaker
    }
}
