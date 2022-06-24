using Game.Component;
using Godot;
using GodotUtilities;
using GodotUtilities.Logic;

namespace Game.GameObject
{
    public class Ghoul : KinematicBody2D
    {
        private const float ACCELERATION_COEFFICIENT = 200f;
        private const float KNOCKBACK_COEFFICIENT = 150f;
        private const float SPEED = 50f;
        private const float RANGE = 75f;
        private const float KNOCKBACK_FORCE = 250f;

        [Node]
        private NavigationAgent2D navigationAgent2D;
        [Node]
        private Timer pathfindTimer;
        [Node]
        private HurtboxComponent hurtboxComponent;
        [Node]
        private BlackboardComponent blackboardComponent;

        private enum State
        {
            Normal,
            Knockback
        }
        private StateMachine<State> stateMachine = new();

        private Vector2 velocity;

        public override void _Notification(int what)
        {
            if (what == NotificationInstanced)
            {
                this.WireNodes();
            }
        }

        public override void _Ready()
        {
            stateMachine.AddState(State.Normal, StateNormal);
            stateMachine.AddEnterState(State.Knockback, EnterStateKnockback);
            stateMachine.AddState(State.Knockback, StateKnockback);
            stateMachine.SetInitialState(State.Normal);

            navigationAgent2D.Connect("velocity_computed", this, nameof(OnVelocityComputed));
            hurtboxComponent.Connect(nameof(HurtboxComponent.Hit), this, nameof(OnHit));
        }

        public override void _PhysicsProcess(float delta)
        {
            stateMachine.Update();
        }

        private void StateNormal()
        {

            var player = GetTree().GetFirstNodeInGroup<Sword>();
            var desiredVelocity = Vector2.Zero;
            if (player.GlobalPosition.DistanceSquaredTo(GlobalPosition) > RANGE * RANGE)
            {
                if (pathfindTimer.IsStopped())
                {
                    var targetPos = player?.GlobalPosition ?? GlobalPosition;
                    pathfindTimer.Start();
                    navigationAgent2D.SetTargetLocation(targetPos);
                }
                desiredVelocity = (navigationAgent2D.GetNextLocation() - GlobalPosition).Normalized() * SPEED;
            }

            velocity = velocity.LinearInterpolate(desiredVelocity, Mathf.Exp(-ACCELERATION_COEFFICIENT * GetPhysicsProcessDeltaTime()));
            navigationAgent2D.SetVelocity(velocity);
            velocity = MoveAndSlide(velocity);
        }

        private void EnterStateKnockback()
        {
            velocity = (blackboardComponent.GetPrimitiveValue<Vector2>(Constants.V_KNOCKBACK_DIRECTION) ?? Vector2.Zero) * KNOCKBACK_FORCE;
        }

        private void StateKnockback()
        {
            velocity = velocity.LinearInterpolate(Vector2.Zero, Mathf.Exp(-KNOCKBACK_COEFFICIENT * GetPhysicsProcessDeltaTime()));
            velocity = MoveAndSlide(velocity);
            if (velocity.LengthSquared() < 10)
            {
                stateMachine.ChangeState(StateNormal);
            }
        }

        private void OnVelocityComputed(Vector2 vel)
        {
            if (stateMachine.GetCurrentState() == State.Normal)
            {
                velocity = vel;
            }
        }

        private void OnHit(HitboxComponent hitboxComponent)
        {
            blackboardComponent.SetValue(Constants.V_KNOCKBACK_DIRECTION, Vector2.Right.Rotated(hitboxComponent.Rotation));
            stateMachine.ChangeState(StateKnockback);
        }

        private static class Constants
        {
            public static readonly string V_KNOCKBACK_DIRECTION = "v_knockback_direction";
        }
    }
}
