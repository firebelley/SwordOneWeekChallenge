using Game.Component;
using Game.Effect;
using Game.Level;
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
        private const float MAX_SUMMON_WAVES = 3;

        [Node]
        private AnimationPlayer animationPlayer;
        [Node]
        private AnimationPlayer blinkAnimationPlayer;
        [Node]
        private ResourcePreloader resourcePreloader;
        [Node]
        private VelocityComponent velocityComponent;
        [Node]
        private PathfindComponent pathfindComponent;
        [Node]
        private RandomTimerComponent dashIntervalTimer;
        [Node]
        private Timer dashDurationTimer;
        [Node]
        private RandomTimerComponent bulletIntervalTimer;
        [Node]
        private Timer attackChargeTimer;
        [Node]
        private Timer bulletAttackTimer;
        [Node]
        private Timer summonAttackTimer;
        [Node]
        private RandomTimerComponent summonIntervalTimer;
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
            SummonCharge,
            Summon
        }

        private StateMachine<State> stateMachine = new();

        private int currentBulletWaves;
        private int currentSummonWaves;
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
            stateMachine.AddEnterState(State.SummonCharge, EnterStateSummonCharge);
            stateMachine.AddState(State.SummonCharge, StateSummonCharge);
            stateMachine.AddEnterState(State.Summon, EnterStateSummon);
            stateMachine.AddState(State.Summon, StateSummon);
            stateMachine.AddLeaveState(State.Summon, LeaveStateSummon);

            stateMachine.SetInitialState(State.Normal);
            dashIntervalTimer.Start();
            bulletIntervalTimer.Start();
            summonIntervalTimer.Start();
        }

        public override void _PhysicsProcess(float delta)
        {
            stateMachine.Update();
            velocityComponent.MoveAndSlide();
        }

        private void StateNormal()
        {
            var player = GetTree().GetFirstNodeInGroup<Sword>();
            var targetPos = player?.CenterMass ?? GlobalPosition;

            if (player != null && targetPos.DistanceSquaredTo(GlobalPosition) < DASH_RANGE * DASH_RANGE && dashIntervalTimer.IsStopped())
            {
                stateMachine.ChangeState(StateDash);
            }
            else if (summonIntervalTimer.IsStopped())
            {
                stateMachine.ChangeState(StateSummonCharge);
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
            attackChargeTimer.Start();
            var eyeNode = alternateBulletAttack ? eyeRightPosition : eyeLeftPosition;
            var attackCharge = resourcePreloader.InstanceSceneOrNull<AttackCharge>();
            eyeNode.AddChild(attackCharge);
            attackCharge.SetDuration(1f / attackChargeTimer.WaitTime);
        }

        private void StateBulletAttackCharge()
        {
            PlayShakeAnimation();
            if (attackChargeTimer.IsStopped())
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
            PlayShakeAnimation();
            if (bulletAttackTimer.IsStopped())
            {
                if (!alternateBulletAttack)
                {
                    var direction = ((GetTree().GetFirstNodeInGroup<Sword>()?.CenterMass ?? Vector2.Right) - GlobalPosition).Normalized();
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

        private void EnterStateSummonCharge()
        {
            var eyes = new Node2D[] {
                eyeLeftPosition,
                eyeRightPosition
            };
            foreach (var eye in eyes)
            {
                var attackCharge = resourcePreloader.InstanceSceneOrNull<AttackCharge>();
                eye.AddChild(attackCharge);
                attackCharge.SetDuration(1f / attackChargeTimer.WaitTime);
            }
            attackChargeTimer.Start();
        }

        private void StateSummonCharge()
        {
            PlayShakeAnimation();
            if (attackChargeTimer.IsStopped())
            {
                stateMachine.ChangeState(StateSummon);
            }
            velocityComponent.AccelerateToVelocity(Vector2.Zero);
        }

        private void EnterStateSummon()
        {
            currentSummonWaves = 0;
            summonAttackTimer.Start();
        }

        private void StateSummon()
        {
            PlayShakeAnimation();
            if (summonAttackTimer.IsStopped())
            {
                Enemy enemy;
                if (currentSummonWaves < MAX_SUMMON_WAVES - 1)
                {
                    enemy = resourcePreloader.InstanceSceneOrNull<Ghoul>();
                }
                else
                {
                    enemy = resourcePreloader.InstanceSceneOrNull<Maggot>();
                }
                GetParent().AddChild(enemy);

                var freeTiles = this.GetAncestor<BaseLevel>()?.FreeTiles ?? new() { Vector2.Zero };
                var position = freeTiles[MathUtil.RNG.RandiRange(0, freeTiles.Count - 1)];
                enemy.GlobalPosition = (position * 16f) + (Vector2.One * 8f);

                currentSummonWaves++;
                if (currentSummonWaves == MAX_SUMMON_WAVES)
                {
                    stateMachine.ChangeState(StateNormal);
                }
                else
                {
                    summonAttackTimer.Start();
                }
            }
            velocityComponent.AccelerateToVelocity(Vector2.Zero);
        }

        private void LeaveStateSummon()
        {
            summonIntervalTimer.Start();
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

        private static class Constants
        {
            public static readonly string V_DASH_DIRECTION = "v_dash_direction";
        }
    }
}
