using Godot;
using GodotUtilities;

namespace Game.Component
{
    public class HitboxComponent : Area2D
    {
        [Signal]
        public delegate void HitHurtbox(HurtboxComponent hurtboxComponent);

        [Export]
        protected bool oneShot;
        [Export]
        public bool ApplyKnockback { get; set; }

        private bool shouldFree;

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

        public override void _PhysicsProcess(float delta)
        {
            if (shouldFree)
            {
                QueueFree();
            }
            shouldFree = oneShot;
        }

        private void OnAreaEntered(Area2D otherArea)
        {
            if (otherArea is HurtboxComponent hurtboxComponent)
            {
                EmitSignal(nameof(HitHurtbox), hurtboxComponent);
            }
        }
    }
}
