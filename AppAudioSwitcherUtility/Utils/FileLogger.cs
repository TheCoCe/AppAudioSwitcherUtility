using System;
using System.IO;
using System.Reflection;

namespace AppAudioSwitcherUtility.Utils
{
    public static class FileLogger
    {
        private const int MaxLogFiles = 10;
        
        public enum LogLevel { Debug, Info, Warning, Error }
        
        private static readonly object Lock = new object();
        private static StreamWriter _writer;

        private static string GetLogDirectory()
        {
            string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string parentDir = Directory.GetParent(exeDir)?.FullName ?? exeDir;
            return Path.Combine(parentDir, "logs");
        }

        private static string GetMainLogFilePath()
        {
            return Path.Combine(GetLogDirectory(), "AppAudioSwitcherUtility.log");
        }

        private static void RotateLogFiles()
        {
            string logDir = GetLogDirectory();

            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            string mainLogPath = GetMainLogFilePath();

            // AppAudioSwitcherUtility.0.log -> AppAudioSwitcherUtility.1.log
            for (int i = MaxLogFiles - 1; i >= 1; i--)
            {
                string sourcePath = Path.Combine(logDir, $"AppAudioSwitcherUtility.{i - 1}.log");
                string targetPath = Path.Combine(logDir, $"AppAudioSwitcherUtility.{i}.log");

                if (!File.Exists(sourcePath)) continue;
                if (File.Exists(targetPath))
                {
                    File.Delete(targetPath);
                }

                File.Move(sourcePath, targetPath);
            }

            // Move the previous main log to AppAudioSwitcherUtility.0.log
            if (!File.Exists(mainLogPath)) return;
            string firstArchivePath = Path.Combine(logDir, "AppAudioSwitcherUtility.0.log");

            if (File.Exists(firstArchivePath))
            {
                File.Delete(firstArchivePath);
            }

            File.Move(mainLogPath, firstArchivePath);
        }
        
        public static void Init()
        {
            RotateLogFiles();

            string mainLogPath = GetMainLogFilePath();
            try
            {
                _writer = new StreamWriter(mainLogPath, append: false)
                {
                    AutoFlush = true
                };
                Console.WriteLine($"Starting new log file at {mainLogPath}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to initialize log file: {ex.Message}");
                _writer = null;
                return;
            }
            
            AppDomain.CurrentDomain.ProcessExit += (s, e) => Shutdown();
            Console.CancelKeyPress += (s, e) =>
            {
                Shutdown();
                e.Cancel = false;
            };
        }

        private static string GetLogString(LogLevel level)
        {
            string logLevelString = Enum.GetName(typeof(LogLevel), level);
            return (logLevelString ?? "Unknown").PadRight(7).ToUpper();
        }

        public static void Log(LogLevel level, string message)
        {
            string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {GetLogString(level)} {message}";
            lock (Lock)
            {
                _writer?.WriteLine(line);
            }

            if (level == LogLevel.Error)
            {
                Console.Error.WriteLine(line);
            }
            else
            {
                Console.WriteLine(line);
            }
        }

        public static void LogDebug(string message)
        {
            #if DEBUG
             Log(LogLevel.Debug, message);
            #endif
        }
        public static void LogInfo(string message) { Log(LogLevel.Info, message); }
        public static void LogWarning(string message) { Log(LogLevel.Warning, message); }
        public static void LogError(string message) { Log(LogLevel.Error, message); }

        public static void Shutdown()
        {
            lock (Lock)
            {
                if (_writer == null) return;
                _writer.Flush();
                _writer.Dispose();
                _writer = null;
            }
        }
    }
}