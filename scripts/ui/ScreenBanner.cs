using Godot;
using GodotUtilities;

namespace Game.UI
{
    public class ScreenBanner : CanvasLayer
    {
        [Node]
        private MarginContainer marginContainer;
        [Node("%Label")]
        private Label label;
        [Node("%RedLabel")]
        private Label redLabel;
        [Node("%ColorRect")]
        private ColorRect colorRect;

        [Export]
        private string text;
        [Export]
        private Color backgroundColor;
        [Export]
        private bool useRedFont;

        public string Text
        {
            get => text;
            set
            {
                text = value;
            }
        }

        private SceneTreeTween backgroundTween;
        private SceneTreeTween textTween;
        private SceneTreeTween redTextTween;

        public override void _Notification(int what)
        {
            if (what == NotificationInstanced)
            {
                this.WireNodes();
            }
        }

        public override void _Ready()
        {
            label.Text = text ?? string.Empty;
            redLabel.Text = text ?? string.Empty;

            label.Visible = !useRedFont;
            redLabel.Visible = useRedFont;

            marginContainer.Visible = false;
            colorRect.Color = backgroundColor != default ? backgroundColor : colorRect.Color;
        }

        public void Play()
        {
            label.Text = text;
            marginContainer.Visible = true;
            backgroundTween = colorRect.CreateTween();
            colorRect.RectScale = new Vector2(1, 0);
            backgroundTween.TweenProperty(colorRect, "rect_scale", Vector2.One, .15f).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
            backgroundTween.TweenInterval(2.70f);
            backgroundTween.TweenProperty(colorRect, "rect_scale", new Vector2(1, 0), .15f).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);

            textTween = label.CreateTween();
            label.RectPosition = new Vector2(-label.RectSize.x, 0);
            textTween.TweenProperty(label, "rect_position", new Vector2(0, 0), .15f).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
            textTween.TweenProperty(label, "rect_position", new Vector2(15, 0), 2.7f).SetTrans(Tween.TransitionType.Linear).SetEase(Tween.EaseType.InOut);
            textTween.TweenProperty(label, "rect_position", new Vector2(label.RectSize.x, 0), .15f).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);

            redTextTween = redLabel.CreateTween();
            redLabel.RectPosition = new Vector2(-redLabel.RectSize.x, 0);
            redTextTween.TweenProperty(redLabel, "rect_position", new Vector2(0, 0), .15f).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
            redTextTween.TweenProperty(redLabel, "rect_position", new Vector2(15, 0), 2.7f).SetTrans(Tween.TransitionType.Linear).SetEase(Tween.EaseType.InOut);
            redTextTween.TweenProperty(redLabel, "rect_position", new Vector2(redLabel.RectSize.x, 0), .15f).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);

            textTween.Connect("finished", this, nameof(OnTweenFinished));
            redTextTween.Connect("finished", this, nameof(OnTweenFinished));
        }

        public override void _Process(float delta)
        {
            colorRect.RectPivotOffset = colorRect.RectSize / 2f;
        }

        public void Stop()
        {
            if (backgroundTween?.IsValid() == true)
            {
                backgroundTween.CustomStep(10f);
                backgroundTween.Kill();
            }
            if (textTween?.IsValid() == true)
            {
                textTween.CustomStep(10f);
                textTween.Kill();
            }
            if (redTextTween?.IsValid() == true)
            {
                redTextTween.CustomStep(10f);
                redTextTween.Kill();
            }
        }

        private void OnTweenFinished()
        {
            marginContainer.Visible = false;
        }
    }
}
