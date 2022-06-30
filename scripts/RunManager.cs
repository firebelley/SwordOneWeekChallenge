using System.Collections.Generic;
using Game.Autoload;
using Game.Data;
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
        private RoomManager currentRoom;

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

        public override void _UnhandledInput(InputEvent evt)
        {
            if (evt.IsActionPressed("pause"))
            {
                GetTree().SetInputAsHandled();
                var pauseMenu = GD.Load<PackedScene>("res://scenes/ui/PauseMenu.tscn").InstanceOrNull<PauseMenu>();
                AddChild(pauseMenu);
                pauseMenu.Connect(nameof(PauseMenu.LevelSelectPressed), this, nameof(OnLevelSelectPressed));
                if (!IsInstanceValid(currentRoom))
                {
                    pauseMenu.HideLevelSelect();
                }
            }
        }

        private void ClearNodes()
        {
            this.GetFirstNodeOfType<LevelSelector>()?.QueueFree();
            // this.GetFirstNodeOfType<RoomManager>()?.QueueFree();
            this.GetFirstNodeOfType<PerkChoice>()?.QueueFree();
        }

        private void ShowLevelSelector()
        {
            foreach (var roomManager in roomManagers)
            {
                roomManager.Reset();
            }

            currentRoom = null;
            ClearNodes();
            runConfig.CurrentHealth = runConfig.MaxHealth;
            var levelSelector = resourcePreloader.InstanceSceneOrNull<LevelSelector>();
            AddChild(levelSelector);
            levelSelector.SetupData(runConfig);
            levelSelector.Connect(nameof(LevelSelector.RoomSelected), this, nameof(OnRoomSelected));
        }

        // private void ShowPerkChoice()
        // {
        //     ClearNodes();
        //     var perkChoice = resourcePreloader.InstanceSceneOrNull<PerkChoice>();
        //     AddChild(perkChoice);
        //     var perks = runConfig.GetPerkOptions();
        //     perkChoice.SetChoices(perks);
        //     perkChoice.Connect(nameof(PerkChoice.PerkSelected), this, nameof(OnPerkSelected));
        // }

        private async void OnRoomSelected(int roomIndex)
        {
            await ScreenTransitionManager.DoTransition();
            ClearNodes();
            var roomManager = roomManagers[roomIndex];
            roomManager.StartRoom(runConfig, new());
            currentRoom = roomManager;
        }

        private void OnRoomComplete()
        {
            if (runConfig.Level == 4)
            {
                GetTree().ChangeScene("res://scenes/ui/GameComplete.tscn");
            }
            else
            {
                runConfig.Level++;
                ShowLevelSelector();
            }
        }

        private void OnRoomFailed()
        {
            ShowLevelSelector();
        }

        // private void OnPerkSelected(PerkType perk)
        // {
        //     runConfig.AddActivePerk(perk);
        //     runConfig.Level++;
        //     ShowLevelSelector();
        // }

        private void OnSwordHealthChanged(int newHealth)
        {
            runConfig.CurrentHealth = newHealth;
        }

        private void OnLevelSelectPressed()
        {
            if (IsInstanceValid(currentRoom))
            {
                currentRoom.FailRoom();
            }
        }
    }
}
