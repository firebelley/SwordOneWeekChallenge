using Game.GameObject;
using Godot;
using GodotUtilities;

namespace Game.UI
{
    public class GameUI : CanvasLayer
    {
        [Node("%DashIndicator")]
        private ColorRect dashIndicator;
        [Node("%LaunchIndicator")]
        private ColorRect launchIndicator;

        public override void _Notification(int what)
        {
            if (what == NotificationInstanced)
            {
                this.WireNodes();
            }
        }

        public void ConnectSword(Sword sword)
        {
            sword.Connect(nameof(Sword.DashTimerStarted), this, nameof(OnDashTimerStarted));
            sword.Connect(nameof(Sword.LaunchTimerStarted), this, nameof(OnLaunchTimerStarted));
        }

        private void OnDashTimerStarted(float time)
        {
            var timer = GetTree().CreateTimer(time);
            timer.Connect("timeout", this, nameof(OnDashTimeout));
            dashIndicator.Modulate = new Color(0f, 0f, 0f, 0f);
        }

        private void OnLaunchTimerStarted(float time)
        {
            var timer = GetTree().CreateTimer(time);
            timer.Connect("timeout", this, nameof(OnLaunchTimeout));
            launchIndicator.Modulate = new Color(0f, 0f, 0f, 0f);
        }

        private void OnDashTimeout()
        {
            dashIndicator.Modulate = Colors.White;
        }

        private void OnLaunchTimeout()
        {
            launchIndicator.Modulate = Colors.White;
        }
    }
}
