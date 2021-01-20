using Godot;

namespace WOLF3D.WOLF3DGame
{
    public interface ISpeaker
    {
        AudioStreamSample Play { get; set; }
        AudioStreamPlayer3D Speaker { get; }
    }
}
