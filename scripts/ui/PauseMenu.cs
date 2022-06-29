using Game.Autoload;
using Godot;
using GodotUtilities;

namespace Game.UI
{
    public class PauseMenu : CanvasLayer
    {
        [Node("%OptionsButton")]
        private Button optionsButton;
        [Node("%ResumeButton")]
        private Button resumeButton;
        [Node("%QuitButton")]
        private Button quitButton;

        public override void _Notification(int what)
        {
            if (what == NotificationInstanced)
            {
                this.WireNodes();
            }
        }

        public override void _Ready()
        {
            GetTree().Paused = true;
            optionsButton.Connect("pressed", this, nameof(OnOptionsPressed));
            resumeButton.Connect("pressed", this, nameof(OnResumePressed));
            quitButton.Connect("pressed", this, nameof(OnQuitPressed));
        }

        public override void _UnhandledInput(InputEvent evt)
        {
            if (evt.IsActionPressed("pause"))
            {
                GetTree().SetInputAsHandled();
                OnResumePressed();
            }
        }

        public override void _Process(float delta)
        {
            if (!GetTree().Paused)
            {
                QueueFree();
            }
        }

        private async void OnOptionsPressed()
        {
            await ScreenTransitionManager.DoTransition();
            var options = GD.Load<PackedScene>("res://scenes/ui/OptionsMenu.tscn");
            AddChild(options.Instance());
        }

        private void OnResumePressed()
        {
            GetTree().Paused = false;
        }

        private async void OnQuitPressed()
        {
            await ScreenTransitionManager.DoTransition();
            GetTree().Paused = false;
            GetTree().ChangeScene("res://scenes/Main.tscn");
        }
    }
}
