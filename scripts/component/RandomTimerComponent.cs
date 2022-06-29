using Godot;
using GodotUtilities;

namespace Game
{
    public class RandomTimerComponent : Timer
    {
        [Export]
        private float minTime = 1;
        [Export]
        private float maxTime = 2;

        public new void Start(float timeSec = -1)
        {
            WaitTime = MathUtil.RNG.RandfRange(minTime, maxTime);
            base.Start(timeSec);
        }
    }
}
