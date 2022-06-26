using Game.Data.Perk;
using Godot;
using GodotUtilities;

namespace Game.UI
{
    public class PerkChoiceButton : Button
    {
        public override void _Notification(int what)
        {
            if (what == NotificationInstanced)
            {
                this.WireNodes();
            }
        }

        public void SetPerkType(PerkType perkType)
        {
            if (perkType == PerkType.Health)
            {
                Text = "+1 HP";
            }
        }
    }
}
