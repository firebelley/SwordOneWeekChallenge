using Godot;
using GodotUtilities;

namespace Game.Autoload
{
    public class MusicPlayer : AudioStreamPlayer
    {
        [Node]
        private Timer waitTimer;

        public override void _EnterTree()
        {
            this.WireNodes();
        }

        public override void _Ready()
        {
            waitTimer.Connect("timeout", this, nameof(OnWaitTimerTimeout));
            Connect("finished", this, nameof(OnFinished));
        }

        private void OnWaitTimerTimeout()
        {
            Play();
        }

        private void OnFinished()
        {
            waitTimer.Start();
        }
    }
}
