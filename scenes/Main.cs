using Game.GameObject;
using Godot;
using GodotUtilities;

namespace Game
{
    public class Main : Node
    {
        public override void _Ready()
        {
            this.GetFirstNodeOfType<GameUI>().ConnectSword(this.GetFirstNodeOfType<Sword>());
        }
    }
}
