using System.Collections.Generic;
using System.Linq;
using Buff.Data;
using Features.Game.Data;
using Framework.Core.LiveData;
using Player;
using Skill.Runtime;

namespace Features.Game.UI.Character
{
    public class GameCharacterModel
    {
        private readonly PlayerCharacterObject _player;
        private readonly int _gridRowNumber;

        private readonly MutableLiveData<List<GameBuffDetailUIData>> _buffs = new();
        public LiveData<List<GameBuffDetailUIData>> GetBuffList() => _buffs;

        private readonly MutableLiveData<List<GameCharacterSkillUIData>> _abilitySkills = new();
        public LiveData<List<GameCharacterSkillUIData>> GetAbilitySkillList() => _abilitySkills;

        private readonly MutableLiveData<List<GameCharacterSkillUIData>> _magicSkills = new();
        public LiveData<List<GameCharacterSkillUIData>> GetMagicSkillList() => _magicSkills;

        public GameCharacterModel(PlayerCharacterObject player, int gridRowNumber)
        {
            _player = player;
            _gridRowNumber = gridRowNumber;
        }

        public void FetchCharacterInformation()
        {
            // 获取角色能力和魔法列表
            var abilitySkills = new List<GameCharacterSkillUIData>();
            var magicSkills = new List<GameCharacterSkillUIData>();
            _player.Parameters.skills.ForEach(skill =>
            {
                switch (skill.group)
                {
                    case SkillGroup.Static:
                    {
                        abilitySkills.Add(new GameCharacterSkillUIData
                        {
                            Skill = skill,
                            Focused = false,
                        });
                    }
                        break;
                    case SkillGroup.Dynamic:
                    {
                        magicSkills.Add(new GameCharacterSkillUIData
                        {
                            Skill = skill,
                            Focused = false,
                        });
                    }
                        break;
                }
            });
            _abilitySkills.SetValue(abilitySkills);
            _magicSkills.SetValue(magicSkills);

            // 获取角色Buff列表
            var buffs = _player.Parameters.buffs.Where(buff => buff.info.visibility != BuffVisibility.Invisible)
                .Select(buff => new GameBuffDetailUIData
                {
                    Id = buff.info.id,
                    Name = buff.info.name,
                    Icon = buff.info.icon,
                    Description = buff.info.Description,
                    Permanent = buff.permanent,
                    Duration = buff.duration,
                    MaxStack = buff.info.maxStack,
                    Stack = buff.stack,
                    CasterName = buff.caster.Parameters.name,
                    Focused = false,
                }).ToList();
            _buffs.SetValue(buffs);
        }

        public bool FocusUpperAbilitySkill(int position, out int index, out GameCharacterSkillUIData data)
        {
            index = -1;
            data = null;
            if (position <= 0 || position >= _abilitySkills.Value.Count)
            {
                return false;
            }

            index = position - 1;
            data = _abilitySkills.Value[index];
            data.Focused = true;
            return true;
        }

        public bool FocusLowerAbilitySkill(int position, out int index, out GameCharacterSkillUIData data)
        {
            index = -1;
            data = null;
            if (position < 0 || position >= _abilitySkills.Value.Count - 1)
            {
                return false;
            }

            index = position + 1;
            data = _abilitySkills.Value[index];
            data.Focused = true;
            return true;
        }

        public bool FocusUpperMagicSkill(int position, out int index, out GameCharacterSkillUIData data)
        {
            index = -1;
            data = null;
            if (position <= 0 || position >= _magicSkills.Value.Count)
            {
                return false;
            }

            index = position - 1;
            data = _magicSkills.Value[index];
            data.Focused = true;
            return true;
        }

        public bool FocusLowerMagicSkill(int position, out int index, out GameCharacterSkillUIData data)
        {
            index = -1;
            data = null;
            if (position < 0 || position >= _magicSkills.Value.Count - 1)
            {
                return false;
            }

            index = position + 1;
            data = _magicSkills.Value[index];
            data.Focused = true;
            return true;
        }

        public bool FocusUpperBuff(int position, out int index, out GameBuffDetailUIData data)
        {
            index = -1;
            data = null;
            if (position < 0 || position >= _buffs.Value.Count || position < _gridRowNumber)
            {
                return false;
            }

            index = position - _gridRowNumber;
            data = _buffs.Value[index];
            data.Focused = true;
            return true;
        }

        public bool FocusLowerBuff(int position, out int index, out GameBuffDetailUIData data)
        {
            index = -1;
            data = null;
            if (position < 0 || position >= _buffs.Value.Count || position >= _buffs.Value.Count - _gridRowNumber)
            {
                return false;
            }

            index = position + _gridRowNumber;
            data = _buffs.Value[index];
            data.Focused = true;
            return true;
        }

        public bool FocusLeftBuff(int position, out int index, out GameBuffDetailUIData data)
        {
            index = -1;
            data = null;
            if (position < 0 || position >= _buffs.Value.Count || position % _gridRowNumber == 0)
            {
                return false;
            }

            index = position - 1;
            data = _buffs.Value[index];
            data.Focused = true;
            return true;
        }

        public bool FocusRightBuff(int position, out int index, out GameBuffDetailUIData data)
        {
            index = -1;
            data = null;
            if (position < 0 || position >= _buffs.Value.Count || position % _gridRowNumber == _gridRowNumber - 1)
            {
                return false;
            }

            index = position + 1;
            data = _buffs.Value[index];
            data.Focused = true;
            return true;
        }
    }
}