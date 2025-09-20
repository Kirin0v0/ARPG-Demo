using Framework.Core.Singleton;
using UnityEngine.Events;
using UnityEngine;

namespace Framework.Common.Function
{
    /// <summary>
    /// 公共MonoBehaviour模块，用于支持非MonoBehaviour使用MonoBehaviour相关能力
    /// </summary>
    public class PublicMonoBehaviour : MonoBehaviour
    {
        private event UnityAction UpdateEvent;
        private event UnityAction FixedUpdateEvent;
        private event UnityAction LateUpdateEvent;

        public void AddUpdateListener(UnityAction updateListener)
        {
            UpdateEvent += updateListener;
        }
    
        public void RemoveUpdateListener(UnityAction updateListener)
        {
            UpdateEvent -= updateListener;
        }
    
        public void AddFixedUpdateListener(UnityAction updateListener)
        {
            FixedUpdateEvent += updateListener;
        }
        
        public void RemoveFixedUpdateListener(UnityAction updateListener)
        {
            FixedUpdateEvent -= updateListener;
        }
        
        public void AddLateUpdateListener(UnityAction updateListener)
        {
            LateUpdateEvent += updateListener;
        }
        
        public void RemoveLateUpdateListener(UnityAction updateListener)
        {
            LateUpdateEvent -= updateListener;
        }
        
        private void Update()
        {
            UpdateEvent?.Invoke();
        }
    
        private void FixedUpdate()
        {
            FixedUpdateEvent?.Invoke();
        }
    
        private void LateUpdate()
        {
            LateUpdateEvent?.Invoke();
        }
    }
}