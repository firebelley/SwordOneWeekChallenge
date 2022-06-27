using Godot;
using GodotUtilities;

namespace Game.Component
{
    public class PathfindComponent : NavigationAgent2D
    {
        [Signal]
        public delegate void VelocityUpdated(Vector2 velocity);

        [Node]
        private Timer timer;

        public override void _Notification(int what)
        {
            if (what == NotificationInstanced)
            {
                this.WireNodes();
            }
        }

        public override void _Ready()
        {
            Connect("velocity_computed", this, nameof(OnVelocityComputed));
        }

        public void SetTargetInterval(Vector2 targetPos)
        {
            if (!timer.IsStopped()) return;
            timer.Start();
            SetTargetLocation(targetPos);
        }

        private void OnVelocityComputed(Vector2 velocity)
        {
            EmitSignal(nameof(VelocityUpdated), velocity);
        }
    }
}
