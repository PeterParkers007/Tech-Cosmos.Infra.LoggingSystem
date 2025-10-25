// 文件输出
using System.IO;
using System;
using UnityEngine;
using TechCosmos.LoggingSystem.Runtime.Interface;
using TechCosmos.LoggingSystem.Runtime.SO;
using TechCosmos.LoggingSystem.Runtime.Struct;
using TechCosmos.LoggingSystem.Runtime.Enum;
namespace TechCosmos.LoggingSystem.Runtime.OutPut
{
    public class FileOutput : ILogOutput
    {
        private StreamWriter writer;
        private string logFilePath;
        private LoggingConfig config;

        public FileOutput(LoggingConfig config)
        {
            this.config = config;
            InitializeFile();
        }

        private void InitializeFile()
        {
            string directory = Path.Combine(Application.persistentDataPath, "Logs");
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            logFilePath = Path.Combine(directory, $"{config.logFileName}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            writer = new StreamWriter(logFilePath, true);

            Write(new LogEntry
            {
                Message = "=== Log Session Started ===",
                Level = LogLevel.Info,
                Timestamp = DateTime.Now
            });
        }

        public void Write(LogEntry entry)
        {
            writer.WriteLine(entry.ToString());
            if (!string.IsNullOrEmpty(entry.StackTrace))
                writer.WriteLine($"Stack: {entry.StackTrace}");

            CheckFileSize();
        }

        private void CheckFileSize()
        {
            if (writer.BaseStream.Length > config.maxFileSizeMB * 1024 * 1024)
            {
                Flush();
                Dispose();
                InitializeFile();
                CleanOldFiles();
            }
        }

        private void CleanOldFiles()
        {
            // 清理旧日志文件逻辑
        }

        public void Flush() => writer?.Flush();
        public void Dispose() => writer?.Dispose();
    }
}
