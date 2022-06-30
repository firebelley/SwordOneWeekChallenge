using GodotUtilities;
using GodotUtilities.CustomNode;

namespace Game
{
    public class RandomAudioStreamPlayerComponent : RandomAudioStreamPlayer
    {
        public override void _Notification(int what)
        {
            if (what == NotificationInstanced)
            {
                this.WireNodes();
            }
        }
    }
}
