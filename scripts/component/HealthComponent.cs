using Godot;
using GodotUtilities;

namespace Game.Component
{
    public class HealthComponent : Node
    {
        [Signal]
        public delegate void HealthChanged(int currentHealth);
        [Signal]
        public delegate void MaxHealthChanged(int maxHealth);
        [Signal]
        public delegate void Died();

        [Export]
        private int maxHealth = 3;

        private int currentHealth;

        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;

        public override void _Notification(int what)
        {
            if (what == NotificationInstanced)
            {
                this.WireNodes();
            }
        }

        public override void _Ready()
        {
            currentHealth = maxHealth;
            EmitSignal(nameof(MaxHealthChanged), maxHealth);
            EmitSignal(nameof(HealthChanged), currentHealth);
        }

        public void SetMaxHealth(int max)
        {
            maxHealth = max;
            EmitSignal(nameof(MaxHealthChanged), maxHealth);
        }

        public void SetCurrentHealth(int current)
        {
            currentHealth = current;
            EmitSignal(nameof(HealthChanged), currentHealth);
        }

        public void Damage(int damage)
        {
            currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
            EmitSignal(nameof(HealthChanged), currentHealth);
            if (currentHealth == 0)
            {
                EmitSignal(nameof(Died));
            }
        }

        public void Heal(int amount)
        {
            Damage(-amount);
        }
    }
}
