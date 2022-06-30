using Game.Autoload;
using Game.UI;
using Godot;
using GodotUtilities;

namespace Game
{
    public class GameComplete : CanvasLayer
    {
        [Node("%Firebelley")]
        private LinkButton firebelley;
        [Node("%HarryMakes")]
        private LinkButton harryMakes;
        [Node("%BackButton")]
        private AnimatedButton backButton;

        public override void _Notification(int what)
        {
            if (what == NotificationInstanced)
            {
                this.WireNodes();
            }
        }

        public override void _Ready()
        {
            firebelley.Connect("pressed", this, nameof(OnFirebelleyClicked));
            harryMakes.Connect("pressed", this, nameof(OnHarryMakesClicked));
            backButton.Connect("pressed", this, nameof(OnBackButtonPressed));
        }

        private void OnFirebelleyClicked()
        {
            OS.ShellOpen("https://twitter.com/firebelley");
        }

        private void OnHarryMakesClicked()
        {
            OS.ShellOpen("https://www.youtube.com/channel/UCIQBJVNeFdv-KPRIKf_az8g");
        }

        private void OnBackButtonPressed()
        {
            ScreenTransitionManager.TransitionToScene("res://scenes/ui/MainMenu.tscn");
        }
    }
}
