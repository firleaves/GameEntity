using System;
using System.IO;
using System.Text;

namespace GameEntity
{
    /// <summary>
    /// 文件日志记录器 - 将日志写入本地文件
    /// </summary>
    public class FileLogger : ILogger, IDisposable
    {
        private readonly StreamWriter _writer;
        private readonly string _filePath;
        private bool _enableDebugLog = true;
        private bool _enableInfoLog = true;
        private bool _enableWarningLog = true;
        private bool _enableErrorLog = true;
        private bool _disposed = false;

        public FileLogger(string filePath)
        {
            _filePath = filePath;

            // 确保目录存在
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 创建 StreamWriter，启用自动刷新
            _writer = new StreamWriter(filePath, append: false, Encoding.UTF8)
            {
                AutoFlush = true // 确保日志立即写入磁盘
            };

            // 写入文件头
            _writer.WriteLine("=".PadRight(80, '='));
            _writer.WriteLine($"Log Session Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            _writer.WriteLine($".NET Version: {Environment.Version}");
            _writer.WriteLine($"Platform: {Environment.OSVersion}");
            _writer.WriteLine("=".PadRight(80, '='));
            _writer.WriteLine();
        }

        public void SetLogLevel(bool debug, bool info, bool warning, bool error)
        {
            _enableDebugLog = debug;
            _enableInfoLog = info;
            _enableWarningLog = warning;
            _enableErrorLog = error;
        }

        public void Debug(object message)
        {
            if (_enableDebugLog && !_disposed)
            {
                WriteLog("DEBUG", message);
            }
        }

        public void Info(object message)
        {
            if (_enableInfoLog && !_disposed)
            {
                WriteLog("INFO", message);
            }
        }

        public void Warning(object message)
        {
            if (_enableWarningLog && !_disposed)
            {
                WriteLog("WARNING", message);
            }
        }

        public void Error(object message)
        {
            if (_enableErrorLog && !_disposed)
            {
                WriteLog("ERROR", message);
            }
        }

        public void Exception(Exception exception)
        {
            if (_enableErrorLog && !_disposed)
            {
                WriteLog("EXCEPTION", $"{exception.GetType().Name}: {exception.Message}\n{exception.StackTrace}");
            }
        }

        private void WriteLog(string level, object message)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string logLine = $"[{timestamp}] [{level}] {message}";
                _writer.WriteLine(logLine);
            }
            catch (Exception ex)
            {
                // 文件写入失败时输出到控制台
                Console.Error.WriteLine($"FileLogger write failed: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                // 写入文件尾
                _writer.WriteLine();
                _writer.WriteLine("=".PadRight(80, '='));
                _writer.WriteLine($"Log Session Ended: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                _writer.WriteLine("=".PadRight(80, '='));

                _writer.Flush();
                _writer.Close();
                _writer.Dispose();
            }
        }

        ~FileLogger()
        {
            Dispose();
        }
    }
}
