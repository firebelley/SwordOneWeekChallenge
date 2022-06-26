using Game.Component;
using Godot;
using GodotUtilities;

namespace Game.GameObject
{
    public class SwordHitbox : HitboxComponent
    {
        [Node]
        private CollisionShape2D circleShape;
        [Node]
        private CollisionShape2D boxShape;
        [Node]
        private CollisionShape2D dashShape;

        private Sword currentSword;
        private Vector2 previousPosition;

        public override void _Notification(int what)
        {
            if (what == NotificationInstanced)
            {
                this.WireNodes();
            }
        }

        public override void _PhysicsProcess(float delta)
        {
            base._PhysicsProcess(delta);
            if (!dashShape.Disabled)
            {
                if (!IsInstanceValid(currentSword))
                {
                    QueueFree();
                    return;
                }
                var direction = currentSword.GlobalPosition - previousPosition;
                var tipDirection = currentSword.TipPosition - currentSword.GlobalPosition;
                var tipLength = tipDirection.Length();
                var length = direction.Length();
                var shape = dashShape.Shape as RectangleShape2D;
                shape.Extents = new Vector2((length + tipLength) / 2f, shape.Extents.y);

                GlobalPosition = previousPosition + (direction.Normalized() * (length + tipLength) / 2f);
                previousPosition = currentSword.GlobalPosition;
            }
        }

        public void EnableCircleShape()
        {
            DisableAll();
            circleShape.Disabled = false;
        }

        public void EnableBoxShape()
        {
            DisableAll();
            boxShape.Disabled = false;
        }

        public void EnableDashShape(Sword sword)
        {
            currentSword = sword;
            previousPosition = currentSword.GlobalPosition;
            DisableAll();
            dashShape.Disabled = false;
            oneShot = false;
            sword.Connect(nameof(Sword.DashEnded), this, nameof(OnDashEnded));
        }

        private void DisableAll()
        {
            boxShape.Disabled = true;
            dashShape.Disabled = true;
            circleShape.Disabled = true;
        }

        private void OnDashEnded()
        {
            oneShot = true;
        }
    }
}
