using System.Collections.Generic;

namespace Game.Data
{
    public class RunConfig
    {
        public const int MAX_LEVEL = 5;

        public int Level { get; set; }
        public List<RoomConfig> Rooms { get; private set; } = new();
    }
}
