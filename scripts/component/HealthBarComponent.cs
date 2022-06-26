using Godot;
using GodotUtilities;

namespace Game.Component
{
    public class HealthBarComponent : PanelContainer
    {
        [Export]
        private NodePath healthComponentPath;

        [Node]
        private HBoxContainer hBoxContainer;
        [Node("%ColorRect")]
        private ColorRect colorRect;

        public override void _Notification(int what)
        {
            if (what == NotificationInstanced)
            {
                this.WireNodes();
            }
        }

        public override void _Ready()
        {
            var healthComponent = this.GetNullableNodePath<HealthComponent>(healthComponentPath);
            healthComponent?.Connect(nameof(HealthComponent.HealthChanged), this, nameof(OnHealthChanged), new Godot.Collections.Array { healthComponent });
            healthComponent?.Connect(nameof(HealthComponent.MaxHealthChanged), this, nameof(OnMaxHealthChanged), new Godot.Collections.Array { healthComponent });

            Initialize(healthComponent);
            UpdateBars(healthComponent);
        }

        private void Initialize(HealthComponent healthComponent)
        {
            for (int i = 1; i < healthComponent.MaxHealth; i++)
            {
                var newRect = colorRect.Duplicate() as ColorRect;
                hBoxContainer.AddChild(newRect);
            }
        }

        private void UpdateBars(HealthComponent healthComponent)
        {
            for (int i = 0; i < healthComponent.MaxHealth; i++)
            {
                var rect = hBoxContainer.GetChild<ColorRect>(i);
                rect.Color = new Color(rect.Color, i < healthComponent.CurrentHealth ? 1f : 0f);
            }
        }

        private void OnHealthChanged(int newHealth, HealthComponent healthComponent)
        {
            UpdateBars(healthComponent);
        }

        private void OnMaxHealthChanged(int maxHealth, HealthComponent healthComponent)
        {
            Initialize(healthComponent);
            UpdateBars(healthComponent);
        }
    }
}
