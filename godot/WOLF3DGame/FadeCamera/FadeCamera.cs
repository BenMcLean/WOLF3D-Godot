using Godot;

namespace WOLF3D.WOLF3DGame.FadeCamera
{
	public class FadeCamera : ARVRCamera, IFadeCamera
	{
		public const string GodotShaderCode = @"
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
";
		#region IFadeCamera
		public Color Color
		{
			get => (Color)((ShaderMaterial)((QuadMesh)ColorVeil.Mesh).Material).GetShaderParam("color");
			set
			{
				((ShaderMaterial)((QuadMesh)ColorVeil.Mesh).Material).SetShaderParam("color", value);
				ColorVeil.Visible = value.a8 > 0;
			}
		}
		public byte Black
		{
			get => (byte)((Color)((ShaderMaterial)((QuadMesh)BlackVeil.Mesh).Material).GetShaderParam("color")).a8;
			set
			{
				((ShaderMaterial)((QuadMesh)BlackVeil.Mesh).Material).SetShaderParam("color", Color.Color8(0, 0, 0, value));
				BlackVeil.Visible = value > 0;
			}
		}
		#endregion IFadeCamera
		#region FadeCamera
		public FadeCamera()
		{
			Name = "FadeCamera";
			AddChild(ColorVeil);
			AddChild(BlackVeil);
		}
		private MeshInstance BlackVeil = new MeshInstance()
		{
			Name = "BlackVeil",
			Mesh = new QuadMesh()
			{
				Size = new Vector2(1f, 1f),
				Material = new ShaderMaterial()
				{
					Shader = new Shader { Code = GodotShaderCode, },
					RenderPriority = 2,
				},
			},
			Transform = new Transform(Basis.Identity, Vector3.Forward),
			Visible = false,
		};
		private MeshInstance ColorVeil = new MeshInstance()
		{
			Name = "ColorVeil",
			Mesh = new QuadMesh()
			{
				Size = new Vector2(1f, 1f),
				Material = new ShaderMaterial()
				{
					Shader = new Shader { Code = GodotShaderCode, },
					RenderPriority = 2,
				},
			},
			Transform = new Transform(Basis.Identity, Vector3.Forward),
			Visible = false,
		};
		#endregion FadeCamera
	}
}
