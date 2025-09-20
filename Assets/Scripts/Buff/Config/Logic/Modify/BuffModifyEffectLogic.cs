using System;
using Character.Ability;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Buff.Config.Logic.Modify
{
    [Serializable]
    public class BuffModifyEffectLogic: BaseBuffModifyLogic
    {
        public enum TargetType
        {
            Caster,
            Carrier,
        }

        public enum TargetCharacterPosition
        {
            Center,
            Top,
            Bottom,
        }

        [Title("特效配置")] [SerializeField] private GameObject prefab;

        [SerializeField] private TargetType targetType;

        [ShowIf("@targetType == TargetType.Caster || targetType == TargetType.Carrier", true, true)] [SerializeField]
        private TargetCharacterPosition targetCharacterPosition = TargetCharacterPosition.Bottom;

        [SerializeField] private bool limitTime = false;

        [ShowIf("limitTime")] [SerializeField, MinValue(0f)]
        private float duration = 0f;

        [SerializeField] private Vector3 localPosition;
        [SerializeField] private Quaternion localRotation;
        [SerializeField] private Vector3 localScale = Vector3.one;

        
        public override void OnBuffModify(Runtime.Buff buff, int modifyStack)
        {
            if (modifyStack < 0)
            {
                return;
            }
            
            
            switch (targetType)
            {
                case TargetType.Caster:
                {
                    buff.caster.EffectAbility?.AddEffect(
                        prefab,
                        localPosition,
                        localRotation,
                        localScale,
                        targetCharacterPosition switch
                        {
                            TargetCharacterPosition.Center => CharacterEffectPosition.Center,
                            TargetCharacterPosition.Top => CharacterEffectPosition.Top,
                            TargetCharacterPosition.Bottom => CharacterEffectPosition.Bottom,
                            _ => CharacterEffectPosition.Center,
                        },
                        0f,
                        false,
                        limitTime ? duration : float.MaxValue
                    );
                }
                    break;
                case TargetType.Carrier:
                {
                    buff.carrier.EffectAbility?.AddEffect(
                        prefab,
                        localPosition,
                        localRotation,
                        localScale,
                        targetCharacterPosition switch
                        {
                            TargetCharacterPosition.Center => CharacterEffectPosition.Center,
                            TargetCharacterPosition.Top => CharacterEffectPosition.Top,
                            TargetCharacterPosition.Bottom => CharacterEffectPosition.Bottom,
                            _ => CharacterEffectPosition.Center,
                        },
                        0f,
                        false,
                        limitTime ? duration : float.MaxValue
                    );
                }
                    break;
            }
        }
    }
}