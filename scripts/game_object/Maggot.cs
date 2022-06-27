using Game.Component;
using Game.Effect;
using Game.Level;
using Godot;
using GodotUtilities;
using GodotUtilities.Logic;

namespace Game.GameObject
{
    public class Maggot : KinematicBody2D
    {
        private const float RANGE = 50f;

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
        [Node]
        private HealthComponent healthComponent;
        [Node]
        private AnimationPlayer animationPlayer;
        [Node]
        private AnimationPlayer blinkAnimationPlayer;
        [Node("%HurtboxShape")]
        private CollisionShape2D hurtboxShape;
        [Node]
        private Timer deathTimer;
        [Node]
        private VelocityComponent velocityComponent;
        [Node]
        private PathfindComponent pathfindComponent;
        [Node]
        private PlayerLineOfSightComponent playerLineOfSightComponent;
        [Node]
        private RayCast2D laserCast;

        private enum State
        {
            Normal,
            Knockback,
            AttackCharge,
            Attack,
            Death
        }
        private StateMachine<State> stateMachine = new();

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
            // stateMachine.AddEnterState(State.Death, EnterStateDeath);
            // stateMachine.AddState(State.Death, StateDeath);
            stateMachine.SetInitialState(State.Normal);

            pathfindComponent.Connect(nameof(PathfindComponent.VelocityUpdated), this, nameof(OnVelocityUpdated));
            healthComponent.Connect(nameof(HealthComponent.Died), this, nameof(OnDied));
            hurtboxComponent.Connect(nameof(HurtboxComponent.Hit), this, nameof(OnHit));
        }

        public override void _PhysicsProcess(float delta)
        {
            stateMachine.Update();
            velocityComponent.MoveAndSlide();
        }

        private void StateNormal()
        {
            var player = GetTree().GetFirstNodeInGroup<Sword>();
            var desiredVelocity = Vector2.Zero;
            if ((player?.GlobalPosition ?? GlobalPosition).DistanceSquaredTo(GlobalPosition) < RANGE * RANGE)
            {
                // TODO: flee!
                // this.GetAncestor<BaseLevel>().FreeTiles;
            }
            else
            {
                var targetPos = player?.GlobalPosition ?? GlobalPosition;
                pathfindComponent.SetTargetInterval(targetPos);
                desiredVelocity = velocityComponent.GetTargetVelocity(pathfindComponent);
            }

            if (attackCooldownTimer.IsStopped() && playerLineOfSightComponent.HasLineOfSight())
            {
                stateMachine.ChangeState(StateAttackCharge);
            }

            velocityComponent.AccelerateToVelocity(desiredVelocity);
            pathfindComponent.SetVelocity(velocityComponent.Velocity);
        }

        private void EnterStateKnockback()
        {

        }

        private void StateKnockback()
        {

        }

        private void EnterStateAttackCharge()
        {
            var playerPos = GetTree().GetFirstNodeInGroup<Sword>()?.CenterMass ?? GlobalPosition;
            var toPlayer = playerPos - GlobalPosition;
            var direction = toPlayer.Normalized();

            var laser = resourcePreloader.InstanceSceneOrNull<LaserCharge>();
            Vector2 finalDirection;
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                finalDirection = direction.x > 0 ? Vector2.Right : Vector2.Left;
            }
            else
            {
                finalDirection = direction.y > 0 ? Vector2.Down : Vector2.Up;
            }

            AddChild(laser);
            laser.SetDirection(finalDirection);
            attackChargeTimer.Start();
        }

        private void StateAttackCharge()
        {
            velocityComponent.AccelerateToVelocity(Vector2.Zero);
            if (attackChargeTimer.IsStopped())
            {
                stateMachine.ChangeState(StateAttack);
            }
        }

        private void EnterStateAttack()
        {

        }

        private void StateAttack()
        {
            stateMachine.ChangeState(StateNormal);
        }

        private void LeaveStateAttack()
        {
            attackCooldownTimer.Start();
        }

        private void OnDied()
        {

        }

        private void OnVelocityUpdated(Vector2 velocity)
        {
            if (stateMachine.GetCurrentState() == State.Normal)
            {
                velocityComponent.SetVelocity(velocity);
            }
        }

        private void OnHit()
        {

        }
    }
}
