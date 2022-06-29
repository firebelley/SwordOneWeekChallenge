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

        [Signal]
        public delegate void RoomComplete();
        [Signal]
        public delegate void RoomFailed();
        [Signal]
        public delegate void SwordHealthChanged(int newHealth);

        [Export]
        private PackedScene level;
        [Export]
        private int maxWaves = 3;
        [Export]
        private int numGhouls;
        [Export]
        private int ghoulsIncrease;
        [Export]
        private int numMaggots;
        [Export]
        private int maggotsIncrease;
        [Export]
        private int numSkulls;

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
        [Node]
        private ScreenBanner deadScreenBanner;
        [Node]
        private ResourcePreloader resourcePreloader;

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

        public void Reset()
        {
            enemyCount = 0;
            currentWave = 0;
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
            for (int i = 0; i < numGhouls + (ghoulsIncrease * (currentWave - 1)); i++)
            {
                var enemy = resourcePreloader.InstanceSceneOrNull<Ghoul>();
                currentLevel.Entities.AddChild(enemy);
                enemy.Connect(nameof(Enemy.Died), this, nameof(OnEnemyDied));
                var tileIndex = MathUtil.RNG.RandiRange(0, currentLevel.FreeTiles.Count - 1);
                var tilePos = currentLevel.FreeTiles[tileIndex];
                enemy.GlobalPosition = (tilePos * 16f) + (Vector2.One * 8f);
                enemyCount++;
            }

            for (int i = 0; i < numMaggots + (maggotsIncrease * (currentWave - 1)); i++)
            {
                var enemy = resourcePreloader.InstanceSceneOrNull<Maggot>();
                currentLevel.Entities.AddChild(enemy);
                enemy.Connect(nameof(Enemy.Died), this, nameof(OnEnemyDied));
                var tileIndex = MathUtil.RNG.RandiRange(0, currentLevel.FreeTiles.Count - 1);
                var tilePos = currentLevel.FreeTiles[tileIndex];
                enemy.GlobalPosition = (tilePos * 16f) + (Vector2.One * 8f);
                enemyCount++;
            }

            for (int i = 0; i < numSkulls; i++)
            {
                var enemy = resourcePreloader.InstanceSceneOrNull<Skull>();
                currentLevel.Entities.AddChild(enemy);
                enemy.Connect(nameof(Enemy.Died), this, nameof(OnEnemyDied));
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
                if (currentWave == maxWaves)
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
            deadScreenBanner.Play();
            GetTree().CreateTimer(3.5f).Connect("timeout", this, nameof(OnDeathTimeout));
        }

        private void OnHealthChanged(int newHealth)
        {
            EmitSignal(nameof(SwordHealthChanged), newHealth);
        }

        private void OnEndTimerTimeout()
        {
            currentLevel.QueueFree();
            EmitSignal(nameof(RoomComplete));
        }

        private void OnWaveIntervalTimerTimeout()
        {
            StartWave();
        }

        private void OnDeathTimeout()
        {
            currentLevel.QueueFree();
            EmitSignal(nameof(RoomFailed));
        }
    }
}
