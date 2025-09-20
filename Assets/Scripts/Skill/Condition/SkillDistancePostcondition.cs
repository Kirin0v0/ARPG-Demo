using System;
using Character;
using Framework.Common.Util;
using UnityEngine;

namespace Skill.Condition
{
    [Serializable]
    public class SkillDistancePostcondition : BaseSkillPostcondition
    {
        private enum DistanceComparerType
        {
            Near,
            Far,
        }

        private enum DistanceAxisType
        {
            XZ,
            Y
        }

        private class SkillPostconditionDistanceTooFarFailureReason : BaseSkillPostconditionFailureReason
        {
            public override string Message => "目标距离施法者过远，无法释放该技能";
        }

        private class SkillPostconditionDistanceTooNearFailureReason : BaseSkillPostconditionFailureReason
        {
            public override string Message => "目标距离施法者过近，无法释放该技能";
        }

        [SerializeField] private DistanceComparerType comparerType = DistanceComparerType.Near;
        [SerializeField] private DistanceAxisType axisType = DistanceAxisType.XZ;
        [SerializeField] private float distance = 1f;

        public override bool MatchSkillPreconditions(
            SkillFlow skillFlow,
            CharacterObject caster,
            CharacterObject target,
            out BaseSkillPostconditionFailureReason reason
        )
        {
            reason = null;
            switch (axisType)
            {
                case DistanceAxisType.XZ:
                {
                    switch (comparerType)
                    {
                        case DistanceComparerType.Near:
                        {
                            if (!MathUtil.IsLessThanDistance(
                                    caster.Parameters.position,
                                    target.Parameters.position,
                                    distance + caster.CharacterController.radius +
                                    target.CharacterController.radius +
                                    GlobalRuleSingletonConfigSO.Instance.collideDetectionExtraRadius,
                                    MathUtil.TwoDimensionAxisType.XZ))
                            {
                                reason = new SkillPostconditionDistanceTooFarFailureReason();
                                return false;
                            }
                        }
                            break;
                        case DistanceComparerType.Far:
                        {
                            if (!MathUtil.IsMoreThanDistance(
                                    caster.Parameters.position,
                                    target.Parameters.position,
                                    distance + caster.CharacterController.radius +
                                    target.CharacterController.radius +
                                    GlobalRuleSingletonConfigSO.Instance.collideDetectionExtraRadius,
                                    MathUtil.TwoDimensionAxisType.XZ))
                            {
                                reason = new SkillPostconditionDistanceTooNearFailureReason();
                                return false;
                            }
                        }
                            break;
                    }
                }
                    break;
                case DistanceAxisType.Y:
                {
                    var additionalHeight =
                        caster.Parameters.position.y >= target.Parameters.position.y
                            ? target.CharacterController.height
                            : caster.CharacterController.height;
                    var offset = Mathf.Abs(caster.Parameters.position.y -
                                           target.Parameters.position.y);
                    switch (comparerType)
                    {
                        case DistanceComparerType.Near:
                        {
                            if (offset >= distance + additionalHeight)
                            {
                                reason = new SkillPostconditionDistanceTooFarFailureReason();
                                return false;
                            }
                        }
                            break;
                        case DistanceComparerType.Far:
                        {
                            if (offset <= distance + additionalHeight)
                            {
                                reason = new SkillPostconditionDistanceTooNearFailureReason();
                                return false;
                            }
                        }
                            break;
                    }
                }
                    break;
            }

            return true;
        }
    }
}