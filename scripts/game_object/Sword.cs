using Godot;
using GodotUtilities;
using GodotUtilities.Logic;

namespace Game.GameObject
{
    public class Sword : RigidBody2D
    {
        private const float LAUNCH_FORCE = 300f;
        private const int TORQUE_COEFFICIENT = 200_000;

        public override void _Notification(int what)
        {
            if (what == NotificationInstanced)
            {
                this.WireNodes();
            }
        }

        private enum State
        {
            Normal
        }
        private StateMachine<State> stateMachine = new();

        public override void _UnhandledInput(InputEvent evt)
        {
            if (evt.IsActionPressed("launch"))
            {
                GetTree().SetInputAsHandled();
                TryLaunch();
            }
        }

        public override void _Ready()
        {
            stateMachine.AddState(State.Normal, StateNormal);
            stateMachine.SetInitialState(StateNormal);
        }

        public override void _PhysicsProcess(float delta)
        {
            stateMachine.Update();
        }

        private void StateNormal()
        {
            var currentAngle = Vector2.Right.Rotated(Rotation);
            var desiredAngle = this.GetMouseDirection();
            var angleTo = currentAngle.AngleTo(desiredAngle);
            AppliedTorque = angleTo * GetPhysicsProcessDeltaTime() * TORQUE_COEFFICIENT;
        }

        private void TryLaunch()
        {
            if (stateMachine.GetCurrentState() == State.Normal)
            {
                ApplyCentralImpulse(this.GetMouseDirection() * LAUNCH_FORCE);
            }
        }
    }
}
