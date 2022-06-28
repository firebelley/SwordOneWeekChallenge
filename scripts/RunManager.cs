using System.Collections.Generic;
using Game.Data;
using Game.Data.Perk;
using Game.UI;
using Godot;
using GodotUtilities;

namespace Game
{
    public class RunManager : Node
    {
        [Node]
        private ResourcePreloader resourcePreloader;

        private RunConfig runConfig;

        private List<RoomManager> roomManagers = new();

        public override void _Notification(int what)
        {
            if (what == NotificationInstanced)
            {
                this.WireNodes();
            }
        }

        public override void _Ready()
        {
            foreach (var child in this.GetChildren<Node>())
            {
                if (child is RoomManager roomManager)
                {
                    roomManager.Connect(nameof(RoomManager.RoomComplete), this, nameof(OnRoomComplete));
                    roomManager.Connect(nameof(RoomManager.RoomFailed), this, nameof(OnRoomFailed));
                    roomManager.Connect(nameof(RoomManager.SwordHealthChanged), this, nameof(OnSwordHealthChanged));
                    roomManagers.Add(roomManager);
                }
            }

            runConfig = new();
            ShowLevelSelector();
        }

        private void ClearNodes()
        {
            this.GetFirstNodeOfType<LevelSelector>()?.QueueFree();
            // this.GetFirstNodeOfType<RoomManager>()?.QueueFree();
            this.GetFirstNodeOfType<PerkChoice>()?.QueueFree();
        }

        private void ShowLevelSelector()
        {
            ClearNodes();
            var levelSelector = resourcePreloader.InstanceSceneOrNull<LevelSelector>();
            AddChild(levelSelector);
            levelSelector.SetupData(runConfig);
            levelSelector.Connect(nameof(LevelSelector.RoomSelected), this, nameof(OnRoomSelected));
        }

        private void ShowPerkChoice()
        {
            ClearNodes();
            var perkChoice = resourcePreloader.InstanceSceneOrNull<PerkChoice>();
            AddChild(perkChoice);
            var perks = runConfig.GetPerkOptions();
            perkChoice.SetChoices(perks);
            perkChoice.Connect(nameof(PerkChoice.PerkSelected), this, nameof(OnPerkSelected));
        }

        private void OnRoomSelected(int roomIndex)
        {
            ClearNodes();
            var roomManager = roomManagers[roomIndex];
            roomManager.StartRoom(runConfig, new());
        }

        private void OnRoomComplete()
        {
            ShowPerkChoice();
        }

        private void OnRoomFailed()
        {
            runConfig = new();
            ShowLevelSelector();
        }

        private void OnPerkSelected(PerkType perk)
        {
            runConfig.AddActivePerk(perk);
            runConfig.Level++;
            ShowLevelSelector();
        }

        private void OnSwordHealthChanged(int newHealth)
        {
            runConfig.CurrentHealth = newHealth;
        }
    }
}
