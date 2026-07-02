using System;

namespace GameEntity
{
    /// <summary>
    /// 纯 C# 默认日志输出，不依赖 UnityEngine。
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        private bool _enableDebugLog = true;
        private bool _enableInfoLog = true;
        private bool _enableWarningLog = true;
        private bool _enableErrorLog = true;

        public void SetLogLevel(bool debug, bool info, bool warning, bool error)
        {
            _enableDebugLog = debug;
            _enableInfoLog = info;
            _enableWarningLog = warning;
            _enableErrorLog = error;
        }

        public void Debug(object message)
        {
            if (_enableDebugLog) Console.WriteLine($"[DEBUG] {message}");
        }

        public void Info(object message)
        {
            if (_enableInfoLog) Console.WriteLine($"[INFO] {message}");
        }

        public void Warning(object message)
        {
            if (_enableWarningLog) Console.WriteLine($"[WARNING] {message}");
        }

        public void Error(object message)
        {
            if (_enableErrorLog) Console.Error.WriteLine($"[ERROR] {message}");
        }

        public void Exception(Exception exception)
        {
            if (_enableErrorLog) Console.Error.WriteLine(exception);
        }
    }
}
