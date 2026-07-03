using System;

namespace GameEntity
{
    /// <summary>
    /// 库默认日志实现：保持静默，由宿主按需替换 Log.Logger。
    /// </summary>
    public sealed class NullLogger : ILogger
    {
        public static readonly NullLogger Instance = new NullLogger();

        private NullLogger()
        {
        }

        public void Debug(object message)
        {
        }

        public void Info(object message)
        {
        }

        public void Warning(object message)
        {
        }

        public void Error(object message)
        {
        }

        public void Exception(Exception exception)
        {
        }

        public void SetLogLevel(bool debug, bool info, bool warning, bool error)
        {
        }
    }
}
