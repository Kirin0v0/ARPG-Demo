using Character;
using Common;
using Framework.Common.Trigger.Chain;
using Framework.Core.Extension;
using UnityEngine;
using VContainer;

namespace Trigger
{
    public class CharacterTriggerChain : FeatureTriggerChain<CharacterObject>
    {
        [SerializeField] private bool onlyPlayerCanTrigger = false;

        [Inject] private IObjectResolver _objectResolver;
        [Inject] private GameManager _gameManager;

        protected override bool TryGetTriggerObject(Collider collider, out CharacterObject triggerObject)
        {
            // 过滤非角色触发
            if (!collider.TryGetCharacter(out triggerObject))
            {
                return false;
            }

            // 过滤上帝触发
            if (triggerObject == _gameManager.God)
            {
                return false;
            }

            // 如果仅玩家就过滤非玩家触发
            if (onlyPlayerCanTrigger && triggerObject != _gameManager.Player)
            {
                return false;
            }

            return true;
        }

        protected override bool IsTriggerObjectExisting(Collider collider, CharacterObject triggerObject)
        {
            return !collider.IsGameObjectDestroyed() && !triggerObject.IsGameObjectDestroyed();
        }

        protected override void OnInitDynamicChain(DynamicTriggerChain<CharacterObject> triggerChain)
        {
            _objectResolver.Inject(triggerChain.Chain);
        }
    }
}