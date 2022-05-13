using Godot;

public class Camera2DFreeLook : Camera2D
{
	public override void _Process(float delta) => Position += InputDirection * (speed * delta);
	public static Vector2 InputDirection => new Vector2(
			Direction(Input.IsActionPressed("ui_right")) - Direction(Input.IsActionPressed("ui_left")),
			Direction(Input.IsActionPressed("ui_down")) - Direction(Input.IsActionPressed("ui_up"))
			);
	private static int Direction(bool @bool) => @bool ? 1 : -1;
	private const float speed = 1000f;
}
