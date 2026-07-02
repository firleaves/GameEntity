using System;
using System.IO;

namespace GameEntity
{
    /// <summary>
    /// 日志管理器 - 管理日志系统的生命周期
    /// </summary>
    public static class LogManager
    {
        private static FileLogger _fileLogger;
        private static bool _initialized = false;

        /// <summary>
        /// 初始化日志系统
        /// </summary>
        public static void Initialize(string logDirectory = null)
        {
            if (_initialized)
            {
                Console.WriteLine("LogManager already initialized");
                return;
            }

            try
            {
                // 生成日志文件路径
                logDirectory ??= GetDefaultLogDirectory();
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string logFileName = $"game_{timestamp}.log";
                string logFilePath = Path.Combine(logDirectory, logFileName);

                // 创建 FileLogger
                _fileLogger = new FileLogger(logFilePath);

                // 创建组合 Logger（Console + File）
                var compositeLogger = new CompositeLogger(
                    new ConsoleLogger(),
                    _fileLogger
                );

                // 替换全局 Logger
                Log.Logger = compositeLogger;

                _initialized = true;

                // 记录初始化成功
                Log.Info($"LogManager initialized. Log file: {logFilePath}");

                // 清理旧日志文件（可选）
                CleanupOldLogs(logDirectory, maxLogFiles: 10);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"LogManager initialization failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 关闭日志系统
        /// </summary>
        public static void Shutdown()
        {
            if (!_initialized)
            {
                return;
            }

            try
            {
                Log.Info("LogManager shutting down...");

                // 释放 FileLogger 资源
                _fileLogger?.Dispose();
                _fileLogger = null;

                // 恢复默认 Logger
                Log.Logger = new ConsoleLogger();

                _initialized = false;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"LogManager shutdown failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理旧日志文件，保留最近的 N 个
        /// </summary>
        private static void CleanupOldLogs(string logDirectory, int maxLogFiles)
        {
            try
            {
                if (!Directory.Exists(logDirectory))
                {
                    return;
                }

                var logFiles = Directory.GetFiles(logDirectory, "game_*.log");

                if (logFiles.Length <= maxLogFiles)
                {
                    return;
                }

                // 按创建时间排序
                Array.Sort(logFiles, (a, b) =>
                    File.GetCreationTime(b).CompareTo(File.GetCreationTime(a))
                );

                // 删除旧文件
                for (int i = maxLogFiles; i < logFiles.Length; i++)
                {
                    try
                    {
                        File.Delete(logFiles[i]);
                        Console.WriteLine($"Deleted old log file: {Path.GetFileName(logFiles[i])}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to delete log file {logFiles[i]}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Log cleanup failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取日志目录路径
        /// </summary>
        public static string GetLogDirectory()
        {
            return GetDefaultLogDirectory();
        }

        private static string GetDefaultLogDirectory()
        {
            return Path.Combine(AppContext.BaseDirectory, "Logs");
        }

        /// <summary>
        /// 获取所有日志文件
        /// </summary>
        public static string[] GetAllLogFiles()
        {
            string logDirectory = GetLogDirectory();
            if (!Directory.Exists(logDirectory))
            {
                return new string[0];
            }

            return Directory.GetFiles(logDirectory, "game_*.log");
        }
    }
}
