using System;
using System.Collections.Generic;
using System.Linq;
using Character;
using Framework.Common.Debug;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Skill.Condition;
using Skill.Runtime;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

namespace Skill.Condition
{
    public static partial class SkillPreconditionFailureReasons
    {
        public static readonly BaseSkillPreconditionFailureReason InCooldown =
            new SkillPreconditionInCooldownFailureReason();

        public static readonly BaseSkillPreconditionFailureReason InMute = new SkillPreconditionInMuteFailureReason();
    }

    public class SkillPreconditionInCooldownFailureReason : BaseSkillPreconditionFailureReason
    {
        public override string Message => "技能冷却中，无法释放该技能";
    }

    public class SkillPreconditionInMuteFailureReason : BaseSkillPreconditionFailureReason
    {
        public override string Message => "角色被沉默，无法释放该技能";
    }
}

namespace Skill
{
    public class SkillManager : MonoBehaviour
    {
        [Inject] private IObjectResolver _objectResolver;

        [FormerlySerializedAs("skillPool")] [LabelText("技能池")] [SerializeField] private SkillPool skillPoolTemplate;

        private SkillPool _runningSkillPool;

        private void Awake()
        {
            _runningSkillPool = skillPoolTemplate;
        }

        private void OnDestroy()
        {
            _runningSkillPool = null;
        }

        public bool TryGetSkillPrototype(string skillId, out SkillFlow skillPrototype)
        {
            return _runningSkillPool.TryGetSkillPrototype(skillId, out skillPrototype);
        }

        public bool TryGenerateSkill(string skillId, SkillGroup skillGroup, float cooldown, out Runtime.Skill skill)
        {
            skill = null;
            if (!TryGetSkillPrototype(skillId, out var prototype))
            {
                return false;
            }

            skill = new Runtime.Skill
            {
                id = skillId,
                flow = prototype,
                group = skillGroup,
                cooldown = cooldown,
                time = 0f,
            };
            return true;
        }

        public bool MatchSkillPreconditions(CharacterObject caster, Skill.Runtime.Skill skill,
            out BaseSkillPreconditionFailureReason failureReason)
        {
            // 优先检查管理器自定义的技能预条件
            if (!CheckCustomSkillPreconditions(caster, skill, out failureReason))
            {
                return false;
            }

            return skill.flow.MatchSkillPreconditions(caster, out failureReason);
        }

        public bool MatchSkillTarget(CharacterObject caster, [CanBeNull] CharacterObject target,
            Skill.Runtime.Skill skill, out string failureReason)
        {
            failureReason = "目标不符合技能目标条件";
            return skill.flow.MatchSkillTarget(caster, target);
        }

        public bool MatchSkillPostconditions(CharacterObject caster, [CanBeNull] CharacterObject target,
            Skill.Runtime.Skill skill,
            out BaseSkillPostconditionFailureReason failureReason)
        {
            failureReason = SkillPostconditionFailureReasons.Unknown;
            if (!caster.SkillAbility)
            {
                return false;
            }

            return skill.flow.MatchSkillPostconditions(caster, target, out failureReason);
        }

        public bool PassSkillConditionProcess(CharacterObject caster, [CanBeNull] CharacterObject target,
            Skill.Runtime.Skill skill,
            out string failureReason)
        {
            // 优先检查管理器自定义的技能预条件
            if (!CheckCustomSkillPreconditions(caster, skill, out var reason))
            {
                failureReason = reason.Message;
                return false;
            }

            return skill.flow.PassSkillConditionProcess(caster, target, out failureReason);
        }

        public bool ReleaseSkill(
            CharacterObject character,
            string skillId,
            [CanBeNull] CharacterObject target,
            out SkillReleaseInfo releaseInfo
        )
        {
            return ReleaseSkill(character, skillId, SkillGroup.Static, target, out releaseInfo) ||
                   ReleaseSkill(character, skillId, SkillGroup.Dynamic, target, out releaseInfo);
        }

        public bool ReleaseSkill(
            CharacterObject character,
            string skillId,
            SkillGroup skillGroup,
            [CanBeNull] CharacterObject target,
            out SkillReleaseInfo releaseInfo
        )
        {
            releaseInfo = null;
            
            DebugUtil.LogOrange($"角色({character.Parameters.DebugName})尝试释放技能({skillId})");

            // 获取技能数据
            if (!character.SkillAbility || !character.SkillAbility.TryGetSkill(skillId, skillGroup, out var skill))
            {
                DebugUtil.LogOrange($"角色({character.Parameters.DebugName})无法释放未拥有的技能({skillId})");
                return false;
            }

            // 检测管理器自定义的技能预条件
            if (!CheckCustomSkillPreconditions(character, skill, out var failureReason))
            {
                DebugUtil.LogOrange($"角色({character.Parameters.DebugName})未通过技能({skillId})的自定义预条件");
                return false;
            }

            // 判断技能释放结果
            if (!skill.flow.TryReleaseSkill(_objectResolver, character, target, out releaseInfo))
            {
                DebugUtil.LogOrange($"角色({character.Parameters.DebugName})释放技能({skillId})失败");
                return false;
            }

            DebugUtil.LogOrange($"角色({character.Parameters.DebugName})释放技能({skillId})成功！");
            // 成功释放技能后设置标识符并重置冷却时间
            skill.releasing = true;
            skill.time = skill.cooldown;
            return true;
        }

        private bool CheckCustomSkillPreconditions(CharacterObject caster, Skill.Runtime.Skill skill,
            out BaseSkillPreconditionFailureReason failureReason)
        {
            failureReason = SkillPreconditionFailureReasons.Unknown;
            if (!caster.SkillAbility)
            {
                return false;
            }

            switch (skill.group)
            {
                case SkillGroup.Static:
                {
                    if (!caster.Parameters.control.allowReleaseAbilitySkill)
                    {
                        failureReason = SkillPreconditionFailureReasons.InMute;
                        return false;
                    }
                }
                    break;
                case SkillGroup.Dynamic:
                {
                    if (!caster.Parameters.control.allowReleaseMagicSkill)
                    {
                        failureReason = SkillPreconditionFailureReasons.InMute;
                        return false;
                    }
                }
                    break;
            }

            if (skill.time > 0f)
            {
                failureReason = SkillPreconditionFailureReasons.InCooldown;
                return false;
            }

            return true;
        }
    }
}