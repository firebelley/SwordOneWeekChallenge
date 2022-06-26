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

        public override void _Notification(int what)
        {
            if (what == NotificationInstanced)
            {
                this.WireNodes();
            }
        }

        public override void _Ready()
        {
            runConfig = new();
            ShowLevelSelector();
        }

        private void ClearNodes()
        {
            this.GetFirstNodeOfType<LevelSelector>()?.QueueFree();
            this.GetFirstNodeOfType<RoomManager>()?.QueueFree();
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
            GD.Print(runConfig.Level);
            var roomManager = resourcePreloader.InstanceSceneOrNull<RoomManager>();
            // TODO: transition elegantly
            AddChild(roomManager);
            roomManager.Connect(nameof(RoomManager.RoomComplete), this, nameof(OnRoomComplete));
            roomManager.Connect(nameof(RoomManager.RoomFailed), this, nameof(OnRoomFailed));
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
    }
}
