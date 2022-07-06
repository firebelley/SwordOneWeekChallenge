using Game.Component;
using Game.Effect;
using Godot;
using GodotUtilities;
using GodotUtilities.Logic;

namespace Game.GameObject
{
    public class Ghoul : Enemy
    {
        private const float KNOCKBACK_COEFFICIENT = 5f;
        private const float DEATH_COEFFICIENT = 3f;
        private const float RANGE = 75f;
        private const float KNOCKBACK_FORCE = 250f;

        [Node]
        private Timer attackChargeTimer;
        [Node]
        private RandomTimerComponent attackCooldownTimer;
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
        private Sprite sprite;
        [Node]
        private RandomAudioStreamPlayerComponent attackStreamPlayerComponent;

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
            stateMachine.AddEnterState(State.Death, EnterStateDeath);
            stateMachine.AddState(State.Death, StateDeath);
            stateMachine.SetInitialState(State.Normal);

            pathfindComponent.Connect(nameof(PathfindComponent.VelocityUpdated), this, nameof(OnVelocityUpdated));
            healthComponent.Connect(nameof(HealthComponent.Died), this, nameof(OnDied));
            hurtboxComponent.Connect(nameof(HurtboxComponent.Hit), this, nameof(OnHit));

            attackCooldownTimer.Start();
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
            if (player?.GlobalPosition.DistanceSquaredTo(GlobalPosition) > RANGE * RANGE)
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
            UpdateFacing();
        }

        private void EnterStateKnockback()
        {
            velocityComponent.SetVelocity((blackboardComponent.GetPrimitiveValue<Vector2>(Constants.V_KNOCKBACK_DIRECTION) ?? Vector2.Zero) * KNOCKBACK_FORCE);
        }

        private void StateKnockback()
        {
            velocityComponent.AccelerateToVelocity(Vector2.Zero, KNOCKBACK_COEFFICIENT);
            if (velocityComponent.Velocity.LengthSquared() < 100)
            {
                stateMachine.ChangeState(StateNormal);
            }
        }

        private void EnterStateAttackCharge()
        {
            attackChargeTimer.Start();
            var attackCharge = resourcePreloader.InstanceSceneOrNull<AttackCharge>();
            AddChild(attackCharge);
            attackCharge.SetDuration(1f / attackChargeTimer.WaitTime);
        }

        private void StateAttackCharge()
        {
            PlayShakeAnimation();
            if (attackChargeTimer.IsStopped())
            {
                stateMachine.ChangeState(StateAttack);
            }
            velocityComponent.AccelerateToVelocity(Vector2.Zero);
        }

        private void EnterStateAttack()
        {
            var playerPos = GetTree().GetFirstNodeInGroup<Sword>()?.GlobalPosition;
            if (playerPos == null)
            {
                stateMachine.ChangeState(StateNormal);
                return;
            }
            var projectile = resourcePreloader.InstanceSceneOrNull<Projectile>();
            projectile.SetDirection((playerPos.Value - GlobalPosition).Normalized());
            GetParent().AddChild(projectile);
            projectile.GlobalPosition = GlobalPosition;
            attackStreamPlayerComponent.Play();
        }

        private void StateAttack()
        {
            stateMachine.ChangeState(StateNormal);
            velocityComponent.AccelerateToVelocity(Vector2.Zero);
        }

        private void LeaveStateAttack()
        {
            attackCooldownTimer.Start();
        }

        private void EnterStateDeath()
        {
            deathTimer.Start();
            hurtboxShape.Disabled = true;
            var node = resourcePreloader.InstanceSceneOrNull<Node2D>("EnemyDeath");
            AddChild(node);
        }

        private void StateDeath()
        {
            PlayShakeAnimation();
            PlayBlinkAnimation();
            velocityComponent.AccelerateToVelocity(Vector2.Zero, DEATH_COEFFICIENT);
            if (deathTimer.IsStopped())
            {
                var node = resourcePreloader.InstanceSceneOrNull<Node2D>("EnemyDeathExplosion");
                GetParent().AddChild(node);
                node.GlobalPosition = GlobalPosition;
                EmitSignal(nameof(Died));
                QueueFree();
            }
        }

        // TODO: abstract this out
        private void PlayShakeAnimation()
        {
            if (animationPlayer.CurrentAnimation != "shake" || !animationPlayer.IsPlaying())
            {
                animationPlayer.PlaybackSpeed = 1f / .1f;
                animationPlayer.Play("shake");
            }
        }

        // TODO: abstract this out
        private void PlayBlinkAnimation()
        {
            if (blinkAnimationPlayer.CurrentAnimation != "blink" || !blinkAnimationPlayer.IsPlaying())
            {
                blinkAnimationPlayer.PlaybackSpeed = 1f / .15f;
                blinkAnimationPlayer.Play("blink");
            }
        }

        private void UpdateFacing()
        {
            sprite.FlipH = velocityComponent.Velocity.x < 0;
        }

        private void OnVelocityUpdated(Vector2 velocity)
        {
            if (stateMachine.GetCurrentState() == State.Normal && velocity.LengthSquared() > 10)
            {
                velocityComponent.SetVelocity(velocity);
            }
        }

        private void OnHit(HitboxComponent hitboxComponent)
        {
            if (stateMachine.GetCurrentState() == State.Death)
            {
                return;
            }
            if (hitboxComponent.ApplyKnockback)
            {
                blackboardComponent.SetValue(Constants.V_KNOCKBACK_DIRECTION, Vector2.Right.Rotated(hitboxComponent.Rotation));
                stateMachine.ChangeState(StateKnockback);
            }
            healthComponent.Damage(1);
        }

        private void OnDied()
        {
            stateMachine.ChangeState(StateDeath);
        }

        private static class Constants
        {
            public static readonly string V_KNOCKBACK_DIRECTION = "v_knockback_direction";
        }
    }
}
