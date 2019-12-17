using Godot;
using WOLF3DGame.Model;

namespace WOLF3DGame
{
    public class Actor : Billboard
    {
        public Direction8 Direction { get; set; } = Direction8.SOUTH;
        public string ActorName { get; set; } = "Guard";
        public string Animation { get; set; } = "Standing";
        public uint Frame { get; set; } = 0;
        private uint LastFrame { get; set; } = 0;

        public override void _Process(float delta)
        {
            base._Process(delta);
            if (MeshInstance.Visible && Game.Assets.Animations.TryGetValue(ActorName + "/" + Animation, out uint[][] frame))
            {
                uint newFrame = frame[Frame][Direction8.Modulus(
                        Direction8.Angle(
                            new Vector2(
                                GlobalTransform.origin.x,
                                GlobalTransform.origin.z
                            ).AngleToPoint(
                            new Vector2(
                                GetViewport().GetCamera().GlobalTransform.origin.x,
                                GetViewport().GetCamera().GlobalTransform.origin.z
                            )
                        )
                    ).MirrorX.Counter90,
                    frame[Frame].Length
                    )];
                if (newFrame != LastFrame)
                {
                    MeshInstance.MaterialOverride = Game.Assets.VSwapMaterials[newFrame];
                    LastFrame = newFrame;
                }
            }
        }
    }
}
