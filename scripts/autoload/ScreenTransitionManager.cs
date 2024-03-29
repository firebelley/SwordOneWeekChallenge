using System.Threading.Tasks;
using Game.UI;
using Godot;
using GodotUtilities;

namespace Game.Autoload
{
    public class ScreenTransitionManager : Node
    {
        [Node]
        private ResourcePreloader resourcePreloader;

        private static ScreenTransitionManager instance;

        public override void _Notification(int what)
        {
            if (what == NotificationInstanced)
            {
                this.WireNodes();
                instance = this;
            }
        }

        public static async Task TransitionToScene(string scenePath, bool skipIn = false)
        {
            await DoTransition();
            instance.GetTree().ChangeScene(scenePath);
        }

        public static async Task DoTransition()
        {
            var transition = instance.resourcePreloader.InstanceSceneOrNull<Transition>();
            instance.AddChild(transition);
            var _ = transition.DoTransition();
            await instance.ToSignal(transition, nameof(Transition.TransitionedHalfway));
        }
    }
}
