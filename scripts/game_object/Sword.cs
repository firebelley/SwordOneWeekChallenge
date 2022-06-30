using Game.Component;
using Game.Effect;
using Godot;
using GodotUtilities;
using GodotUtilities.Logic;

namespace Game.GameObject
{
    public class Sword : RigidBody2D
    {
        [Signal]
        public delegate void DashEnded();
        [Signal]
        public delegate void EnemyHit();
        [Signal]
        public delegate void DamageTaken();
        [Signal]
        public delegate void Died();

        [Node]
        private Timer dashTimer;
        [Node]
        private Timer attackIntervalTimer;
        [Node]
        private Timer attackStateTimer;
        [Node]
        private Timer attackResetTimer;
        [Node("Visuals/Sprite")]
        private Sprite sprite;
        [Node("Visuals/AnimatedSpriteAttack1")]
        private AnimatedSprite animatedSpriteAttack1;
        [Node("Visuals/AnimatedSpriteAttack2")]
        private AnimatedSprite animatedSpriteAttack2;
        [Node("Visuals/AnimatedSpriteAttack3")]
        private AnimatedSprite animatedSpriteAttack3;
        [Node]
        private ResourcePreloader resourcePreloader;
        [Node]
        private Position2D tip;
        [Node]
        private HurtboxComponent hurtboxComponent;
        [Node]
        private Particles2D dashParticles;
        [Node]
        private HealthComponent healthComponent;
        [Node("%HurtboxShape")]
        private CollisionShape2D hurtboxShape;
        [Node]
        private AnimationPlayer animationPlayer;
        [Node]
        private RandomAudioStreamPlayerComponent randomAudioStreamPlayerComponent;
        [Node]
        private AudioStreamPlayer hitStreamPlayer1;
        [Node]
        private AudioStreamPlayer hitStreamPlayer2;

        [Export]
        private bool trainingMode;

        private Vector2 previousPosition;

        private const int TORQUE_COEFFICIENT = 800_000;
        private const float DASH_FORCE = 1500f;
        private const float ATTACK_FORCE = 1000f;
        private const float DASH_LINEAR_DAMP = 12f;
        private const float ATTACK_LINEAR_DAMP = 20f;

        public Vector2 TipPosition => tip.GlobalPosition;
        public HealthComponent HealthComponent => healthComponent;
        public Vector2 CenterMass => hurtboxShape.GlobalPosition;

        public override void _Notification(int what)
        {
            if (what == NotificationInstanced)
            {
                this.WireNodes();
            }
        }

        private enum State
        {
            Normal,
            Dash,
            Attack,
            Fly,
            Dead
        }
        private StateMachine<State> stateMachine = new();

        private int attackChain;
        private float attackTime;
        private bool hitThisFrame;

        public override void _UnhandledInput(InputEvent evt)
        {
            if (evt.IsActionPressed("dash"))
            {
                GetTree().SetInputAsHandled();
                CallDeferred(nameof(TryDash));
            }
        }

        public override void _Ready()
        {
            AddToGroup(nameof(Sword));
            stateMachine.AddState(State.Normal, StateNormal);
            stateMachine.AddEnterState(State.Dash, EnterStateDash);
            stateMachine.AddState(State.Dash, StateDash);
            stateMachine.AddLeaveState(State.Dash, LeaveStateDash);
            stateMachine.AddEnterState(State.Attack, EnterStateAttack);
            stateMachine.AddState(State.Attack, StateAttack);
            stateMachine.AddLeaveState(State.Attack, LeaveStateAttack);
            stateMachine.AddEnterState(State.Fly, EnterStateFly);
            stateMachine.AddState(State.Fly, StateFly);
            stateMachine.AddLeaveState(State.Fly, LeaveStateFly);
            stateMachine.AddEnterState(State.Dead, EnterStateDead);
            stateMachine.AddState(State.Dead, StateDead);
            stateMachine.SetInitialState(StateNormal);

            animatedSpriteAttack1.Visible = false;
            animatedSpriteAttack2.Visible = false;

            attackTime = attackIntervalTimer.WaitTime;

            hurtboxComponent.Connect(nameof(HurtboxComponent.Hit), this, nameof(OnHit));
            attackResetTimer.Connect("timeout", this, nameof(OnAttackResetTimeout));
            healthComponent.Connect(nameof(HealthComponent.Died), this, nameof(OnDied));
        }

        public override void _PhysicsProcess(float delta)
        {
            stateMachine.Update();
            sprite.FlipV = !((RotationDegrees is > -90 and < 0) || (RotationDegrees is < 90 and > 0));
            animatedSpriteAttack1.FlipV = sprite.FlipV;
            animatedSpriteAttack2.FlipV = !sprite.FlipV;
            animatedSpriteAttack3.FlipV = sprite.FlipV;
            previousPosition = GlobalPosition;
            hitThisFrame = false;
        }

        private void StateNormal()
        {
            CheckInsideTerrain();
            ApplyTorqueTowardMouse();
            GravityScale = LinearVelocity.y < 0 ? 1.5f : 1f;

            if (Input.IsActionPressed("attack"))
            {
                TryAttack();
            }
            else if (Input.IsActionPressed("fly"))
            {
                stateMachine.ChangeState(StateFly);
            }
        }

        private void EnterStateDash()
        {
            dashTimer.Start();
            var direction = this.GetMouseDirection();
            ApplyCentralImpulse(direction * DASH_FORCE);
            var dashIndicator = resourcePreloader.InstanceSceneOrNull<DashIndicator>();
            GetParent().AddChild(dashIndicator);
            dashIndicator.SetSword(this);
            dashIndicator.StartTimer(dashTimer.WaitTime);

            GravityScale = 0;
            LinearDamp = DASH_LINEAR_DAMP;
            Rotation = direction.Angle();
            AppliedTorque = 0f;
            dashParticles.Emitting = true;
            (dashParticles.ProcessMaterial as ParticlesMaterial).Angle = Mathf.Rad2Deg(-direction.Angle());

            var hitbox = resourcePreloader.InstanceSceneOrNull<SwordHitbox>();
            GetParent().AddChild(hitbox);
            hitbox.EnableDashShape(this);
            hitbox.GlobalPosition = GlobalPosition;
            hitbox.Rotation = direction.Angle();
            hitbox.ApplyKnockback = false;
            hitbox.Connect(nameof(HitboxComponent.HitHurtbox), this, nameof(OnHurtboxHit));

            randomAudioStreamPlayerComponent.Play();
        }

        private void StateDash()
        {
            hurtboxShape.Disabled = true;
            if (CheckInsideTerrain())
            {
                stateMachine.ChangeState(StateNormal);
            }
            else if (Input.IsActionPressed("attack"))
            {
                TryAttack();
            }
            else if (dashTimer.TimeLeft < .75f)
            {
                stateMachine.ChangeState(StateNormal);
            }
        }

        private void LeaveStateDash()
        {
            GravityScale = 1;
            LinearDamp = 0;
            dashParticles.Emitting = false;
            hurtboxShape.Disabled = false;
            EmitSignal(nameof(DashEnded));
        }

        private void EnterStateAttack()
        {
            if (attackChain > 2)
            {
                attackChain = 0;
            }

            var animatedSprite = animatedSpriteAttack1;
            if (attackChain == 1)
            {
                animatedSprite = animatedSpriteAttack2;
            }
            else if (attackChain == 2)
            {
                animatedSprite = animatedSpriteAttack3;
            }

            attackResetTimer.Start();
            attackStateTimer.Start();
            attackIntervalTimer.WaitTime = attackTime;
            if (attackChain == 2)
            {
                attackIntervalTimer.WaitTime = attackTime * 2f;
            }
            attackIntervalTimer.Start();
            GravityScale = 0;
            LinearVelocity = Vector2.Zero;
            ApplyCentralImpulse(this.GetMouseDirection() * ATTACK_FORCE);
            LinearDamp = ATTACK_LINEAR_DAMP;
            AppliedTorque = 0f;
            Rotation = this.GetMouseDirection().Angle();

            sprite.Visible = false;
            animatedSprite.Visible = true;
            animatedSprite.Play("attack");
            animatedSprite.Frame = 0;

            var hitbox = resourcePreloader.InstanceSceneOrNull<SwordHitbox>();
            GetParent().AddChild(hitbox);
            if (attackChain == 2)
            {
                hitbox.EnableCircleShape();
            }
            else
            {
                hitbox.EnableBoxShape();
            }
            hitbox.GlobalPosition = GlobalPosition;
            hitbox.Rotation = this.GetMouseDirection().Angle();
            hitbox.Connect(nameof(HitboxComponent.HitHurtbox), this, nameof(OnHurtboxHit));

            attackChain++;
            randomAudioStreamPlayerComponent.Play();
        }

        private void StateAttack()
        {
            CheckInsideTerrain();
            ApplyTorqueTowardMouse();
            if (attackStateTimer.IsStopped())
            {
                stateMachine.ChangeState(StateNormal);
            }
        }

        private void LeaveStateAttack()
        {
            GravityScale = 1f;
            LinearDamp = 0f;
            sprite.Visible = true;
            animatedSpriteAttack1.Visible = false;
            animatedSpriteAttack2.Visible = false;
            animatedSpriteAttack3.Visible = false;
        }

        private void EnterStateFly()
        {
            LinearDamp = 12f;
        }

        private void StateFly()
        {
            CheckInsideTerrain();
            ApplyTorqueTowardMouse();
            AppliedForce = Vector2.Right.Rotated(Rotation) * 3000f;

            if (!Input.IsActionPressed("fly"))
            {
                stateMachine.ChangeState(StateNormal);
            }
            else if (Input.IsActionPressed("attack"))
            {
                TryAttack();
            }
        }

        private void LeaveStateFly()
        {
            LinearDamp = 0f;
            AppliedForce = Vector2.Zero;
            AppliedTorque = 0f;
        }

        private void EnterStateDead()
        {
            AngularDamp = 5f;
            LinearDamp = 0f;
            AppliedTorque = 0f;
            AppliedForce = Vector2.Zero;
            ApplyTorqueImpulse(MathUtil.RNG.RandfRange(-2000f, 2000f));
        }

        private void StateDead()
        {
            CheckInsideTerrain();
        }

        private void ApplyTorqueTowardMouse()
        {
            var currentAngle = Vector2.Right.Rotated(Rotation);
            var desiredAngle = this.GetMouseDirection();
            var angleTo = currentAngle.AngleTo(desiredAngle);
            AppliedTorque = angleTo * GetPhysicsProcessDeltaTime() * TORQUE_COEFFICIENT;
        }

        private bool CheckInsideTerrain()
        {
            var result = GetTree().Root.World2d.DirectSpaceState.Raycast(previousPosition, tip.GlobalPosition, null, 1 << 0, true, false);
            if (result != null)
            {
                var offset = tip.GlobalPosition - GlobalPosition;
                GlobalPosition = result.Position - offset;
                LinearVelocity = Vector2.Zero;
                return true;
            }

            result = GetTree().Root.World2d.DirectSpaceState.Raycast(previousPosition, GlobalPosition, null, 1 << 0, true, false);
            if (result != null)
            {
                GlobalPosition = previousPosition + result.Normal;
                LinearVelocity = Vector2.Zero;
                return true;
            }
            return false;
        }

        private void TryDash()
        {
            if (stateMachine.GetCurrentState() == State.Dead)
            {
                return;
            }

            if (dashTimer.IsStopped())
            {
                stateMachine.ChangeState(StateDash);
            }
        }

        private void TryAttack()
        {
            if (stateMachine.GetCurrentState() == State.Dead)
            {
                return;
            }

            if (attackIntervalTimer.IsStopped())
            {
                stateMachine.ChangeState(StateAttack);
            }
        }

        private void OnHit(HitboxComponent hitbox)
        {
            if (hitThisFrame || stateMachine.GetCurrentState() == State.Dead)
            {
                return;
            }
            if (!trainingMode)
            {
                healthComponent.Damage(1);
            }
            animationPlayer.Play("iframes");
            hitThisFrame = true;
            hitStreamPlayer1.Play();
            hitStreamPlayer2.Play();
            EmitSignal(nameof(DamageTaken));
        }

        private void OnHurtboxHit(HurtboxComponent hurtboxComponent)
        {
            var swordHit = resourcePreloader.InstanceSceneOrNull<SwordHit>();
            GetParent().AddChild(swordHit);
            swordHit.GlobalPosition = hurtboxComponent.GlobalPosition;
            EmitSignal(nameof(EnemyHit));
        }

        private void OnAttackResetTimeout()
        {
            attackChain = 0;
        }

        private void OnDied()
        {
            // QueueFree();
            stateMachine.ChangeState(StateDead);
            EmitSignal(nameof(Died));
        }
    }
}
