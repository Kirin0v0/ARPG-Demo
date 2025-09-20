using UnityEngine;

namespace Interact
{
    public interface IInteractable
    {
        /// <summary>
        /// 当前是否可交互，存在物体不可交互和可交互相互切换的场景
        /// </summary>
        /// <param name="target">与该接口物体交互的对象</param>
        /// <returns>是否可交互</returns>
        public bool AllowInteract(GameObject target);
        
        /// <summary>
        /// 交互函数，交互后是否可继续交互通过AllowInteract函数控制
        /// </summary>
        /// <param name="target">与该接口物体交互的对象</param>
        public void Interact(GameObject target);

        /// <summary>
        /// 提示函数
        /// </summary>
        /// <param name="target">与该接口物体交互的对象</param>
        /// <returns>返回交互提示字符串</returns>
        public string Tip(GameObject target);
    }
}