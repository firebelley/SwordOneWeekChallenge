using Game.Autoload;
using Godot;
using GodotUtilities;

namespace Game.UI
{
    public class MainMenu : CanvasLayer
    {
        [Node("%PlayButton")]
        private Button playButton;

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
        }

        private void OnPlayPressed()
        {
            ScreenTransitionManager.TransitionToScene("res://scenes/RunManager.tscn");
        }
    }
}
