using UnityEngine;
using System;
using System.Diagnostics;

namespace GE
{
    public static class Log
    {
        private static ILogger _logger = new UnityLogger();

        public static ILogger Logger
        {
            get => _logger;
            set => _logger = value ?? throw new ArgumentNullException(nameof(value));
        }

        public static void SetLogLevel(bool debug, bool info, bool warning, bool error)
        {
            if (_logger == null) return;
            _logger.SetLogLevel(debug, info, warning, error);
        }

        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_DEBUG_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        public static void Debug(object message)
        {
            if (_logger == null) return;
            _logger.Debug(message);
        }

        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_INFO_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        public static void Info(object message)
        {
            if (_logger == null) return;
            _logger.Info(message);
        }

        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_WARNING_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        public static void Warning(object message)
        {
            if (_logger == null) return;
            _logger.Warning(message);
        }

        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_ERROR_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        public static void Error(object message)
        {
            if (_logger == null) return;
            _logger.Error(message);
        }

        // Exception 始终启用，不使用条件编译
        public static void Exception(Exception exception)
        {
            if (_logger == null) return;
            _logger.Exception(exception);
        }


    }
}