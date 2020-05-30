using Godot;

namespace WOLF3D.WOLF3DGame.Action
{
    public class Actor : Billboard
    {
        public Actor()
        {
            Name = "Actor";
        }
        public Direction8 Direction { get; set; } = Direction8.SOUTH;
        public string Animation { get; set; } = "Standing";
        public uint Frame { get; set; } = 0;
        private uint LastFrame { get; set; } = 0;

        public override void _Process(float delta)
        {
            base._Process(delta);
            if (MeshInstance.Visible && Assets.Animations.TryGetValue(XML?.Attribute("Actor")?.Value + "/" + Animation, out uint[][] frame))
            {
                uint newFrame = frame[Frame][Direction8.Modulus(
                    Direction8.AngleToPoint(
                                GlobalTransform.origin.x,
                                GlobalTransform.origin.z,
                                GetViewport().GetCamera().GlobalTransform.origin.x,
                                GetViewport().GetCamera().GlobalTransform.origin.z
                    ).MirrorZ + Direction,
                    frame[Frame].Length
                    )];
                if (newFrame != LastFrame)
                {
                    MeshInstance.MaterialOverride = Assets.VSwapMaterials[newFrame];
                    LastFrame = newFrame;
                }
            }
        }
    }
}
