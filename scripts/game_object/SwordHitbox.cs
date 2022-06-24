using Godot;
using GodotUtilities;

namespace Game.GameObject
{
    public class SwordHitbox : Node2D
    {
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
            shouldFree = true;
        }
    }
}
