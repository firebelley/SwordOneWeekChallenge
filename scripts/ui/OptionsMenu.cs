using Game.Autoload;
using Godot;
using GodotUtilities;

namespace Game
{
    public class OptionsMenu : CanvasLayer
    {
        [Node("%SFXSlider")]
        private HSlider sfxSlider;
        [Node("%MusicSlider")]
        private HSlider musicSlider;
        [Node("%BackButton")]
        private Button backButton;
        [Node("%WindowButton")]
        private Button windowButton;
        [Node("%WindowContainer")]
        private Control windowContainer;

        public override void _Notification(int what)
        {
            if (what == NotificationInstanced)
            {
                this.WireNodes();
            }
        }

        public override void _Ready()
        {
            sfxSlider.Value = GetBusVolume("sfx");
            musicSlider.Value = GetBusVolume("music");

            sfxSlider.Connect("value_changed", this, nameof(OnSfxChanged));
            musicSlider.Connect("value_changed", this, nameof(OnMusicChanged));
            backButton.Connect("pressed", this, nameof(OnBackButtonPressed));
            windowButton.Connect("pressed", this, nameof(OnWindowButtonPressed));

            UpdateText();

            if (OS.HasFeature("HTML5"))
            {
                windowContainer.QueueFree();
            }
        }

        private void UpdateText()
        {
            windowButton.Text = OS.WindowFullscreen ? "Fullscreen" : "Windowed";
        }

        private float GetBusVolume(string busName)
        {
            var busIdx = AudioServer.GetBusIndex(busName);
            var dbVol = AudioServer.GetBusVolumeDb(busIdx);
            var percent = GD.Db2Linear(dbVol);
            return percent;
        }

        private void SetBusVolume(string busName, float percent)
        {
            var busIdx = AudioServer.GetBusIndex(busName);
            var db = GD.Linear2Db(percent);
            AudioServer.SetBusVolumeDb(busIdx, db);
        }

        private void OnSfxChanged(float val)
        {
            SetBusVolume("sfx", val);
        }

        private void OnMusicChanged(float val)
        {
            SetBusVolume("music", val);
        }

        private async void OnBackButtonPressed()
        {
            await ScreenTransitionManager.DoTransition();
            QueueFree();
        }

        private void OnWindowButtonPressed()
        {
            OS.WindowFullscreen = !OS.WindowFullscreen;
            UpdateText();
        }
    }
}
