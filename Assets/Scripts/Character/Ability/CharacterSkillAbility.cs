using System.Collections.Generic;
using System.Linq;
using Framework.Common.Debug;
using Framework.Common.Timeline;
using JetBrains.Annotations;
using Sirenix.Utilities;
using Skill;
using Skill.Condition;
using Skill.Runtime;
using UnityEngine;
using VContainer;

namespace Character.Ability
{
    public class CharacterSkillAbility : BaseCharacterOptionalAbility
    {
        [SerializeField] private List<string> defaultAbilitySkills = new();

        [Inject] private IObjectResolver _objectResolver;
        [Inject] private SkillManager _skillManager;

        private readonly Dictionary<(string skillId, SkillGroup group), (Skill.Runtime.Skill skill, int refrenceCount)>
            _skillReferences = new(); // 技能配置引用计数，防止不同模块添加/删除同一技能导致其他模块添加的该技能失效

        // 技能释放列表，包含全部释放技能以及正在释放的技能
        private readonly List<SkillReleaseInfo> _totalReleasedSkills = new();
        private readonly List<SkillReleaseInfo> _releasingSkills = new();
        public SkillReleaseInfo NewestReleasingSkill => _releasingSkills.Count != 0 ? _releasingSkills[^1] : null;

        public event System.Action<SkillReleaseInfo> OnSkillReleased;
        public event System.Action<SkillReleaseInfo> OnSkillStopped;
        public event System.Action<SkillReleaseInfo> OnSkillCompleted;

        private float _skillTimeScale = 1f;

        protected override void OnInit()
        {
            base.OnInit();
            // 初始化默认能力技能
            defaultAbilitySkills.ForEach(skillId => AddSkill(skillId, SkillGroup.Static));
        }

        public void Tick(float deltaTime)
        {
            if (Owner.Parameters.dead)
            {
                return;
            }

            // 每帧检测技能冷却时间
            Owner.Parameters.skills.ForEach(skill =>
            {
                if (skill.releasing)
                {
                    return;
                }

                skill.time = Mathf.Clamp(skill.time - deltaTime, 0f, skill.time);
            });
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            // 清空角色技能引用
            _skillReferences.Clear();
            Owner.Parameters.skills.Clear();
            // 停止技能释放
            StopAllReleasingSkills();
            _releasingSkills.Clear();
            // 销毁所有已释放的技能资源
            _totalReleasedSkills.ForEach(skillReleaseInfo => { skillReleaseInfo.Destroy(); });
            _totalReleasedSkills.Clear();
        }

        public bool TryGetSkill(string id, out Skill.Runtime.Skill skill)
        {
            skill = Owner.Parameters.skills.Find(skill => skill.id == id);
            return skill != null;
        }

        public bool TryGetSkill(string id, SkillGroup group, out Skill.Runtime.Skill skill)
        {
            skill = Owner.Parameters.skills.Find(skill => skill.id == id && skill.group == group);
            return skill != null;
        }

        public bool MatchSkillPreconditions(string skillId, out BaseSkillPreconditionFailureReason failureReason)
        {
            failureReason = SkillPreconditionFailureReasons.Unknown;
            if (!Owner.SkillAbility)
            {
                return false;
            }

            return Owner.SkillAbility.TryGetSkill(skillId, out var skill) &&
                   _skillManager.MatchSkillPreconditions(Owner, skill, out failureReason);
        }

        public bool MatchSkillPreconditions(string skillId, SkillGroup skillGroup,
            out BaseSkillPreconditionFailureReason failureReason)
        {
            failureReason = SkillPreconditionFailureReasons.Unknown;
            if (!Owner.SkillAbility)
            {
                return false;
            }

            return Owner.SkillAbility.TryGetSkill(skillId, skillGroup, out var skill) &&
                   _skillManager.MatchSkillPreconditions(Owner, skill, out failureReason);
        }

        public bool MatchSkillTarget(string skillId, CharacterObject target, out string failureReason)
        {
            failureReason = "目标不符合技能目标条件";
            if (!Owner.SkillAbility)
            {
                return false;
            }

            return Owner.SkillAbility.TryGetSkill(skillId, out var skill) &&
                   _skillManager.MatchSkillTarget(Owner, target, skill, out failureReason);
        }

        public bool MatchSkillTarget(string skillId, SkillGroup skillGroup, CharacterObject target,
            out string failureReason)
        {
            failureReason = "目标不符合技能目标条件";
            if (!Owner.SkillAbility)
            {
                return false;
            }

            return Owner.SkillAbility.TryGetSkill(skillId, skillGroup, out var skill) &&
                   _skillManager.MatchSkillTarget(Owner, target, skill, out failureReason);
        }

        public bool MatchSkillPostconditions(string skillId, CharacterObject target,
            out BaseSkillPostconditionFailureReason failureReason)
        {
            failureReason = SkillPostconditionFailureReasons.Unknown;
            if (!Owner.SkillAbility)
            {
                return false;
            }

            return Owner.SkillAbility.TryGetSkill(skillId, out var skill) &&
                   _skillManager.MatchSkillPostconditions(Owner, target, skill, out failureReason);
        }

        public bool MatchSkillPostconditions(string skillId, SkillGroup skillGroup, CharacterObject target,
            out BaseSkillPostconditionFailureReason failureReason)
        {
            failureReason = SkillPostconditionFailureReasons.Unknown;
            if (!Owner.SkillAbility)
            {
                return false;
            }

            return Owner.SkillAbility.TryGetSkill(skillId, skillGroup, out var skill) &&
                   _skillManager.MatchSkillPostconditions(Owner, target, skill, out failureReason);
        }

        public bool FindAllowReleaseSkill(string skillId, [CanBeNull] CharacterObject target,
            out Skill.Runtime.Skill skill)
        {
            skill = null;
            foreach (var item in Owner.Parameters.skills)
            {
                if (item.id != skillId)
                {
                    continue;
                }

                if (_skillManager.PassSkillConditionProcess(Owner, target, item, out _))
                {
                    skill = item;
                    return true;
                }
            }

            return false;
        }

        public bool FindAllowReleaseSkills(List<string> skillIds, [CanBeNull] CharacterObject target,
            out List<Skill.Runtime.Skill> skills)
        {
            skills = new List<Skill.Runtime.Skill>();
            var ids = skillIds.ToHashSet();
            foreach (var item in Owner.Parameters.skills)
            {
                if (!ids.Contains(item.id))
                {
                    continue;
                }

                if (_skillManager.PassSkillConditionProcess(Owner, target, item, out _))
                {
                    skills.Add(item);
                }
            }

            return skills.Count != 0;
        }

        public bool FindAllowReleaseSkills([CanBeNull] CharacterObject target, out List<Skill.Runtime.Skill> skills)
        {
            skills = new List<Skill.Runtime.Skill>();
            foreach (var item in Owner.Parameters.skills)
            {
                if (_skillManager.PassSkillConditionProcess(Owner, target, item, out _))
                {
                    skills.Add(item);
                }
            }

            return skills.Count != 0;
        }

        /// <summary>
        /// 添加角色技能，这里将技能分组是为了区别内在技能和外部技能，设计为自身掌握的技能为能力（Ability），外部提供的技能为魔法（Magic）
        /// </summary>
        /// <param name="skillId"></param>
        /// <param name="skillGroup"></param>
        public void AddSkill(string skillId, SkillGroup skillGroup)
        {
            if (_skillReferences.ContainsKey((skillId, skillGroup)))
            {
                var tuple = _skillReferences[(skillId, skillGroup)];
                tuple.refrenceCount++;
            }
            else if (_skillManager.TryGenerateSkill(skillId, skillGroup, 0.1f, out var skill))
            {
                _skillReferences.Add((skillId, skillGroup), (skill, 1));
                Owner.Parameters.skills.Add(skill);
            }
        }

        public void DeleteSkill(string skillId, SkillGroup skillGroup)
        {
            if (!_skillReferences.TryGetValue((skillId, skillGroup), out var tuple))
            {
                return;
            }

            tuple.refrenceCount--;
            if (tuple.refrenceCount != 0)
            {
                return;
            }

            _skillReferences.Remove((skillId, skillGroup));
            Owner.Parameters.skills.Remove(tuple.skill);
        }

        public bool ReleaseSkill(string skillId, [CanBeNull] CharacterObject target, out SkillReleaseInfo releaseInfo)
        {
            if (!_skillManager.ReleaseSkill(Owner, skillId, target, out releaseInfo)) return false;
            HandleSkillRelease(releaseInfo);
            releaseInfo.OnStop += HandleSkillStopped;
            releaseInfo.OnComplete += HandleSkillCompleted;
            return true;
        }

        public bool ReleaseSkill(string skillId, SkillGroup skillGroup, [CanBeNull] CharacterObject target,
            out SkillReleaseInfo releaseInfo)
        {
            if (!_skillManager.ReleaseSkill(Owner, skillId, skillGroup, target, out releaseInfo)) return false;
            HandleSkillRelease(releaseInfo);
            releaseInfo.OnStop += HandleSkillStopped;
            releaseInfo.OnComplete += HandleSkillCompleted;
            return true;
        }

        public void StopReleasingSkill(string skillId)
        {
            var releaseInfo = _releasingSkills.Find(skillReleaseInfo => skillReleaseInfo.Id == skillId);
            if (releaseInfo == null)
            {
                return;
            }

            releaseInfo.Stop();
        }

        public void StopAllReleasingSkills()
        {
            _releasingSkills.ToArray().ForEach(skillReleaseInfo => skillReleaseInfo.Stop());
        }

        public void SetSkillSpeed(float speed)
        {
            _skillTimeScale = speed;
            _releasingSkills.ForEach(skillReleaseInfo => skillReleaseInfo.SetTimeScale(speed));
        }

        private void HandleSkillRelease(SkillReleaseInfo skillReleaseInfo)
        {
            skillReleaseInfo.SetTimeScale(_skillTimeScale);
            _totalReleasedSkills.Add(skillReleaseInfo);
            _releasingSkills.Add(skillReleaseInfo);
            OnSkillReleased?.Invoke(skillReleaseInfo);
        }

        private void HandleSkillStopped(SkillReleaseInfo skillReleaseInfo)
        {
            _releasingSkills.Remove(skillReleaseInfo);
            // 遍历自身技能列表，重置其释放标识符
            Owner.Parameters.skills.ForEach(skill =>
            {
                if (skill.id == skillReleaseInfo.Id)
                {
                    skill.releasing = false;
                }
            });
            OnSkillStopped?.Invoke(skillReleaseInfo);
        }

        private void HandleSkillCompleted(SkillReleaseInfo skillReleaseInfo)
        {
            _releasingSkills.Remove(skillReleaseInfo);
            // 遍历自身技能列表，重置其释放标识符
            Owner.Parameters.skills.ForEach(skill =>
            {
                if (skill.id == skillReleaseInfo.Id)
                {
                    skill.releasing = false;
                }
            });
            OnSkillCompleted?.Invoke(skillReleaseInfo);
        }
    }
}