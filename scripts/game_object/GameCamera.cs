using Godot;
using GodotUtilities;

namespace Game.GameObject
{
    public class GameCamera : Camera2D
    {
        private const float MAX_OFFSET = 10f;
        private const float X_DELTA = 19f;
        private const float Y_DELTA = 25f;
        private const float AMPLITUDE_DECAY = 5f;

        [Export]
        private OpenSimplexNoise noise;

        private float shakeAmplitude;

        private Vector2 noiseSample;

        public override void _Notification(int what)
        {
            if (what == NotificationInstanced)
            {
                this.WireNodes();
            }
        }

        public override void _Process(float delta)
        {
            noiseSample += new Vector2(X_DELTA, Y_DELTA) * delta;
            noiseSample.x = Mathf.Wrap(noiseSample.x, -1000, 1000);
            noiseSample.y = Mathf.Wrap(noiseSample.y, -1000, 1000);

            var xNoise = noise.GetNoise1d(noiseSample.x);
            var yNoise = noise.GetNoise1d(noiseSample.y);

            Offset = new Vector2(xNoise, yNoise) * MAX_OFFSET * shakeAmplitude * shakeAmplitude;

            shakeAmplitude = Mathf.Clamp(shakeAmplitude - (AMPLITUDE_DECAY * delta), 0f, 1f);
        }

        public void Shake()
        {
            shakeAmplitude = 1f;
        }
    }
}
