using Game.GameObject;
using Game.Level;
using Godot;
using GodotUtilities;

namespace Game
{
    public class RunManager : Node
    {
        private const int MAX_WAVES = 3;

        [Export]
        private Godot.Collections.Array<PackedScene> levelPool = new();
        [Export]
        private Godot.Collections.Array<PackedScene> enemyPool = new();

        [Node]
        private Timer waveTimer;

        private BaseLevel currentLevel;
        private int enemyCount;
        private int roomLevel;
        private int currentWave;

        public override void _Notification(int what)
        {
            if (what == NotificationInstanced)
            {
                this.WireNodes();
            }
        }

        public override void _Ready()
        {
            waveTimer.Connect("timeout", this, nameof(OnWaveTimerTimeout));
            BeginNewLevel();
        }

        private void BeginNewLevel()
        {
            roomLevel++;
            currentWave = 0;
            SetupLevel();
            StartWave();
        }

        private void StartWave()
        {
            waveTimer.Start();
            currentWave++;
        }

        private void SetupLevel()
        {
            var levelIndex = MathUtil.RNG.RandiRange(0, levelPool.Count - 1);
            var level = levelPool[levelIndex];
            currentLevel = level.InstanceOrNull<BaseLevel>();
            AddChild(currentLevel);
        }

        private void SpawnEnemies()
        {
            for (int i = 0; i < 3 + currentWave; i++)
            {
                var enemyIndex = MathUtil.RNG.RandiRange(0, levelPool.Count - 1);
                var enemy = enemyPool[enemyIndex].InstanceOrNull<Ghoul>();
                currentLevel.Entities.AddChild(enemy);
                enemy.Connect(nameof(Ghoul.Died), this, nameof(OnEnemyDied));

                var tileIndex = MathUtil.RNG.RandiRange(0, currentLevel.FreeTiles.Count - 1);
                var tilePos = currentLevel.FreeTiles[tileIndex];
                enemy.GlobalPosition = (tilePos * 16f) + (Vector2.One * 8f);
                enemyCount++;
            }
        }

        private void OnWaveTimerTimeout()
        {
            SpawnEnemies();
        }

        private void OnEnemyDied()
        {
            enemyCount--;
            if (enemyCount <= 0)
            {
                if (currentWave == MAX_WAVES)
                {
                    BeginNewLevel();
                }
                else
                {
                    StartWave();
                }
            }
        }
    }
}
