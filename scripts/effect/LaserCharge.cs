using Game.Component;
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
        [Node]
        private Line2D damageLine;
        [Node("%HitboxShape")]
        private CollisionShape2D hitboxShape;
        [Node]
        private HitboxComponent hitboxComponent;

        [Export]
        private Color horizontalModulate;
        [Export]
        private Color verticalModulate;

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
            rayCast2D.CastTo = direction * 1000f;
            rayCast2D.ForceRaycastUpdate();

            var length = (rayCast2D.GetCollisionPoint() - GlobalPosition).Length();

            line2D.Points = new Vector2[] {
                Vector2.Zero,
                direction * length
            };

            damageLine.Points = new Vector2[] {
                line2D.Points[0],
                line2D.Points[1]
            };

            var shape = hitboxShape.Shape as RectangleShape2D;
            shape.Extents = new Vector2(length / 2f, shape.Extents.y);
            hitboxComponent.Position = (line2D.Points[1] - line2D.Points[0]) / 2f;
            hitboxComponent.Rotation = direction.Angle();
        }

        public void SetDirection(Vector2 dir)
        {
            direction = dir.Normalized();
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                damageLine.Modulate = horizontalModulate;
            }
            else
            {
                damageLine.Modulate = verticalModulate;
            }
        }
    }
}
