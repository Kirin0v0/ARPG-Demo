using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Framework.Common.Trigger.Chain
{
    public abstract class FeatureTriggerChain<T> : BaseTriggerChain where T : Object
    {
        protected struct DynamicTriggerChain<T> where T : Object
        {
            public GameObject Chain;
            public T Target;
            public List<BaseTriggerLogic> Logics;
        }

        [Header("触发逻辑预设列表")] [SerializeField, ReadOnly]
        private List<FeatureTriggerLogic<T>> triggerLogics = new();

        private readonly Dictionary<Collider, DynamicTriggerChain<T>> _runtimeTriggerChains = new();

        private void FixedUpdate()
        {
            // 获取需要删除的触发对象
            var toDeleteTriggerObject = new List<Collider>();
            foreach (var pair in _runtimeTriggerChains)
            {
                var collider = pair.Key;
                if (!IsTriggerObjectExisting(collider, pair.Value.Target))
                {
                    toDeleteTriggerObject.Add(collider);
                }
            }

            // 删除触发对象并执行其退出触发逻辑
            foreach (var collider in toDeleteTriggerObject)
            {
                if (_runtimeTriggerChains.Remove(collider, out var triggerChain))
                {
                    triggerChain.Logics.ForEach(logic => logic.ExitTriggerChain(triggerChain.Target));
                    GameObject.Destroy(triggerChain.Chain);
                }
            }
        }

        private void OnDestroy()
        {
            // 清空所有触发对象并执行其退出触发逻辑
            foreach (var pair in _runtimeTriggerChains)
            {
                pair.Value.Logics.ForEach(logic => logic.ExitTriggerChain(pair.Value.Target));
                GameObject.Destroy(pair.Value.Chain);
            }

            _runtimeTriggerChains.Clear();
        }

        public sealed override void Begin(Collider collider)
        {
            // 如果获取触发对象失败，则判断其是否已经触发过，是则删除并执行其退出触发逻辑
            if (!TryGetTriggerObject(collider, out var triggerObject))
            {
                if (_runtimeTriggerChains.Remove(collider, out var chain))
                {
                    chain.Logics.ForEach(logic => logic.ExitTriggerChain(chain.Target));
                    GameObject.Destroy(chain.Chain);
                }

                return;
            }

            // 如果触发对象已经存在，则执行其停留触发逻辑，否则动态创建触发列表并执行其进入触发逻辑
            if (_runtimeTriggerChains.TryGetValue(collider, out var triggerChain))
            {
                // 特殊情况，如果触发对象改变，则执行旧对象退出触发逻辑并同时执行新对象的进入触发逻辑
                if (triggerChain.Target != triggerObject)
                {
                    triggerChain.Logics.ForEach(logic => logic.ExitTriggerChain(triggerChain.Target));
                    triggerChain.Chain.name = CreateChainName(triggerObject);
                    triggerChain.Target = triggerObject;
                    triggerChain.Logics.ForEach(logic => logic.EnterTriggerChain(triggerObject));
                    return;
                }

                triggerChain.Logics.ForEach(logic => logic.StayTriggerChain(triggerObject));
            }
            else
            {
                // 动态创建新的触发链
                var chainObject = new GameObject
                {
                    name = CreateChainName(triggerObject),
                    transform =
                    {
                        parent = transform.parent,
                    }
                };
                triggerChain = new DynamicTriggerChain<T>
                {
                    Chain = chainObject,
                    Target = triggerObject,
                    Logics = triggerLogics.Select(logic =>
                    {
                        var logicGameObject = new GameObject
                        {
                            transform = { parent = chainObject.transform }
                        };
                        var triggerLogic = logic.Clone(logicGameObject);
                        return triggerLogic;
                    }).ToList()
                };
                OnInitDynamicChain(triggerChain);
                _runtimeTriggerChains.Add(collider, triggerChain);
                // 执行其进入触发逻辑
                triggerChain.Logics.ForEach(logic => logic.EnterTriggerChain(triggerObject));
            }
        }

        public sealed override void Finish(Collider collider)
        {
            if (_runtimeTriggerChains.Remove(collider, out var triggerChain))
            {
                triggerChain.Logics.ForEach(logic => logic.ExitTriggerChain(triggerChain.Target));
                GameObject.Destroy(triggerChain.Chain);
            }
        }

        protected abstract bool TryGetTriggerObject(Collider collider, out T triggerObject);

        protected abstract bool IsTriggerObjectExisting(Collider collider, T triggerObject);

        protected abstract void OnInitDynamicChain(DynamicTriggerChain<T> triggerChain);

        private string CreateChainName(T triggerObject)
        {
            return triggerObject switch
            {
                MonoBehaviour monoBehaviour => monoBehaviour.gameObject.name,
                GameObject gameObject => gameObject.name,
                _ => triggerObject.name
            } + " => " + GetType().Name;
        }
    }
}