using Game.Autoload;
using Godot;
using GodotUtilities;

namespace Game.UI
{
    public class MainMenu : CanvasLayer
    {
        [Node("%PlayButton")]
        private Button playButton;
        [Node("%OptionsButton")]
        private Button optionsButton;
        [Node("%TrainingButton")]
        private Button trainingButton;
        [Node("%QuitButton")]
        private Button quitButton;
        [Node("%EndlessButton")]
        private Button endlessButton;

        public override void _Notification(int what)
        {
            if (what == NotificationInstanced)
            {
                this.WireNodes();
            }
        }

        public override void _Ready()
        {
            playButton.Connect("pressed", this, nameof(OnPlayPressed));
            optionsButton.Connect("pressed", this, nameof(OnOptionsPressed));
            trainingButton.Connect("pressed", this, nameof(OnTrainingPressed));
            quitButton.Connect("pressed", this, nameof(OnQuitPressed));
            endlessButton.Connect("pressed", this, nameof(OnEndlessPressed));

            if (OS.HasFeature("HTML5"))
            {
                quitButton.QueueFree();
            }
        }

        private void OnPlayPressed()
        {
            ScreenTransitionManager.TransitionToScene("res://scenes/RunManager.tscn");
        }

        private async void OnOptionsPressed()
        {
            var scene = GD.Load<PackedScene>("res://scenes/ui/OptionsMenu.tscn");
            await ScreenTransitionManager.DoTransition();
            AddChild(scene.Instance());
        }

        private void OnTrainingPressed()
        {
            ScreenTransitionManager.TransitionToScene("res://scenes/level/TrainingRoom.tscn");
        }

        private void OnQuitPressed()
        {
            GetTree().Quit();
        }

        private void OnEndlessPressed()
        {
            ScreenTransitionManager.TransitionToScene("res://scenes/level/EndlessLevel.tscn");
        }
    }
}
