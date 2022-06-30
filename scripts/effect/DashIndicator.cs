using Game.GameObject;
using Godot;
using GodotUtilities;

namespace Game.Effect
{
    public class DashIndicator : Node2D
    {
        [Node]
        private Timer timer;
        [Node]
        private TextureProgress textureProgress;

        private Sword sword;

        public override void _Notification(int what)
        {
            if (what == NotificationInstanced)
            {
                this.WireNodes();
            }
        }

        public override void _Ready()
        {
            timer.Connect("timeout", this, nameof(OnTimerTimeout));
        }

        public override void _Process(float delta)
        {
            if (IsInstanceValid(sword))
            {
                GlobalPosition = sword.GlobalPosition;
            }
            textureProgress.Value = timer.TimeLeft / timer.WaitTime;
        }

        public void SetSword(Sword sword)
        {
            this.sword = sword;
            GlobalPosition = sword.GlobalPosition;
        }

        public void StartTimer(float waitTime)
        {
            timer.WaitTime = waitTime;
            timer.Start();
        }

        private void OnTimerTimeout()
        {
            QueueFree();
        }
    }
}
