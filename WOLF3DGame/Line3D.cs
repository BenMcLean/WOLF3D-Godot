using Godot;

namespace WOLF3DGame
{
    /// <summary>
    /// Draws a 3D line in space, used for debugging.
    /// </summary>
    public class Line3D : ImmediateGeometry
    {
        public Line3D() =>
            MaterialOverride = new SpatialMaterial()
            {
                AlbedoColor = Color.Color8(255, 255, 255, 255),
                FlagsUnshaded = true,
                FlagsDoNotReceiveShadows = true,
                FlagsDisableAmbientLight = true,
                FlagsTransparent = false,
                ParamsCullMode = SpatialMaterial.CullMode.Disabled,
                ParamsSpecularMode = SpatialMaterial.SpecularMode.Disabled,
            };

        public Vector3[] Vertices { get; set; }

        public Color Color
        {
            get => ((SpatialMaterial)MaterialOverride).AlbedoColor;
            set => ((SpatialMaterial)MaterialOverride).AlbedoColor = value;
        }

        public override void _Process(float delta)
        {
            base._Process(delta);
            Clear();
            Begin(Mesh.PrimitiveType.Lines);
            if (Vertices != null)
                foreach (Vector3 vertex in Vertices)
                    AddVertex(vertex);
            End();
        }
    }
}
