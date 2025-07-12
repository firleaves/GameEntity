using System;

namespace GE
{
    public interface ILogger
    {
        void Debug(object message);
        void Info(object message);
        void Warning(object message);
        void Error(object message);
        void Exception(Exception exception);
        void SetLogLevel(bool debug, bool info, bool warning, bool error);
    }
} 