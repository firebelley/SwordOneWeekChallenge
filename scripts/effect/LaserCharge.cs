using Godot;
using GodotUtilities;

namespace Game.Effect
{
    public class LaserCharge : Node2D
    {
        [Node]
        private RayCast2D rayCast2D;
        [Node]
        private Line2D line2D;

        private Vector2 direction;

        public override void _Notification(int what)
        {
            if (what == NotificationInstanced)
            {
                this.WireNodes();
            }
        }

        public override void _PhysicsProcess(float delta)
        {
            rayCast2D.CastTo = GlobalPosition + (direction * 1000f);
            rayCast2D.ForceRaycastUpdate();

            var length = (rayCast2D.GetCollisionPoint() - GlobalPosition).Length();

            line2D.Points = new Vector2[] {
                Vector2.Zero,
                direction * length
            };

            // GD.Print(line2D.ToLocal(GlobalPosition) - line2D.ToLocal(rayCast2D.GetCollisionPoint()).Normalized());
            // var castLength = Mathf.Abs((rayCast2D.GetCollisionPoint() - GlobalPosition).x);
            // Scale = new Vector2(castLength, 1);
        }

        public void SetDirection(Vector2 dir)
        {
            direction = dir.Normalized();
        }
    }
}
