using Game.Component;
using Godot;
using GodotUtilities;
using GodotUtilities.Logic;

namespace Game.GameObject
{
    public class Sword : RigidBody2D
    {
        [Signal]
        public delegate void LaunchTimerStarted(float time);
        [Signal]
        public delegate void DashTimerStarted(float time);
        [Signal]
        public delegate void LaunchTimerStopped();
        [Signal]
        public delegate void DashTimerStopped();
        [Signal]
        public delegate void EnemyHit();

        [Node]
        private Timer launchTimer;
        [Node]
        private Timer dashTimer;
        [Node]
        private Timer attackIntervalTimer;
        [Node]
        private Timer attackStateTimer;
        [Node]
        private Timer attackResetTimer;
        [Node]
        private Sprite sprite;
        [Node]
        private AnimatedSprite animatedSpriteAttack1;
        [Node]
        private AnimatedSprite animatedSpriteAttack2;
        [Node]
        private AnimatedSprite animatedSpriteAttack3;
        [Node]
        private ResourcePreloader resourcePreloader;
        [Node]
        private Position2D tip;
        [Node]
        private HurtboxComponent hurtboxComponent;

        private Vector2 previousPosition;

        private const float LAUNCH_FORCE = 300f;
        private const int TORQUE_COEFFICIENT = 200_000;
        private const float DASH_FORCE = 1500f;
        private const float ATTACK_FORCE = 300f;
        private const float DASH_LINEAR_DAMP = 8f;
        private const float ATTACK_LINEAR_DAMP = 8f;

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
            Attack
        }
        private StateMachine<State> stateMachine = new();

        private int attackChain;
        private float attackTime;

        public override void _UnhandledInput(InputEvent evt)
        {
            if (evt.IsActionPressed("launch"))
            {
                GetTree().SetInputAsHandled();
                TryLaunch();
            }
            else if (evt.IsActionPressed("dash"))
            {
                GetTree().SetInputAsHandled();
                TryDash();
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
            stateMachine.SetInitialState(StateNormal);

            animatedSpriteAttack1.Visible = false;
            animatedSpriteAttack2.Visible = false;

            attackTime = attackIntervalTimer.WaitTime;

            hurtboxComponent.Connect(nameof(HurtboxComponent.Hit), this, nameof(OnHit));
            attackResetTimer.Connect("timeout", this, nameof(OnAttackResetTimeout));
        }

        public override void _PhysicsProcess(float delta)
        {
            stateMachine.Update();
            sprite.FlipV = !((RotationDegrees is > -90 and < 0) || (RotationDegrees is < 90 and > 0));
            animatedSpriteAttack1.FlipV = sprite.FlipV;
            animatedSpriteAttack2.FlipV = !sprite.FlipV;
            animatedSpriteAttack3.FlipV = sprite.FlipV;
            previousPosition = GlobalPosition;
        }

        private void StateNormal()
        {
            ApplyTorqueTowardMouse();
            GravityScale = LinearVelocity.y < 0 ? 1.5f : 1f;

            if (Input.IsActionPressed("attack"))
            {
                TryAttack();
            }
        }

        private void EnterStateDash()
        {
            dashTimer.Start();
            ApplyCentralImpulse(this.GetMouseDirection() * DASH_FORCE);
            EmitSignal(nameof(DashTimerStarted), dashTimer.WaitTime);

            GravityScale = 0;
            LinearDamp = DASH_LINEAR_DAMP;
            Rotation = this.GetMouseDirection().Angle();
            AppliedTorque = 0f;
        }

        private void StateDash()
        {
            if (CheckInsideTerrain())
            {
                stateMachine.ChangeState(StateNormal);
            }

            if (dashTimer.TimeLeft < .75f)
            {
                stateMachine.ChangeState(StateNormal);
            }
        }

        private void LeaveStateDash()
        {
            GravityScale = 1;
            LinearDamp = 0;
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

            var hitbox = resourcePreloader.InstanceSceneOrNull<HitboxComponent>("SwordHitbox");
            GetParent().AddChild(hitbox);
            if (attackChain == 2)
            {
                hitbox.GetNode<CollisionShape2D>("BoxShape").Disabled = true;
                hitbox.GetNode<CollisionShape2D>("CircleShape").Disabled = false;
            }
            hitbox.GlobalPosition = GlobalPosition;
            hitbox.Rotation = this.GetMouseDirection().Angle();
            hitbox.Connect(nameof(HitboxComponent.HitHurtbox), this, nameof(OnHurtboxHit));

            attackChain++;
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
            return false;
        }

        private void TryLaunch()
        {
            if (stateMachine.GetCurrentState() == State.Normal && launchTimer.IsStopped())
            {
                LinearVelocity = Vector2.Zero;
                ApplyCentralImpulse(Vector2.Up * LAUNCH_FORCE);
                launchTimer.Start();
                EmitSignal(nameof(LaunchTimerStarted), launchTimer.WaitTime);
            }
        }

        private void TryDash()
        {
            if ((stateMachine.GetCurrentState() == State.Normal || stateMachine.GetCurrentState() == State.Attack) && dashTimer.IsStopped())
            {
                stateMachine.ChangeState(StateDash);
            }
        }

        private void TryAttack()
        {
            if ((stateMachine.GetCurrentState() == State.Normal || stateMachine.GetCurrentState() == State.Dash) && attackIntervalTimer.IsStopped())
            {
                stateMachine.ChangeState(StateAttack);
            }
        }

        private void OnHit(HitboxComponent hitbox)
        {
            GD.Print("hit");
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
    }
}
