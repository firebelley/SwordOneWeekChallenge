using Game.UI;
using Godot;

namespace Game.Level
{
    public class TrainingRoom : BaseLevel
    {
        public override void _UnhandledInput(InputEvent evt)
        {
            base._UnhandledInput(evt);
            if (evt.IsActionPressed("pause"))
            {
                GetTree().SetInputAsHandled();
                var pauseMenu = GD.Load<PackedScene>("res://scenes/ui/PauseMenu.tscn").InstanceOrNull<PauseMenu>();
                AddChild(pauseMenu);
                pauseMenu.HideLevelSelect();
            }
        }
    }
}
