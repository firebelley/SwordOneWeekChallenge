using Godot;
using GodotUtilities;

namespace Game.Effect
{
    public class EnemyDeathExplosion : Node2D
    {
        [Node]
        private ResourcePreloader resourcePreloader;

        public override void _Notification(int what)
        {
            if (what == NotificationInstanced)
            {
                this.WireNodes();
            }
        }

        public override void _Ready()
        {
            CallDeferred(nameof(SpawnTrails));
        }

        private void SpawnTrails()
        {
            var count = MathUtil.RNG.RandiRange(1, 3);
            for (int i = 0; i < count; i++)
            {
                var node = resourcePreloader.InstanceSceneOrNull<EnemyDeathTrail>();
                GetParent().AddChild(node);
                node.GlobalPosition = GlobalPosition;
            }
        }
    }
}
