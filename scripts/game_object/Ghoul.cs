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
        private Timer attackChargeTimer;
        [Node]
        private Timer attackCooldownTimer;
        [Node]
        private HurtboxComponent hurtboxComponent;
        [Node]
        private BlackboardComponent blackboardComponent;
        [Node]
        private ResourcePreloader resourcePreloader;

        private enum State
        {
            Normal,
            Knockback,
            AttackCharge,
            Attack
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
            stateMachine.AddEnterState(State.AttackCharge, EnterStateAttackCharge);
            stateMachine.AddState(State.AttackCharge, StateAttackCharge);
            stateMachine.AddEnterState(State.Attack, EnterStateAttack);
            stateMachine.AddState(State.Attack, StateAttack);
            stateMachine.AddLeaveState(State.Attack, LeaveStateAttack);
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

            if (attackCooldownTimer.IsStopped() && HasPlayerLineOfSight())
            {
                stateMachine.ChangeState(StateAttackCharge);
            }

            AccelerateToVelocity(desiredVelocity, ACCELERATION_COEFFICIENT);
            navigationAgent2D.SetVelocity(velocity);
            velocity = MoveAndSlide(velocity);
        }

        private void EnterStateKnockback()
        {
            velocity = (blackboardComponent.GetPrimitiveValue<Vector2>(Constants.V_KNOCKBACK_DIRECTION) ?? Vector2.Zero) * KNOCKBACK_FORCE;
        }

        private void StateKnockback()
        {
            AccelerateToVelocity(Vector2.Zero, KNOCKBACK_COEFFICIENT);
            velocity = MoveAndSlide(velocity);
            if (velocity.LengthSquared() < 10)
            {
                stateMachine.ChangeState(StateNormal);
            }
        }

        private void EnterStateAttackCharge()
        {
            attackChargeTimer.Start();
        }

        private void StateAttackCharge()
        {
            // TODO: do some sort of particle and shake here
            if (attackChargeTimer.IsStopped())
            {
                stateMachine.ChangeState(StateAttack);
            }
            AccelerateToVelocity(Vector2.Zero, ACCELERATION_COEFFICIENT);
        }

        private void EnterStateAttack()
        {
            var playerPos = GetTree().GetFirstNodeInGroup<Sword>()?.GlobalPosition;
            if (playerPos == null)
            {
                stateMachine.ChangeState(StateNormal);
                return;
            }
            var magma = resourcePreloader.InstanceSceneOrNull<Projectile>();
            magma.SetDirection((playerPos.Value - GlobalPosition).Normalized());
            GetParent().AddChild(magma);
            magma.GlobalPosition = GlobalPosition;
        }

        private void StateAttack()
        {
            // TODO: have some animation here
            stateMachine.ChangeState(StateNormal);
            AccelerateToVelocity(Vector2.Zero, ACCELERATION_COEFFICIENT);
        }

        private void LeaveStateAttack()
        {
            attackCooldownTimer.WaitTime = MathUtil.RNG.RandfRange(3f, 5f);
            attackCooldownTimer.Start();
        }

        private void AccelerateToVelocity(Vector2 toVelocity, float coefficient)
        {
            velocity = velocity.LinearInterpolate(toVelocity, Mathf.Exp(-coefficient * GetPhysicsProcessDeltaTime()));
        }

        private bool HasPlayerLineOfSight()
        {
            var playerPos = GetTree().GetFirstNodeInGroup<Sword>()?.GlobalPosition;
            if (playerPos == null)
            {
                return false;
            }
            var raycast = GetTree().Root.World2d.DirectSpaceState.Raycast(GlobalPosition, playerPos.Value, null, 1 << 0, true, false);
            return raycast == null;
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
