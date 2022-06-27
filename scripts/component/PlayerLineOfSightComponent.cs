using Game.GameObject;
using Godot;
using GodotUtilities;

namespace Game.Component
{
    public class PlayerLineOfSightComponent : RayCast2D
    {
        public bool HasLineOfSight()
        {
            var targetPos = GetTree().GetFirstNodeInGroup<Sword>()?.CenterMass;
            if (targetPos == null)
            {
                return false;
            }

            var raycast = targetPos.Value - GlobalPosition;
            CastTo = raycast;
            ForceRaycastUpdate();
            return !IsColliding();
        }
    }
}
