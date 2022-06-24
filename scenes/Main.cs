using Game.GameObject;
using Game.UI;
using Godot;
using GodotUtilities;

namespace Game
{
    public class Main : Node
    {

        public override void _Ready()
        {
            this.GetFirstNodeOfType<GameUI>().ConnectSword(GetNode<Sword>("%Sword"));
        }
    }
}
