using Framework.Core.Singleton;
using UnityEngine;

namespace Framework.Common.Debug
{
    /// <summary>
    /// 调试类，根据调试配置初始化相关系统
    /// </summary>
    public class Debugger : Singleton<Debugger>
    {
        private Debugger()
        {
        }

        public void Init(DebugConfig debugConfig)
        {
            // 这里要提前定义Log系统宏
#if DEBUG_ENABLE
            DebugUtil.InitConfig(debugConfig);
            if (debugConfig.LogFileEnable)
            {
                DebugFileSystem.Instance.InitConfig(debugConfig);
            }

            DebugFpsSystem.Instance.enabled = debugConfig.FpsShowEnable;
#else
        // Debug.unityLogger.logEnabled = false;
#endif
        }
    }
}