using System.Collections.Generic;
using System.Linq;
using Character;
using Sirenix.Utilities;
using TimeScale.Command;

namespace TimeScale
{
    public class TimeScaleMode<T> : ITimeScaleEdit where T: BaseTimeScaleCommand
    {
        private readonly Dictionary<string, T> _commands = new();
        private readonly Dictionary<int, CharacterObject> _characterIdMappings = new();
        private readonly Dictionary<int, float> _characterTimeScales = new();

        public void Update(float deltaTime)
        {
            // 更新命令执行时间并选出需要移除的命令
            var toRemoveCommands = new List<string>();
            _commands.ForEach(pair =>
            {
                var command = pair.Value;
                command.Time += deltaTime;
                if (command.Time >= command.Duration)
                {
                    toRemoveCommands.Add(command.Id);
                }
            });
            // 移除命令并更新时间缩放
            if (_commands.Count == 0) return;
            toRemoveCommands.ForEach(id => { _commands.Remove(id); });
            UpdateTimeScales();
        }

        public bool IsActive() => _commands.Count != 0;

        public float GetTimeScale(CharacterObject character)
        {
            return GetTimeScale(character.Parameters.id);
        }

        public float GetTimeScale(int characterId)
        {
            return _characterTimeScales.GetValueOrDefault(characterId, 1f);
        }

        public void SetTimeScale(CharacterObject character, float timeScale)
        {
            _characterTimeScales[character.Parameters.id] = timeScale;
        }

        public void MultiplyTimeScale(CharacterObject character, float multiplier)
        {
            _characterTimeScales[character.Parameters.id] *= multiplier;
        }

        public void AddCommand(T command)
        {
            if (!_commands.TryAdd(command.Id, command)) return;
            UpdateTimeScales();
        }

        public void RemoveCommand(string commandId)
        {
            if (!_commands.Remove(commandId, out var command)) return;
            UpdateTimeScales();
        }

        public void ClearAllCommands()
        {
            _commands.Clear();
            UpdateTimeScales();
        }

        public void AddCharacter(CharacterObject character)
        {
            _characterIdMappings.Add(character.Parameters.id, character);
            _characterTimeScales.Add(character.Parameters.id, 1f);
            _commands.ForEach(pair => pair.Value.Execute(this, character));
        }

        public void RemoveCharacter(CharacterObject character)
        {
            _characterIdMappings.Remove(character.Parameters.id);
            _characterTimeScales.Remove(character.Parameters.id);
        }

        private void UpdateTimeScales()
        {
            _characterTimeScales.Keys.ToList().ForEach(id => { _characterTimeScales[id] = 1f; });
            _characterIdMappings.Values.ForEach(character =>
            {
                _commands.Values.ForEach(command => command.Execute(this, character));
            });
        }
    }
}