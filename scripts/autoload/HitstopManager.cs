using Godot;

namespace Game.Autoload
{
    public class HitstopManager : Node
    {
        private static HitstopManager instance;

        public override void _EnterTree()
        {
            instance = this;
        }

        public static void Hitstop()
        {
            instance.GetTree().Paused = true;
            instance.GetTree().CreateTimer(.06f, true).Connect("timeout", instance, nameof(ResetPause));
        }

        private void ResetPause()
        {
            instance.GetTree().Paused = false;
        }
    }
}
