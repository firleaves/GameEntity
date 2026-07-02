using System;
using System.Collections.Generic;

namespace GameEntity
{
    /// <summary>
    /// 组合日志记录器 - 将日志输出到多个目标
    /// </summary>
    public class CompositeLogger : ILogger
    {
        private readonly List<ILogger> _loggers = new List<ILogger>();

        public CompositeLogger(params ILogger[] loggers)
        {
            _loggers.AddRange(loggers);
        }

        public void AddLogger(ILogger logger)
        {
            if (logger != null && !_loggers.Contains(logger))
            {
                _loggers.Add(logger);
            }
        }

        public void RemoveLogger(ILogger logger)
        {
            _loggers.Remove(logger);
        }

        public void SetLogLevel(bool debug, bool info, bool warning, bool error)
        {
            foreach (var logger in _loggers)
            {
                logger.SetLogLevel(debug, info, warning, error);
            }
        }

        public void Debug(object message)
        {
            foreach (var logger in _loggers)
            {
                logger.Debug(message);
            }
        }

        public void Info(object message)
        {
            foreach (var logger in _loggers)
            {
                logger.Info(message);
            }
        }

        public void Warning(object message)
        {
            foreach (var logger in _loggers)
            {
                logger.Warning(message);
            }
        }

        public void Error(object message)
        {
            foreach (var logger in _loggers)
            {
                logger.Error(message);
            }
        }

        public void Exception(Exception exception)
        {
            foreach (var logger in _loggers)
            {
                logger.Exception(exception);
            }
        }
    }
}
