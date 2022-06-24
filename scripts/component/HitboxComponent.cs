using Godot;
using GodotUtilities;

namespace Game.Component
{
    public class HitboxComponent : Area2D
    {
        [Export]
        private bool oneShot;

        private bool shouldFree;

        public override void _Notification(int what)
        {
            if (what == NotificationInstanced)
            {
                this.WireNodes();
            }
        }

        public override void _PhysicsProcess(float delta)
        {
            if (shouldFree)
            {
                QueueFree();
            }
            shouldFree = oneShot;
        }
    }
}
