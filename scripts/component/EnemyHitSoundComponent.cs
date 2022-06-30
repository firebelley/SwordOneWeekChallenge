using Godot;
using GodotUtilities;

namespace Game.Component
{
    public class EnemyHitSoundComponent : Node
    {
        [Export]
        private NodePath hurtboxComponentPath;

        [Node]
        private RandomAudioStreamPlayerComponent randomAudioStreamPlayerComponent;
        [Node]
        private RandomAudioStreamPlayerComponent randomAudioStreamPlayerComponent2;
        [Node]
        private RandomAudioStreamPlayerComponent randomAudioStreamPlayerComponent3;
        [Node]
        private RandomAudioStreamPlayerComponent randomAudioStreamPlayerComponent4;

        public override void _Notification(int what)
        {
            if (what == NotificationInstanced)
            {
                this.WireNodes();
            }
        }

        public override void _Ready()
        {
            this.GetNullableNodePath<HurtboxComponent>(hurtboxComponentPath)?.Connect(nameof(HurtboxComponent.Hit), this, nameof(OnHit));
        }

        private void OnHit(object _)
        {
            randomAudioStreamPlayerComponent.Play();
            randomAudioStreamPlayerComponent2.Play();
            randomAudioStreamPlayerComponent3.Play();
            randomAudioStreamPlayerComponent4.Play();
        }
    }
}
