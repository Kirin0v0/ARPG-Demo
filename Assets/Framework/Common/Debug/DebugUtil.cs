using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Framework.Common.Debug
{
    public static class DebugUtil
    {
        public enum LogColor
        {
            None,
            Cyan,
            Blue,
            LightBlue,
            Green,
            Grey,
            Orange,
            Purple,
            Magenta,
            Red,
            Yellow,
        }

        private static DebugConfig _debugConfig = new DebugConfig();

        [Conditional("DEBUG_ENABLE")]
        public static void InitConfig(DebugConfig debugConfig)
        {
            _debugConfig = debugConfig;
        }

        [Conditional("DEBUG_ENABLE")]
        public static void Log(object obj)
        {
            if (!_debugConfig.Enable)
            {
                return;
            }

            string log = GenerateLog(obj.ToString());
            UnityEngine.Debug.Log(log);
        }

        [Conditional("DEBUG_ENABLE")]
        public static void Log(string obj, params object[] args)
        {
            if (!_debugConfig.Enable)
            {
                return;
            }

            string conent = string.Empty;
            if (args != null)
            {
                foreach (var item in args)
                {
                    conent += item;
                }
            }

            string log = GenerateLog(obj + conent);
            UnityEngine.Debug.Log(log);
        }

        [Conditional("DEBUG_ENABLE")]
        public static void LogWarning(object obj)
        {
            if (!_debugConfig.Enable)
            {
                return;
            }

            string log = GenerateLog(obj.ToString());
            UnityEngine.Debug.LogWarning(log);
        }

        [Conditional("DEBUG_ENABLE")]
        public static void LogWarning(string obj, params object[] args)
        {
            if (!_debugConfig.Enable)
            {
                return;
            }

            string conent = string.Empty;
            if (args != null)
            {
                foreach (var item in args)
                {
                    conent += item;
                }
            }

            string log = GenerateLog(obj + conent);
            UnityEngine.Debug.LogWarning(log);
        }

        [Conditional("DEBUG_ENABLE")]
        public static void LogError(object obj)
        {
            if (!_debugConfig.Enable)
            {
                return;
            }

            string log = GenerateLog(obj.ToString());
            UnityEngine.Debug.LogError(log);
        }

        [Conditional("DEBUG_ENABLE")]
        public static void LogError(string obj, params object[] args)
        {
            if (!_debugConfig.Enable || !_debugConfig.LogError)
            {
                return;
            }
            
            string conent = string.Empty;
            if (args != null)
            {
                foreach (var item in args)
                {
                    conent += item;
                }
            }

            string log = GenerateLog(obj + conent);
            UnityEngine.Debug.LogError(log);
        }

        [Conditional("DEBUG_ENABLE")]
        public static void LogWithColor(LogColor color, object obj)
        {
            if (!_debugConfig.Enable)
            {
                return;
            }

            string log = GenerateLog(obj.ToString(), color);
            log = GetUnityColor(log, color);
            UnityEngine.Debug.Log(log);
        }

        [Conditional("DEBUG_ENABLE")]
        public static void LogCyan(object obj)
        {
            LogWithColor(LogColor.Cyan, obj);
        }

        [Conditional("DEBUG_ENABLE")]
        public static void LogBlue(object obj)
        {
            LogWithColor(LogColor.Blue, obj);
        }

        [Conditional("DEBUG_ENABLE")]
        public static void LogLightBlue(object obj)
        {
            LogWithColor(LogColor.LightBlue, obj);
        }

        [Conditional("DEBUG_ENABLE")]
        public static void LogGreen(object obj)
        {
            LogWithColor(LogColor.Green, obj);
        }

        [Conditional("DEBUG_ENABLE")]
        public static void LogGrey(object obj)
        {
            LogWithColor(LogColor.Grey, obj);
        }

        [Conditional("DEBUG_ENABLE")]
        public static void LogOrange(object obj)
        {
            LogWithColor(LogColor.Orange, obj);
        }

        [Conditional("DEBUG_ENABLE")]
        public static void LogPurple(object obj)
        {
            LogWithColor(LogColor.Purple, obj);
        }

        [Conditional("DEBUG_ENABLE")]
        public static void LogMagenta(object obj)
        {
            LogWithColor(LogColor.Magenta, obj);
        }

        [Conditional("DEBUG_ENABLE")]
        public static void LogRed(object obj)
        {
            LogWithColor(LogColor.Red, obj);
        }

        [Conditional("DEBUG_ENABLE")]
        public static void LogYellow(object obj)
        {
            LogWithColor(LogColor.Yellow, obj);
        }

        private static string GenerateLog(string log, LogColor color = LogColor.None)
        {
            StringBuilder stringBuilder = new StringBuilder(_debugConfig.LogPrefix, 100);
            if (_debugConfig.ShowColorName)
            {
                if (color != LogColor.None)
                {
                    stringBuilder.AppendFormat(" {0}", color.ToString());
                }
            }
            
            if (_debugConfig.ShowFrameCount)
            {
                stringBuilder.AppendFormat(" {0}", Time.frameCount);
            }

            if (_debugConfig.ShowLogTime)
            {
                stringBuilder.AppendFormat(" {0}", DateTime.Now.ToString("hh:mm:ss-fff"));
            }

            if (_debugConfig.ShowThreadId)
            {
                stringBuilder.AppendFormat(" ThreadID {0}", Thread.CurrentThread.ManagedThreadId);
            }

            stringBuilder.AppendFormat(": {0}", log);
            return stringBuilder.ToString();
        }

        private static string GetUnityColor(string msg, LogColor color)
        {
            if (color == LogColor.None)
            {
                return msg;
            }

            switch (color)
            {
                case LogColor.Cyan:
                    msg = $"<color=cyan>{msg}</color>";
                    break;
                case LogColor.Blue:
                    msg = $"<color=blue>{msg}</color>";
                    break;
                case LogColor.LightBlue:
                    msg = $"<color=lightblue>{msg}</color>";
                    break;
                case LogColor.Green:
                    msg = $"<color=green>{msg}</color>";
                    break;
                case LogColor.Orange:
                    msg = $"<color=orange>{msg}</color>";
                    break;
                case LogColor.Red:
                    msg = $"<color=red>{msg}</color>";
                    break;
                case LogColor.Yellow:
                    msg = $"<color=yellow>{msg}</color>";
                    break;
                case LogColor.Magenta:
                    msg = $"<color=magenta>{msg}</color>";
                    break;
                case LogColor.Grey:
                    msg = $"<color=grey>{msg}</color>";
                    break;
                case LogColor.Purple:
                    msg = $"<color=purple>{msg}</color>";
                    break;
            }

            return msg;
        }
    }
}