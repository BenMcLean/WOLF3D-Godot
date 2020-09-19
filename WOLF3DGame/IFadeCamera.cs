using Godot;

namespace WOLF3D.WOLF3DGame
{
    public interface IFadeCamera
    {
        Color Color { get; set; }
        MeshInstance Veil { get; set; }
    }
}
