using System.Collections.Generic;
using Game.Data.Perk;
using Godot;

namespace Game.Data
{
    public class RunConfig
    {
        public const int MAX_LEVEL = 5;
        public int Level { get; set; }
        public List<RoomConfig> Rooms { get; private set; } = new();

        public List<PerkType> perks = new();

        public int CurrentHealth { get; private set; } = 3;
        public int MaxHealth { get; private set; } = 3;

        public List<PerkType> GetPerkOptions()
        {
            var result = new List<PerkType>
            {
                PerkType.Health
            };
            return result;
        }

        public void AddActivePerk(PerkType perk)
        {
            perks.Add(perk);
            switch (perk)
            {
                case PerkType.Health:
                    MaxHealth++;
                    CurrentHealth = Mathf.Clamp(CurrentHealth + 1, 0, MaxHealth);
                    break;
            }
        }
    }
}
