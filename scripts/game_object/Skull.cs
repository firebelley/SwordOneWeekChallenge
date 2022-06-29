using Game.Component;
using Game.Effect;
using Godot;
using GodotUtilities;
using GodotUtilities.Logic;

namespace Game.GameObject
{
    public class Skull : Enemy
    {
        private const float DASH_RANGE = 64f;
        private const float DASH_SPEED = 250f;
        private const float SPACING = 32f;
        private const float MAX_BULLET_WAVES = 2;

        [Node]
        private ResourcePreloader resourcePreloader;
        [Node]
        private VelocityComponent velocityComponent;
        [Node]
        private PathfindComponent pathfindComponent;
        [Node]
        private Timer dashIntervalTimer;
        [Node]
        private Timer dashDurationTimer;
        [Node]
        private Timer bulletIntervalTimer;
        [Node]
        private Timer bulletChargeTimer;
        [Node]
        private Timer bulletAttackTimer;
        [Node]
        private BlackboardComponent blackboardComponent;
        [Node]
        private Position2D eyeLeftPosition;
        [Node]
        private Position2D eyeRightPosition;

        private enum State
        {
            Normal,
            Dash,
            BulletAttackCharge,
            BulletAttack,
        }

        private StateMachine<State> stateMachine = new();

        private int currentBulletWaves;
        private bool alternateBulletAttack;

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
            stateMachine.AddEnterState(State.Dash, EnterStateDash);
            stateMachine.AddState(State.Dash, StateDash);
            stateMachine.AddLeaveState(State.Dash, LeaveStateDash);
            stateMachine.AddEnterState(State.BulletAttackCharge, EnterStateBulletAttackCharge);
            stateMachine.AddState(State.BulletAttackCharge, StateBulletAttackCharge);
            stateMachine.AddLeaveState(State.BulletAttackCharge, LeaveStateBulletAttackCharge);
            stateMachine.AddEnterState(State.BulletAttack, EnterStateBulletAttack);
            stateMachine.AddState(State.BulletAttack, StateBulletAttack);
            stateMachine.AddLeaveState(State.BulletAttack, LeaveStateBulletAttack);

            stateMachine.SetInitialState(State.Normal);
        }

        public override void _PhysicsProcess(float delta)
        {
            stateMachine.Update();
            velocityComponent.MoveAndSlide();
        }


        private void StateNormal()
        {
            var player = GetTree().GetFirstNodeInGroup<Sword>();
            var targetPos = player?.GlobalPosition ?? GlobalPosition;

            if (player != null && targetPos.DistanceSquaredTo(GlobalPosition) < DASH_RANGE * DASH_RANGE && dashIntervalTimer.IsStopped())
            {
                stateMachine.ChangeState(StateDash);
            }
            else if (bulletIntervalTimer.IsStopped())
            {
                stateMachine.ChangeState(StateBulletAttackCharge);
            }
            else
            {
                pathfindComponent.SetTargetInterval(targetPos + (Vector2.Right.Rotated(MathUtil.RNG.RandfRange(0f, Mathf.Tau)) * SPACING), 2f);
                velocityComponent.AccelerateToVelocity(velocityComponent.GetTargetVelocity(pathfindComponent));
            }
        }

        private void EnterStateDash()
        {
            var direction = Vector2.Right.Rotated(MathUtil.RNG.RandfRange(0f, Mathf.Tau));
            blackboardComponent.SetValue(Constants.V_DASH_DIRECTION, direction);
            dashDurationTimer.Start();
        }

        private void StateDash()
        {
            velocityComponent.AccelerateToVelocity((blackboardComponent.GetPrimitiveValue<Vector2>(Constants.V_DASH_DIRECTION) ?? Vector2.Right) * DASH_SPEED);
            if (dashDurationTimer.IsStopped())
            {
                stateMachine.ChangeState(StateNormal);
            }
        }

        private void LeaveStateDash()
        {
            dashIntervalTimer.Start();
        }

        private void EnterStateBulletAttackCharge()
        {
            bulletChargeTimer.Start();
            var eyeNode = alternateBulletAttack ? eyeRightPosition : eyeLeftPosition;
            var attackCharge = resourcePreloader.InstanceSceneOrNull<AttackCharge>();
            eyeNode.AddChild(attackCharge);
            attackCharge.SetDuration(1f / bulletChargeTimer.WaitTime);
        }

        private void StateBulletAttackCharge()
        {
            if (bulletChargeTimer.IsStopped())
            {
                stateMachine.ChangeState(StateBulletAttack);
            }
            velocityComponent.AccelerateToVelocity(Vector2.Zero);
        }

        private void LeaveStateBulletAttackCharge()
        {
            bulletIntervalTimer.Start();
        }

        private void EnterStateBulletAttack()
        {
            currentBulletWaves = 0;
        }

        private void StateBulletAttack()
        {
            if (bulletAttackTimer.IsStopped())
            {
                if (!alternateBulletAttack)
                {
                    var direction = ((GetTree().GetFirstNodeInGroup<Sword>()?.GlobalPosition ?? Vector2.Right) - GlobalPosition).Normalized();
                    for (int i = 0; i < 3; i++)
                    {
                        var projectile = resourcePreloader.InstanceSceneOrNull<Projectile>();
                        GetParent().AddChild(projectile);
                        projectile.GlobalPosition = eyeLeftPosition.GlobalPosition;
                        projectile.SetDirection(direction.Rotated(Mathf.Deg2Rad(-30f + (30f * i))));
                    }
                }
                else
                {
                    for (int i = 0; i < 9; i++)
                    {
                        var projectile = resourcePreloader.InstanceSceneOrNull<Projectile>();
                        GetParent().AddChild(projectile);
                        projectile.GlobalPosition = eyeRightPosition.GlobalPosition;
                        projectile.SetDirection(Vector2.Right.Rotated(Mathf.Deg2Rad(-360f + (currentBulletWaves * 20) + (40f * i))));
                    }
                }
                bulletAttackTimer.Start();
                currentBulletWaves++;
            }
            if (currentBulletWaves == MAX_BULLET_WAVES)
            {
                stateMachine.ChangeState(StateNormal);
            }
            velocityComponent.AccelerateToVelocity(Vector2.Zero);
        }

        private void LeaveStateBulletAttack()
        {
            alternateBulletAttack = !alternateBulletAttack;
        }

        private static class Constants
        {
            public static readonly string V_DASH_DIRECTION = "v_dash_direction";
        }
    }
}
