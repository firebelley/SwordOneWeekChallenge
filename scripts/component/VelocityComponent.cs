using Godot;

namespace Game.Component
{
    public class VelocityComponent : Node
    {
        [Export]
        private float accelerationCoefficient = 200f;
        [Export]
        private float speed = 50f;

        public Vector2 Velocity { get; private set; }

        public void AccelerateToVelocity(Vector2 toVelocity, float coefficient)
        {
            Velocity = Velocity.LinearInterpolate(toVelocity, Mathf.Exp(-coefficient * GetPhysicsProcessDeltaTime()));
        }

        public void AccelerateToVelocity(Vector2 toVelocity)
        {
            AccelerateToVelocity(toVelocity, accelerationCoefficient);
        }

        public Vector2 GetTargetVelocity(PathfindComponent pathfindComponent)
        {
            if (Owner is not Node2D node)
            {
                return Vector2.Zero;
            }
            return (pathfindComponent.GetNextLocation() - node.GlobalPosition).Normalized() * speed;
        }

        public void SetVelocity(Vector2 velocity)
        {
            Velocity = velocity;
        }

        public void MoveAndSlide()
        {
            if (Owner is not KinematicBody2D body)
            {
                return;
            }
            Velocity = body.MoveAndSlide(Velocity);
        }
    }
}
