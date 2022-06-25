using Godot;
using GodotUtilities;

namespace Game
{
    public class SwordHit : Node2D
    {
        public override void _Notification(int what)
        {
            if (what == NotificationInstanced)
            {
                this.WireNodes();
            }
        }

        public override void _Ready()
        {
            RotationDegrees = MathUtil.RNG.RandfRange(0, 360);
        }
    }
}
