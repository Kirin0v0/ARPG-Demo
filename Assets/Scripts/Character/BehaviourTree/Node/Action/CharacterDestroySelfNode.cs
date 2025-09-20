using Damage.Data;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.BehaviourTree.Node.Action;
using Framework.Common.Debug;

namespace Character.BehaviourTree.Node.Action
{
    [NodeMenuItem("Action/Character/Combo")]
    public class CharacterDestroySelfNode : ActionNode
    {
        public override string Description => "角色销毁自身节点，销毁自身角色物体并返回成功";

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            if (payload is not CharacterBehaviourTreeParameters parameters || !parameters.Character ||
                !parameters.GameManager || !parameters.DamageManager)
            {
                DebugUtil.LogError("The node can't execute correctly");
                return NodeState.Failure;
            }

            // 真实伤害杀死角色
            parameters.DamageManager.AddDamage(
                parameters.GameManager.God,
                parameters.Character,
                DamageEnvironmentMethod.Default,
                DamageType.TrueDamage,
                new DamageValue
                {
                    noType = parameters.Character.Parameters.property.maxHp,
                },
                DamageResourceMultiplier.Hp,
                0f,
                parameters.Character.transform.forward,
                true
            );
            // 设置角色销毁参数
            parameters.Character.StateAbility.SetDestroyParameters(true, 0f);
            return NodeState.Success;
        }
    }
}