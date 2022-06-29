using Game.Component;
using Game.Effect;
using Godot;
using GodotUtilities;

namespace Game.GameObject
{
    public class Projectile : KinematicBody2D
    {
        private const float SPEED = 75f;

        [Node]
        private Timer effectSpawnTimer;
        [Node]
        private ResourcePreloader resourcePreloader;
        [Node]
        private AnimationPlayer animationPlayer;
        [Node]
        private HitboxComponent hitboxComponent;

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
            effectSpawnTimer.Connect("timeout", this, nameof(OnEffectSpawnTimerTimeout));
            hitboxComponent.Connect(nameof(HitboxComponent.HitHurtbox), this, nameof(OnHurtboxHit));
            SetEffectSpawnTimer();
        }

        public override void _PhysicsProcess(float delta)
        {
            var collision = MoveAndCollide(velocity * delta);
            if (collision != null)
            {
                animationPlayer.Play("die");
            }
        }

        public void Kill()
        {
            animationPlayer.Play("die");
        }

        public void SetDirection(Vector2 direction)
        {
            velocity = direction.Normalized() * SPEED;
        }

        private void SetEffectSpawnTimer()
        {
            effectSpawnTimer.WaitTime = MathUtil.RNG.RandfRange(.4f, .8f);
            effectSpawnTimer.Start();
        }

        private void OnEffectSpawnTimerTimeout()
        {
            // TODO: have globally accessible effect layer
            var effect = resourcePreloader.InstanceSceneOrNull<MagmaEffect>();
            GetParent().AddChild(effect);
            effect.GlobalPosition = GlobalPosition;
            SetEffectSpawnTimer();
        }

        private void OnHurtboxHit(object _)
        {
            Kill();
        }
    }
}
