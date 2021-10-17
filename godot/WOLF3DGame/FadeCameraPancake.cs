using Godot;

namespace WOLF3D.WOLF3DGame
{
	public class FadeCameraPancake : Camera, IFadeCamera
	{
		public FadeCameraPancake()
		{
			Name = "FadeCameraPancake";
			Veil = new MeshInstance()
			{
				Name = "Veil",
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
