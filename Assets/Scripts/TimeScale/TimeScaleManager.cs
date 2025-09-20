using System;
using System.Collections.Generic;
using System.Linq;
using Character;
using Sirenix.Utilities;
using TimeScale.Command;
using UnityEngine;

namespace TimeScale
{
    /// <summary>
    /// 时间缩放管理器，本质上是游戏管理器的时间缩放业务的拆分，因此不作为MonoBehaviour独立存在，仅由游戏管理器内部管理
    /// </summary>
    public class TimeScaleManager
    {
        // 由于业务上不同类型的时间缩放命令存在冲突，所以采用单个业务模式对应其命令，并由管理器具体控制业务模式的优先级
        private readonly TimeScaleMode<TimeScaleSingleCommand> _singleMode = new();
        private readonly TimeScaleMode<TimeScaleGlobalCommand> _globalMode = new();
        private readonly TimeScaleMode<TimeScaleComboCommand> _comboMode = new();
        private readonly TimeScaleMode<TimeScaleWitchCommand> _witchMode = new();
        private readonly TimeScaleMode<TimeScaleSkillCommand> _skillMode = new();

        // 这里记录角色最终时间缩放，方便检查值变化并执行回调
        private readonly Dictionary<int, float> _characterTimeScales = new();
        public event System.Action<int, float> OnTimeScaleChanged;

        /// <summary>
        /// 更新函数，这里传入的是游戏时间间隔，基本可以认为是现实时间
        /// </summary>
        /// <param name="deltaTime"></param>
        public void Update(float deltaTime)
        {
            // 如果技能模式激活，就仅更新技能模式，如果没激活，就更新其他模式
            if (_skillMode.IsActive())
            {
                _skillMode.Update(deltaTime);
            }
            else
            {
                // 这里存在一个问题，即多模式同时激活时同一现实时间间隔内感知到的游戏时间速度不符合游戏设定速度，这里暂时没有合理的解决方案处理
                _singleMode.Update(deltaTime);
                _globalMode.Update(deltaTime);
                _comboMode.Update(deltaTime);
                _witchMode.Update(deltaTime);
            }

            // 检查角色时间缩放值变化并执行回调
            _characterTimeScales.Keys.ToArray().ForEach(CheckWhetherCharacterTimeScaleChanged);
        }

        public void ClearAllCommands()
        {
            _singleMode.ClearAllCommands();
            _globalMode.ClearAllCommands();
            _comboMode.ClearAllCommands();
            _witchMode.ClearAllCommands();
            _skillMode.ClearAllCommands();
        }

        public float GetTimeScale(CharacterObject character)
        {
            return GetTimeScale(character.Parameters.id);
        }

        public float GetTimeScale(int characterId)
        {
            return _characterTimeScales.GetValueOrDefault(characterId, 1f);
        }

        public void AddCommand(BaseTimeScaleCommand command)
        {
            switch (command)
            {
                case TimeScaleSingleCommand timeScaleSingleCommand:
                    _singleMode.AddCommand(timeScaleSingleCommand);
                    break;
                case TimeScaleGlobalCommand timeScaleGlobalCommand:
                    _globalMode.AddCommand(timeScaleGlobalCommand);
                    break;
                case TimeScaleComboCommand timeScaleComboCommand:
                    _comboMode.AddCommand(timeScaleComboCommand);
                    break;
                case TimeScaleWitchCommand timeScaleWitchCommand:
                    _witchMode.AddCommand(timeScaleWitchCommand);
                    break;
                case TimeScaleSkillCommand timeScaleSkillCommand:
                    _skillMode.AddCommand(timeScaleSkillCommand);
                    break;
            }

            // 检查角色时间缩放值变化并执行回调
            _characterTimeScales.Keys.ToArray().ForEach(CheckWhetherCharacterTimeScaleChanged);
        }

        public void RemoveCommand(string commandId)
        {
            _singleMode.RemoveCommand(commandId);
            _globalMode.RemoveCommand(commandId);
            _comboMode.RemoveCommand(commandId);
            _witchMode.RemoveCommand(commandId);
            _skillMode.RemoveCommand(commandId);

            // 检查角色时间缩放值变化并执行回调
            _characterTimeScales.Keys.ToArray().ForEach(CheckWhetherCharacterTimeScaleChanged);
        }

        public void AddCharacter(CharacterObject character)
        {
            // 添加角色时间缩放记录
            _characterTimeScales.Add(character.Parameters.id, 1f);
            // 添加角色到各种时间模式
            _singleMode.AddCharacter(character);
            _globalMode.AddCharacter(character);
            _comboMode.AddCharacter(character);
            _witchMode.AddCharacter(character);
            _skillMode.AddCharacter(character);
            // 检查角色时间缩放值变化并执行回调
            _characterTimeScales.Keys.ToArray().ForEach(CheckWhetherCharacterTimeScaleChanged);
        }

        public void RemoveCharacter(CharacterObject character)
        {
            // 从各种时间模式移除角色
            _singleMode.RemoveCharacter(character);
            _globalMode.RemoveCharacter(character);
            _comboMode.RemoveCharacter(character);
            _witchMode.RemoveCharacter(character);
            _skillMode.RemoveCharacter(character);
            // 检查角色时间缩放值变化并执行回调
            _characterTimeScales.Keys.ToArray().ForEach(CheckWhetherCharacterTimeScaleChanged);
            // 移除角色时间缩放记录
            _characterTimeScales.Remove(character.Parameters.id);
        }

        public bool ContainsCharacter(CharacterObject character)
        {
            return _characterTimeScales.ContainsKey(character.Parameters.id);
        }

        private void CheckWhetherCharacterTimeScaleChanged(int characterId)
        {
            if (!_characterTimeScales.TryGetValue(characterId, out var oldTimeScale)) return;
            var timeScale = CalculateCharacterTimeScale();
            if (!Mathf.Approximately(oldTimeScale, timeScale))
            {
                _characterTimeScales[characterId] = timeScale;
                OnTimeScaleChanged?.Invoke(characterId, timeScale);
            }

            return;

            float CalculateCharacterTimeScale()
            {
                // 如果技能模式激活，则返回技能模式时间缩放
                if (_skillMode.IsActive())
                {
                    return _skillMode.GetTimeScale(characterId);
                }

                // 如果没激活，就混合返回其他模式的乘积
                return _singleMode.GetTimeScale(characterId) *
                       _globalMode.GetTimeScale(characterId) *
                       _comboMode.GetTimeScale(characterId) *
                       _witchMode.GetTimeScale(characterId);
            }
        }
    }
}