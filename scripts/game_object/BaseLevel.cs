using Game.Autoload;
using Game.UI;
using Godot;
using GodotUtilities;

namespace Game.GameObject
{
    public class BaseLevel : Node
    {
        public override void _Notification(int what)
        {
            if (what == NotificationInstanced)
            {
                this.WireNodes();
            }
        }

        public override void _Ready()
        {
            var sword = GetNode<Sword>("%Sword");
            this.GetFirstNodeOfType<GameUI>().ConnectSword(sword);
            sword.Connect(nameof(Sword.EnemyHit), this, nameof(OnEnemyHit));
        }

        private void OnEnemyHit()
        {
            HitstopManager.HitStop();
            this.GetFirstNodeOfType<GameCamera>()?.Shake();
        }
    }
}
