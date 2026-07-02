using System;
using UnityEngine;

namespace GameEntity.Unity
{
    /// <summary>
    /// GameEntity 日志到 Unity Console 的桥接。
    /// </summary>
    public sealed class UnityGameEntityLogger : GameEntity.ILogger
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
            if (_enableDebugLog) UnityEngine.Debug.Log($"[DEBUG] {message}");
        }

        public void Info(object message)
        {
            if (_enableInfoLog) UnityEngine.Debug.Log($"[INFO] {message}");
        }

        public void Warning(object message)
        {
            if (_enableWarningLog) UnityEngine.Debug.LogWarning($"[WARNING] {message}");
        }

        public void Error(object message)
        {
            if (_enableErrorLog) UnityEngine.Debug.LogError($"[ERROR] {message}");
        }

        public void Exception(Exception exception)
        {
            if (_enableErrorLog) UnityEngine.Debug.LogException(exception);
        }
    }
}
