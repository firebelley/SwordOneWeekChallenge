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
        private TileMap tileMap;
        [Node("%Entities")]
        public Node2D Entities { get; private set; }

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

            RecordFreeTiles();
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

        private void OnEnemyHit()
        {
            HitstopManager.HitStop();
            this.GetFirstNodeOfType<GameCamera>()?.Shake();
        }
    }
}