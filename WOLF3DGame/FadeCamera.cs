using Godot;

namespace WOLF3D.WOLF3DGame
{
    public class FadeCamera : ARVRCamera
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
            Veil = new MeshInstance()
            {
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
        }

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
    }
}
