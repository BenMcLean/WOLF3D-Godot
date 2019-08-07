using Godot;

public class Game : Node2D
{
    /// Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        WOLF3D.DownloadSharewareWOLF3D.Main();
        Sprite sprite = new Sprite
        {
            Name = "Sprite1",
            Texture = ResourceLoader.Load<Texture>("res://icon.png"),
        };
        AddChild(sprite);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    //  public override void _Process(float delta)
    //  {
    //      
    //  }
}
