using System;
using UnityEngine;

namespace Framework.Common.BehaviourTree
{
    public class BehaviourTreeComponent : MonoBehaviour
    {
        [SerializeField] private BehaviourTreeExecutor executor;

        private void Awake()
        {
            executor.Init();
            executor.Tick(0);
        }

        private void Update()
        {
            executor.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            executor.Destroy();
        }
    }
}