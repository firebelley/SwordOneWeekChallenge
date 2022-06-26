using System.Collections.Generic;
using Game.Data.Perk;
using Godot;
using GodotUtilities;

namespace Game.UI
{
    public class PerkChoice : CanvasLayer
    {
        [Signal]
        public delegate void PerkSelected(PerkType perkType);

        [Node]
        private ResourcePreloader resourcePreloader;
        [Node("%ButtonContainer")]
        private HBoxContainer buttonContainer;

        public override void _Notification(int what)
        {
            if (what == NotificationInstanced)
            {
                this.WireNodes();
            }
        }

        public void SetChoices(List<PerkType> choices)
        {
            foreach (var choice in choices)
            {
                var button = resourcePreloader.InstanceSceneOrNull<PerkChoiceButton>();
                buttonContainer.AddChild(button);
                button.SetPerkType(choice);
                button.Connect("pressed", this, nameof(OnPerkSelected), new Godot.Collections.Array { choice });
            }
        }

        private void OnPerkSelected(PerkType perk)
        {
            EmitSignal(nameof(PerkSelected), perk);
        }
    }
}
