using Game.Component;
using Game.GameObject;
using Game.UI;
using Godot;
using GodotUtilities;

namespace Game.Level
{
    public class EndlessLevel : BaseLevel
    {
        private const int MAX_ENEMIES = 8;

        [Node]
        private ScreenBanner incomingScreenBanner;
        [Node]
        private ScreenBanner deadScreenBanner;
        [Node]
        private Timer waveIntervalTimer;
        [Node]
        private Timer waveTimer;
        [Node]
        private Timer spawnIntervalTimer;
        [Node]
        private Timer deathTimer;
        [Node]
        private ResourcePreloader resourcePreloader;

        private int currentWave = 0;
        private int enemiesToSpawn;
        private int killedEnemies;

        public override void _Notification(int what)
        {
            base._Notification(what);
            if (what == NotificationInstanced)
            {
                this.WireNodes();
            }
        }

        public override void _Ready()
        {
            base._Ready();
            waveIntervalTimer.Start();
            waveIntervalTimer.Connect("timeout", this, nameof(OnWaveIntervalTimerTimeout));
            waveTimer.Connect("timeout", this, nameof(OnWaveTimerTimeout));
            spawnIntervalTimer.Connect("timeout", this, nameof(OnSpawnIntervalTimerTimeout));
            deathTimer.Connect("timeout", this, nameof(OnDeathTimerTimeout));
            GetNode("%Sword").Connect(nameof(Sword.Died), this, nameof(OnSwordDied));
        }

        public void BeginWave()
        {
            GetNode("%Sword")?.GetFirstNodeOfType<HealthComponent>()?.Heal(100);
            killedEnemies = 0;
            currentWave++;
            incomingScreenBanner.Text = $"Wave {currentWave}";
            incomingScreenBanner.Play();
            waveTimer.Start();
        }

        private bool IsBossWave()
        {
            return (currentWave % 5) == 0;
        }

        private void OnWaveIntervalTimerTimeout()
        {
            BeginWave();
        }

        private void OnSpawnIntervalTimerTimeout()
        {
            if (IsBossWave())
            {
                var enemy = resourcePreloader.InstanceSceneOrNull<Skull>();
                Entities.AddChild(enemy);
                enemy.GlobalPosition = GetFreePosition();
                enemy.Connect(nameof(Enemy.Died), this, nameof(OnEnemyDied));
            }
            else
            {
                Enemy enemy;
                if (MathUtil.RNG.Randf() < .33f)
                {
                    enemy = resourcePreloader.InstanceSceneOrNull<Maggot>();
                }
                else
                {
                    enemy = resourcePreloader.InstanceSceneOrNull<Ghoul>();
                }
                Entities.AddChild(enemy);
                enemy.GlobalPosition = GetFreePosition();
                enemy.Connect(nameof(Enemy.Died), this, nameof(OnEnemyDied));
                enemiesToSpawn--;
                if (enemiesToSpawn > 0)
                {
                    spawnIntervalTimer.Start();
                }
            }
        }

        private void OnWaveTimerTimeout()
        {
            enemiesToSpawn = MAX_ENEMIES;
            OnSpawnIntervalTimerTimeout();
        }

        private void OnEnemyDied()
        {
            killedEnemies++;
            if (killedEnemies == MAX_ENEMIES || (IsBossWave() && killedEnemies == 1))
            {
                waveIntervalTimer.Start();
            }
        }

        private void OnDeathTimerTimeout()
        {
            //transition
        }

        private void OnSwordDied()
        {
            deathTimer.Start();
            deadScreenBanner.Play();
        }
    }
}
