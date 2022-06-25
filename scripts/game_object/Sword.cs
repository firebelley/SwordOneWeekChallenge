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

        [Node]
        private Timer launchTimer;
        [Node]
        private Timer dashTimer;
        [Node]
        private Timer attackTimer;
        [Node]
        private Sprite sprite;
        [Node]
        private AnimatedSprite animatedSprite;
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
        private const float ATTACK_FORCE = 100f;
        private const float DASH_LINEAR_DAMP = 8f;

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
        private SceneTreeTween currentAttackTween;

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
            else if (evt.IsActionPressed("attack"))
            {
                GetTree().SetInputAsHandled();
                TryAttack();
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

            animatedSprite.Visible = false;

            hurtboxComponent.Connect(nameof(HurtboxComponent.Hit), this, nameof(OnHit));
        }

        public override void _PhysicsProcess(float delta)
        {
            stateMachine.Update();
            sprite.FlipV = !((RotationDegrees is > -90 and < 0) || (RotationDegrees is < 90 and > 0));
            animatedSprite.FlipV = sprite.FlipV;
            previousPosition = GlobalPosition;
        }

        private void StateNormal()
        {
            ApplyTorqueTowardMouse();
            GravityScale = LinearVelocity.y < 0 ? 1.5f : 1f;
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
            attackTimer.Start();
            GravityScale = 0;
            LinearVelocity = Vector2.Zero;
            ApplyCentralImpulse(this.GetMouseDirection() * ATTACK_FORCE);
            AppliedTorque = 0f;
            // animatedSprite.GlobalRotation = this.GetMouseDirection().Angle();
            Rotation = this.GetMouseDirection().Angle();

            if (IsInstanceValid(currentAttackTween))
            {
                currentAttackTween.Kill();
            }

            sprite.Visible = false;
            animatedSprite.Visible = true;
            animatedSprite.Play("attack_1");
            animatedSprite.Frame = 0;

            var hitbox = resourcePreloader.InstanceSceneOrNull<HitboxComponent>("SwordHitbox");
            GetParent().AddChild(hitbox);
            hitbox.GlobalPosition = GlobalPosition;
            hitbox.Rotation = this.GetMouseDirection().Angle();
            hitbox.Connect(nameof(HitboxComponent.HitHurtbox), this, nameof(OnHurtboxHit));
        }

        private void StateAttack()
        {
            CheckInsideTerrain();
            ApplyTorqueTowardMouse();
            if (attackTimer.IsStopped())
            {
                stateMachine.ChangeState(StateNormal);
            }
        }

        private void LeaveStateAttack()
        {
            GravityScale = 1f;
            sprite.Visible = true;
            animatedSprite.Visible = false;
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
            if (stateMachine.GetCurrentState() == State.Normal && dashTimer.IsStopped())
            {
                stateMachine.ChangeState(StateDash);
            }
        }

        private void TryAttack()
        {
            if (stateMachine.GetCurrentState() == State.Normal && attackTimer.IsStopped())
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
            // TODO: move this out of sword, perhaps to autoload
            GetTree().Paused = true;
            GetTree().CreateTimer(.06f, true).Connect("timeout", this, nameof(ResetTimeScale));
        }

        private void ResetTimeScale()
        {
            GetTree().Paused = false;
        }
    }
}
