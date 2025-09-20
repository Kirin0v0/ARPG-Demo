using System.Collections.Generic;
using System.Linq;
using Sirenix.Utilities;

namespace Character.Data
{
    public class CharacterState
    {
        private readonly Dictionary<string, float> _activeCommands = new(); // 激活指令字典
        public bool IsActive() => _activeCommands.Count > 0;

        public void Active(string command, float countdown)
        {
            if (_activeCommands.ContainsKey(command))
            {
                _activeCommands[command] = countdown;
            }
            else
            {
                _activeCommands.Add(command, countdown);
            }
        }

        public void Inactive(string command)
        {
            _activeCommands.Remove(command);
        }

        public void Countdown(float deltaTime)
        {
            var toRemovedCommands = new List<string>();
            var keys = _activeCommands.Keys.ToArray();
            keys.ForEach(key =>
            {
                _activeCommands[key] -= deltaTime;
                if (_activeCommands[key] <= 0f)
                {
                    toRemovedCommands.Add(key);
                }
            });
            toRemovedCommands.ForEach(key => _activeCommands.Remove(key));
        }
    }
}