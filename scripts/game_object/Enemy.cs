using Godot;

namespace Game.GameObject
{
    public abstract class Enemy : KinematicBody2D
    {
        [Signal]
        public delegate void Died();
    }
}
