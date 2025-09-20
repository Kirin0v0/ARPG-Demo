using Character;
using Common;
using Damage;
using Damage.Data;
using Framework.Common.Debug;
using Framework.Common.Trigger;
using Framework.Common.Trigger.Chain;
using UnityEngine;
using VContainer;

namespace Trigger.Point
{
    public class KillCharacterTriggerPoint :  BaseTriggerPoint<CharacterObject>
    {
        [Inject] private GameManager _gameManager;

        [Inject] private DamageManager _damageManager;

        public override void Trigger(CharacterObject target)
        {
            DebugUtil.LogCyan($"角色({target.Parameters.DebugName})因击杀触发器死亡");
            // 添加死区伤害，走实际伤害流程
            _damageManager.AddDamage(
                _gameManager.God,
                target,
                DamageEnvironmentMethod.DeadZone,
                DamageType.TrueDamage,
                new DamageValue
                {
                    physics = target.Parameters.property.maxHp,
                },
                DamageResourceMultiplier.Hp,
                0f,
                -target.transform.forward,
                true
            );
        }

        public override BaseTriggerLogic Clone(GameObject gameObject)
        {
            gameObject.name = "Kill Trigger";
            var triggerPoint = gameObject.AddComponent<KillCharacterTriggerPoint>();
            triggerPoint._gameManager = _gameManager;
            triggerPoint._damageManager = _damageManager;
            return triggerPoint;
        }
    }
}