using Game.Component;
using Game.Effect;
using Game.Level;
using Godot;
using GodotUtilities;
using GodotUtilities.Logic;

namespace Game.GameObject
{
    public class Maggot : Enemy
    {
        private const float RANGE = 50f;
        private const float KNOCKBACK_FORCE = 250f;
        private const float KNOCKBACK_COEFFICIENT = 3f;
        private const float DEATH_COEFFICIENT = 3f;
        private const float ATTACK_COEFFICIENT = 5f;

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
        private Sprite sprite;

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

            StartAttackCooldown();
        }

        public override void _PhysicsProcess(float delta)
        {
            stateMachine.Update();
            velocityComponent.MoveAndSlide();
        }

        private void StartAttackCooldown()
        {
            attackCooldownTimer.WaitTime = MathUtil.RNG.RandfRange(2f, 4f);
            attackCooldownTimer.Start();
        }

        private void StateNormal()
        {
            var player = GetTree().GetFirstNodeInGroup<Sword>();
            var desiredVelocity = Vector2.Zero;
            if ((player?.GlobalPosition ?? GlobalPosition).DistanceSquaredTo(GlobalPosition) < RANGE * RANGE)
            {
                // TODO: flee!
                var freeTiles = this.GetAncestor<BaseLevel>()?.FreeTiles;
                if (freeTiles != null)
                {
                    var targetTile = MathUtil.RNG.RandiRange(0, freeTiles.Count - 1);
                    pathfindComponent.SetTargetInterval((freeTiles[targetTile] * 16f) + (Vector2.One * 8f), 2f);
                }
            }
            else
            {
                var targetPos = player?.GlobalPosition ?? GlobalPosition;
                pathfindComponent.SetTargetInterval(targetPos);
            }

            desiredVelocity = velocityComponent.GetTargetVelocity(pathfindComponent);

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

            var attackCharge = resourcePreloader.InstanceSceneOrNull<AttackCharge>();
            AddChild(attackCharge);
            attackCharge.GlobalPosition = GlobalPosition;
            attackCharge.SetDuration(1f / attackChargeTimer.WaitTime);

            blackboardComponent.SetValue(Constants.V_ATTACK_DIRECTION, finalDirection);
        }

        private void StateAttackCharge()
        {
            PlayShakeAnimation();
            velocityComponent.AccelerateToVelocity(Vector2.Zero, ATTACK_COEFFICIENT);
            if (attackChargeTimer.IsStopped())
            {
                stateMachine.ChangeState(StateAttack);
            }
        }

        private void EnterStateAttack()
        {
            var laser = resourcePreloader.InstanceSceneOrNull<LaserAttack>();
            AddChild(laser);
            laser.SetDirection(blackboardComponent.GetPrimitiveValue<Vector2>(Constants.V_ATTACK_DIRECTION) ?? Vector2.Right);
        }

        private void StateAttack()
        {
            stateMachine.ChangeState(StateNormal);
        }

        private void LeaveStateAttack()
        {
            StartAttackCooldown();
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

        private void OnDied()
        {
            stateMachine.ChangeState(StateDeath);
        }

        private void OnVelocityUpdated(Vector2 velocity)
        {
            if (stateMachine.GetCurrentState() == State.Normal)
            {
                velocityComponent.SetVelocity(velocity);
            }
        }

        private void OnHit(HitboxComponent hitboxComponent)
        {
            if (hitboxComponent.ApplyKnockback)
            {
                blackboardComponent.SetValue(Constants.V_KNOCKBACK_DIRECTION, Vector2.Right.Rotated(hitboxComponent.Rotation));
                stateMachine.ChangeState(StateKnockback);
            }
            healthComponent.Damage(1);
        }

        private static class Constants
        {
            public static readonly string V_KNOCKBACK_DIRECTION = "v_knockback_direction";
            public static readonly string V_ATTACK_DIRECTION = "v_attack_direction";
        }
    }
}
