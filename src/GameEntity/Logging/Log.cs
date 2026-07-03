using System;
using System.Diagnostics;

namespace GameEntity
{
    public static class Log
    {
        private static ILogger _logger = NullLogger.Instance;

        public static ILogger Logger
        {
            get => _logger;
            set => _logger = value ?? throw new ArgumentNullException(nameof(value));
        }

        public static void SetLogLevel(bool debug, bool info, bool warning, bool error)
        {
            _logger.SetLogLevel(debug, info, warning, error);
        }

        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_DEBUG_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        public static void Debug(object message)
        {
            _logger.Debug(message);
        }

        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_INFO_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        public static void Info(object message)
        {
            _logger.Info(message);
        }

        public static void Warning(object message)
        {
            _logger.Warning(message);
        }

        public static void Error(object message)
        {
            _logger.Error(message);
        }

        public static void Exception(Exception exception)
        {
            _logger.Exception(exception);
        }
    }
}
