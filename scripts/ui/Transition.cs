using System.Threading.Tasks;
using Godot;
using GodotUtilities;

namespace Game.UI
{
    public class Transition : CanvasLayer
    {
        [Signal]
        public delegate void TransitionedHalfway();

        private const string ANIM_DEFAULT = "default";

        [Node]
        private AnimationPlayer animationPlayer;

        public override void _EnterTree()
        {
            this.WireNodes();
        }

        public async Task DoTransition(bool skipIn = false)
        {
            animationPlayer.Play(ANIM_DEFAULT);
            if (skipIn)
            {
                animationPlayer.Seek(.49f, true);
            }
            else
            {
                animationPlayer.Seek(0f, true);
            }
            await ToSignal(animationPlayer, "animation_finished");
            QueueFree();
        }

        protected void EmitTransitionedHalfway()
        {
            EmitSignal(nameof(TransitionedHalfway));
        }
    }
}
