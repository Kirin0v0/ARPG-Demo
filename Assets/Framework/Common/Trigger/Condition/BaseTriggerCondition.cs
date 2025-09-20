using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Framework.Common.Trigger.Chain
{
    public abstract class BaseTriggerCondition<T> : FeatureTriggerLogic<T> where T : Object
    {
        [Header("触发逻辑预设列表")] [SerializeField] private List<FeatureTriggerLogic<T>> triggerLogics = new();

        private bool _triggered = false;

        public override void EnterTriggerChain(T target)
        {
            // 如果不满足条件，则判断是否已触发，如果是则执行退出触发逻辑并设置为未触发
            if (!MatchCondition(target))
            {
                if (_triggered)
                {
                    triggerLogics.ForEach(logic => logic.ExitTriggerChain(target));
                    _triggered = false;
                }

                return;
            }

            // 如果已触发，则执行其停留触发逻辑，否则执行其进入触发逻辑
            if (_triggered)
            {
                triggerLogics.ForEach(logic => logic.StayTriggerChain(target));
            }
            else
            {
                triggerLogics.ForEach(logic => logic.EnterTriggerChain(target));
                _triggered = true;
            }
        }

        public override void StayTriggerChain(T target)
        {
            // 如果不满足条件，则判断是否已触发，如果是则执行退出触发逻辑并设置为未触发
            if (!MatchCondition(target))
            {
                if (_triggered)
                {
                    triggerLogics.ForEach(logic => logic.ExitTriggerChain(target));
                    _triggered = false;
                }

                return;
            }

            // 如果已触发，则执行其停留触发逻辑，否则执行其进入触发逻辑
            if (_triggered)
            {
                triggerLogics.ForEach(logic => logic.StayTriggerChain(target));
            }
            else
            {
                triggerLogics.ForEach(logic => logic.EnterTriggerChain(target));
                _triggered = true;
            }
        }

        public override void ExitTriggerChain(T target)
        {
            // 判断是否已触发，如果是则执行退出触发逻辑并设置为未触发
            if (_triggered)
            {
                triggerLogics.ForEach(logic => logic.ExitTriggerChain(target));
                _triggered = false;
            }
        }

        protected abstract bool MatchCondition(T target);

        public sealed override BaseTriggerLogic Clone(GameObject gameObject)
        {
            var condition = OnClone(gameObject);
            condition.triggerLogics = triggerLogics.Select(logic =>
            {
                var logicGameObject = new GameObject
                {
                    transform = { parent = gameObject.transform }
                };
                var triggerLogic = logic.Clone(logicGameObject) as FeatureTriggerLogic<T>;
                return triggerLogic;
            }).ToList();
            return condition;
        }

        protected abstract BaseTriggerCondition<T> OnClone(GameObject gameObject);
    }
}