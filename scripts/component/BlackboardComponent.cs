using System.Collections.Generic;
using Godot;

namespace Game.Component
{
    public class BlackboardComponent : Node
    {
        [Signal]
        public delegate void ValueUpdated(string key);

        private Dictionary<string, object> blackboard = new();

        public void SetValue(string key, object o)
        {
            blackboard[key] = o;
            EmitSignal(nameof(ValueUpdated), key);
        }

        public void RemoveValue(string key)
        {
            if (blackboard.ContainsKey(key))
            {
                blackboard.Remove(key);
            }
            EmitSignal(nameof(ValueUpdated), key);
        }

        public T GetValue<T>(string key) where T : class
        {
            var success = blackboard.TryGetValue(key, out var o);
            if (!success)
            {
                return null;
            }
            return o as T;
        }

        public T? GetPrimitiveValue<T>(string key) where T : struct
        {
            var success = blackboard.TryGetValue(key, out var o);
            if (!success)
            {
                return null;
            }
            return (T?)o;
        }
    }
}
