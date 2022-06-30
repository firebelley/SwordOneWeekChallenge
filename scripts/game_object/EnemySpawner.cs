using Godot;
using GodotUtilities;

namespace Game.GameObject
{
    public class EnemySpawner : Node2D
    {
        [Export]
        private PackedScene enemyScene;

        [Node]
        private Timer timer;

        private Enemy currentEnemy;

        public override void _Notification(int what)
        {
            if (what == NotificationInstanced)
            {
                this.WireNodes();
            }
        }

        public override void _Ready()
        {
            timer.Connect("timeout", this, nameof(OnTimerTimeout));
        }

        private void OnTimerTimeout()
        {
            if (!IsInstanceValid(currentEnemy))
            {
                var enemy = enemyScene.InstanceOrNull<Enemy>();
                GetParent().AddChild(enemy);
                enemy.GlobalPosition = GlobalPosition;
                currentEnemy = enemy;
            }
        }
    }
}
