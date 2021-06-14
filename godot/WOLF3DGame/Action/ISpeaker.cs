using Godot;

namespace WOLF3D.WOLF3DGame.Action
{
	public interface ISpeaker
	{
		AudioStreamSample Play { get; set; }
		AudioStreamPlayer3D Speaker { get; }
	}
}
