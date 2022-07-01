using Game.Autoload;
using Godot;
using GodotUtilities;

namespace Game.UI
{
    public class EndlessLevelOver : CanvasLayer
    {
        [Node("%BackButton")]
        private AnimatedButton backButton;
        [Node("%WaveLabel")]
        private Label waveLabel;

        public int WaveNumber;

        public override void _Notification(int what)
        {
            if (what == NotificationInstanced)
            {
                this.WireNodes();
            }
        }

        public override void _Ready()
        {
            waveLabel.Text = string.Format(waveLabel.Text, WaveNumber);
            backButton.Connect("pressed", this, nameof(OnBackPressed));
        }

        private async void OnBackPressed()
        {
            await ScreenTransitionManager.TransitionToScene("res://scenes/ui/MainMenu.tscn");
            QueueFree();
        }
    }
}
