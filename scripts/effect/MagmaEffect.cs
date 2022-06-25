using Godot;
using GodotUtilities;

namespace Game.Effect
{
    public class MagmaEffect : Node2D
    {
        public override void _Notification(int what)
        {
            if (what == NotificationInstanced)
            {
                this.WireNodes();
            }
        }

        private Vector2 velocity;

        public override void _Ready()
        {
            velocity = Vector2.Right.Rotated(MathUtil.RNG.RandfRange(Mathf.Deg2Rad(-15f), Mathf.Deg2Rad(-90f + 15f))) * MathUtil.RNG.RandfRange(50f, 100f);
        }

        public override void _PhysicsProcess(float delta)
        {
            velocity += Vector2.Down * 200f * delta;
            GlobalPosition += velocity * delta;
        }
    }
}
