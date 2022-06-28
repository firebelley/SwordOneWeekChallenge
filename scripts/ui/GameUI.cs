using Game.Component;
using Game.GameObject;
using Godot;
using GodotUtilities;

namespace Game.UI
{
    public class GameUI : CanvasLayer
    {
        [Node("%DashIndicator")]
        private ColorRect dashIndicator;
        [Node("%HealthBarComponent")]
        private HealthBarComponent healthBarComponent;

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
            healthBarComponent.SetHealthComponent(sword.HealthComponent);
        }

        private void OnDashTimerStarted(float time)
        {
            var timer = GetTree().CreateTimer(time);
            timer.Connect("timeout", this, nameof(OnDashTimeout));
            dashIndicator.Modulate = new Color(0f, 0f, 0f, 0f);
        }

        private void OnDashTimeout()
        {
            dashIndicator.Modulate = Colors.White;
        }
    }
}
