using System.Collections.Generic;
using Game.Autoload;
using Game.GameObject;
using Game.UI;
using Godot;
using GodotUtilities;

namespace Game.Level
{
    public class BaseLevel : Node
    {
        public List<Vector2> FreeTiles { get; private set; } = new();

        [Node("%TileMap")]
        protected TileMap tileMap;
        [Node("%Entities")]
        public Node2D Entities;

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
            sword.Connect(nameof(Sword.DamageTaken), this, nameof(OnDamageTaken));

            RecordFreeTiles();
        }


        public Vector2 GetFreePosition()
        {
            var position = FreeTiles[MathUtil.RNG.RandiRange(0, FreeTiles.Count - 1)];
            return (position * 16f) + (Vector2.One * 8f);
        }

        private void RecordFreeTiles()
        {
            for (int x = -20; x < 20; x++)
            {
                for (int y = -11; y < 11; y++)
                {
                    if (tileMap.GetCell(x, y) == 1)
                    {
                        FreeTiles.Add(new Vector2(x, y));
                    }
                }
            }

        }

        private void DoHitstop()
        {
            HitstopManager.Hitstop();
            this.GetFirstNodeOfType<GameCamera>()?.Shake();
        }

        private void OnEnemyHit()
        {
            DoHitstop();
        }

        private void OnDamageTaken()
        {
            DoHitstop();
        }
    }
}
