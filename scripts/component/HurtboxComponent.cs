using Godot;
using GodotUtilities;

namespace Game.Component
{
    public class HurtboxComponent : Area2D
    {
        [Signal]
        public delegate void Hit(HitboxComponent hitbox);

        public override void _Notification(int what)
        {
            if (what == NotificationInstanced)
            {
                this.WireNodes();
            }
        }

        public override void _Ready()
        {
            Connect("area_entered", this, nameof(OnAreaEntered));
        }

        private void OnAreaEntered(Area2D otherArea)
        {
            if (otherArea is HitboxComponent hitbox)
            {
                EmitSignal(nameof(Hit), hitbox);
            }
        }
    }
}
