using System;

namespace Framework.Core.Singleton
{
    /// <summary>
    /// 单例接口
    /// </summary>
    public interface ISingleton
    {
        /// <summary>
        /// 单例初始化函数
        /// </summary>
        public void OnSingletonInit();
        
        /// <summary>
        /// 单例处理函数
        /// </summary>
        public void Dispose();
    }
}