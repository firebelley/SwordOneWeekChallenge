using Game.Data;
using Godot;
using GodotUtilities;

namespace Game.UI
{
    public class LevelSelector : CanvasLayer
    {
        [Signal]
        public delegate void RoomSelected(int roomIndex);

        [Node]
        private ResourcePreloader resourcePreloader;
        [Node("%ButtonContainer")]
        private VBoxContainer buttonContainer;

        public override void _Notification(int what)
        {
            if (what == NotificationInstanced)
            {
                this.WireNodes();
            }
        }

        public override void _Ready()
        {

        }

        public void SetupData(RunConfig runConfig)
        {
            for (int i = 0; i < RunConfig.MAX_LEVEL; i++)
            {
                var button = resourcePreloader.InstanceSceneOrNull<AnimatedButton>();
                buttonContainer.AddChild(button);
                button.Text = $"Level {i + 1}";
                button.Connect("pressed", this, nameof(OnButtonPressed), new Godot.Collections.Array { i });
                if (i > runConfig.Level)
                {
                    button.Disabled = true;
                }
            }
        }

        private void OnButtonPressed(int roomIndex)
        {
            EmitSignal(nameof(RoomSelected), roomIndex);
        }
    }
}
