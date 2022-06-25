using Godot;

namespace Game.Autoload
{
    public class Visuals : Node
    {
        public override void _EnterTree()
        {
            VisualServer.SetDefaultClearColor(new Color(5f / 255f, 2f / 255f, 8f / 255f));
        }
    }
}
