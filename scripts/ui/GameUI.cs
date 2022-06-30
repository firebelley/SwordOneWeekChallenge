using Game.Component;
using Game.GameObject;
using Godot;
using GodotUtilities;

namespace Game.UI
{
    public class GameUI : CanvasLayer
    {
        [Node("%HealthBarComponent")]
        private HealthBarComponent healthBarComponent;

        public override void _Notification(int what)
        {
            if (what == NotificationInstanced)
            {
                this.WireNodes();
            }
        }

        public void ConnectSword(Sword sword)
        {
            healthBarComponent.SetHealthComponent(sword.HealthComponent);
        }
    }
}
