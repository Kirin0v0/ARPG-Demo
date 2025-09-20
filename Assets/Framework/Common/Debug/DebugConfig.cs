using System;
using UnityEngine;

namespace Framework.Common.Debug
{
    /// <summary>
    /// 调试配置类
    /// </summary>
    public class DebugConfig
    {
        // 是否打开日志系统
        public bool Enable = true;
        
        // 是否打印异常日志
        public bool LogError = true;

        // 日志前缀
        public string LogPrefix = "";

        // 是否显示帧号
        public bool ShowFrameCount = true;

        // 是否显示时间
        public bool ShowLogTime = true;

        // 显示线程id
        public bool ShowThreadId = true;

        // 显示颜色名称
        public bool ShowColorName = true;

        // 日志文件储存开关
        public bool LogFileEnable = true;

        // 是否显示FPS
        public bool FpsShowEnable = true;

        // 文件储存路径
        public string LogFilePath => Application.persistentDataPath + "/";

        // 日志文件名称
        public string LogFileName => Application.productName + " " + DateTime.Now.ToString("yyyy-MM-dd HH-mm") + ".log";
    }
}