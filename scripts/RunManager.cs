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
        }

        private void ShowLevelSelector()
        {
            ClearNodes();
            var levelSelector = resourcePreloader.InstanceSceneOrNull<LevelSelector>();
            AddChild(levelSelector);
            levelSelector.SetupData(runConfig);
            levelSelector.Connect(nameof(LevelSelector.RoomSelected), this, nameof(OnRoomSelected));
        }

        private void OnRoomSelected(int roomIndex)
        {
            ClearNodes();
            runConfig.Level = roomIndex;
            GD.Print(runConfig.Level);
            var roomManager = resourcePreloader.InstanceSceneOrNull<RoomManager>();
            // TODO: transition elegantly
            AddChild(roomManager);
            roomManager.Connect(nameof(RoomManager.RoomComplete), this, nameof(OnRoomComplete));
            roomManager.StartRoom(runConfig, new());
        }

        private void OnRoomComplete()
        {
            ShowLevelSelector();
        }
    }
}