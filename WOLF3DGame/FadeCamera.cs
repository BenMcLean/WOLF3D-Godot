using Godot;

namespace WOLF3D.WOLF3DGame
{
    public class FadeCamera : ARVRCamera, IFadeCamera
    {
        public readonly static Shader Shader = new Shader()
        {
            Code = @"
shader_type spatial;
render_mode blend_mix, skip_vertex_transform, cull_disabled, unshaded, depth_draw_never, depth_test_disable;

uniform vec4 color : hint_color;

void vertex() {
    POSITION = vec4(2.*UV - 1., 0., 1.);
}
void fragment() {
    ALBEDO = color.rgb;
    ALPHA = color.a;
}
",
        };

        public FadeCamera()
        {
            Name = "FadeCamera";
            Veil = new MeshInstance()
            {
                Name = "Veil",
                Mesh = new QuadMesh()
                {
                    Size = new Vector2(1f, 1f),
                    Material = new ShaderMaterial()
                    {
                        Shader = Shader,
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
