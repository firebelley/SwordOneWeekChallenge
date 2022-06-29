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
    }
}
