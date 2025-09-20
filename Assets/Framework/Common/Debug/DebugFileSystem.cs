using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Framework.Core.Singleton;
using UnityEngine;

namespace Framework.Common.Debug
{
    /// <summary>
    /// 日志文件调试类，主要用于保存日志文件到本地
    /// </summary>
    internal class DebugFileSystem : MonoGlobalSingleton<DebugFileSystem>
    {
        /// <summary>
        /// 日志数据类
        /// </summary>
        private class LogData
        {
            public readonly string Log;
            public readonly string Trace;
            public readonly LogType Type;

            public LogData(string log, string trace, LogType type)
            {
                Log = log;
                Trace = trace;
                Type = type;
            }
        }

        // 是否初始化
        private bool _init;

        // 文件写入流
        private StreamWriter _streamWriter;

        // 日志数据队列
        private readonly ConcurrentQueue<LogData> _concurrentQueue = new();

        // 工作信号事件，用于多线程同步
        private readonly ManualResetEvent _manualResetEvent = new(false);

        // 协程运行标识符
        private bool _threadRunning;

        public void InitConfig(DebugConfig debugConfig)
        {
            if (_init)
            {
                return;
            }

            _init = true;
            string logFilePath = Path.Combine(debugConfig.LogFilePath, debugConfig.LogFileName);
            _streamWriter = new StreamWriter(logFilePath);
            Application.logMessageReceivedThreaded += OnLogMessageReceivedThreaded;
            _threadRunning = true;
            Task.Run(WriteLogFile);
        }

        public override void OnSingletonInit()
        {
            base.OnSingletonInit();
            InitConfig(new DebugConfig());
        }

        public override void Dispose()
        {
            base.Dispose();
            if (!_init)
            {
                return;
            }

            Application.logMessageReceivedThreaded -= OnLogMessageReceivedThreaded;
            _threadRunning = false;
            _manualResetEvent.Reset();
            _streamWriter.Close();
            _streamWriter = null;
        }

        private void WriteLogFile()
        {
            while (_threadRunning)
            {
                _manualResetEvent.WaitOne(); // 让线程进入等待，并进行阻塞
                if (_streamWriter == null)
                {
                    break;
                }

                // 轮询获取Log数据
                LogData data;
                while (_concurrentQueue.Count > 0 && _concurrentQueue.TryDequeue(out data))
                {
                    if (data.Type == LogType.Log)
                    {
                        _streamWriter.Write("Log >>> ");
                        _streamWriter.WriteLine(data.Log);
                        _streamWriter.WriteLine(data.Trace);
                    }
                    else if (data.Type == LogType.Warning)
                    {
                        _streamWriter.Write("Warning >>> ");
                        _streamWriter.WriteLine(data.Log);
                        _streamWriter.WriteLine(data.Trace);
                    }
                    else if (data.Type == LogType.Error)
                    {
                        _streamWriter.Write("Error >>> ");
                        _streamWriter.WriteLine(data.Log);
                        _streamWriter.Write('\n');
                        _streamWriter.WriteLine(data.Trace);
                    }

                    _streamWriter.Write("\r\n");
                }

                // 保存当前文件内容，使其生效
                _streamWriter.Flush();
                _manualResetEvent.Reset();
                Thread.Sleep(1);
            }
        }

        private void OnLogMessageReceivedThreaded(string condition, string stackTrace, LogType type)
        {
            _concurrentQueue.Enqueue(
                new LogData(DateTime.Now.ToString("yyyy:MM:dd HH:mm:ss") + " " + condition, stackTrace, type)
            );
            _manualResetEvent.Set();
        }
    }
}