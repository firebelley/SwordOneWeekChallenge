using Game.Component;
using Game.Data;
using Game.GameObject;
using Game.Level;
using Game.UI;
using Godot;
using GodotUtilities;

namespace Game
{
    public class RoomManager : Node
    {
        private const int MAX_WAVES = 3;

        [Signal]
        public delegate void RoomComplete();
        [Signal]
        public delegate void RoomFailed();
        [Signal]
        public delegate void SwordHealthChanged(int newHealth);

        [Export]
        private Godot.Collections.Array<PackedScene> levelPool = new();
        [Export]
        private Godot.Collections.Array<PackedScene> enemyPool = new();

        [Node]
        private Timer waveTimer;
        [Node]
        private Timer endTimer;
        [Node]
        private Timer waveIntervalTimer;
        [Node]
        private ScreenBanner incomingScreenBanner;
        [Node]
        private ScreenBanner completeScreenBanner;

        private BaseLevel currentLevel;
        private int enemyCount;
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
            endTimer.Connect("timeout", this, nameof(OnEndTimerTimeout));
            waveIntervalTimer.Connect("timeout", this, nameof(OnWaveIntervalTimerTimeout));
        }

        public void StartRoom(RunConfig runConfig, RoomConfig roomConfig)
        {
            BeginNewLevel();
            SetupSword(runConfig);
        }

        private void BeginNewLevel()
        {
            SetupLevel();
            waveIntervalTimer.Start();
        }

        private void StartWave()
        {
            incomingScreenBanner.Play();
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

        private void SetupSword(RunConfig runConfig)
        {
            var sword = currentLevel.GetNode<Sword>("%Sword");
            sword?.HealthComponent.SetMaxHealth(runConfig.MaxHealth);
            sword?.HealthComponent.SetCurrentHealth(runConfig.CurrentHealth);
            sword?.HealthComponent.Connect(nameof(HealthComponent.HealthChanged), this, nameof(OnHealthChanged));
            sword?.Connect(nameof(Sword.Died), this, nameof(OnSwordDied));
        }

        private void SpawnEnemies()
        {
            for (int i = 0; i < 2 + currentWave; i++)
            {
                var enemyIndex = MathUtil.RNG.RandiRange(0, enemyPool.Count - 1);
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
                    completeScreenBanner.Play();
                    endTimer.Start();
                }
                else
                {
                    waveIntervalTimer.Start();
                }
            }
        }

        private void OnSwordDied()
        {
            // TODO: do some stuff here like delay
            EmitSignal(nameof(RoomFailed));
        }

        private void OnHealthChanged(int newHealth)
        {
            EmitSignal(nameof(SwordHealthChanged), newHealth);
        }

        private void OnEndTimerTimeout()
        {
            EmitSignal(nameof(RoomComplete));
        }

        private void OnWaveIntervalTimerTimeout()
        {
            StartWave();
        }
    }
}
