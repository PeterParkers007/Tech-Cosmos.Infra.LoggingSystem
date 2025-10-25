using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using System.Linq;
using TechCosmos.LoggingSystem.Runtime.Interface;
using TechCosmos.LoggingSystem.Runtime.SO;
using TechCosmos.LoggingSystem.Runtime.Struct;
using TechCosmos.LoggingSystem.Runtime.Data;
using TechCosmos.LoggingSystem.Runtime.Enum;
namespace TechCosmos.LoggingSystem.Runtime.OutPut
{
    public class DatabaseOutput : ILogOutput
    {
        private string dbPath;
        private List<LogRecord> logCache = new List<LogRecord>();
        private bool initialized = false;
        private const int MAX_CACHE_SIZE = 100; // 内存缓存100条后写入文件

        public DatabaseOutput(LoggingConfig config)
        {
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            try
            {
                dbPath = Path.Combine(Application.persistentDataPath, "Logs");
                if (!Directory.Exists(dbPath))
                    Directory.CreateDirectory(dbPath);

                initialized = true;
                LoggingManager.Instance.Info($"文件数据库初始化完成: {dbPath}", "Logging");
            }
            catch (Exception ex)
            {
                LoggingManager.Instance.Error($"数据库初始化失败: {ex.Message}", "Logging");
            }
        }

        public void Write(LogEntry entry)
        {
            if (!initialized) return;

            var record = ConvertToRecord(entry);
            logCache.Add(record);

            // 缓存达到一定数量后写入文件
            if (logCache.Count >= MAX_CACHE_SIZE)
            {
                WriteCacheToFile();
            }
        }

        private void WriteCacheToFile()
        {
            if (logCache.Count == 0) return;

            try
            {
                string filePath = Path.Combine(dbPath, $"logs_{DateTime.Now:yyyyMMdd_HHmmss}.json");

                // 将日志转换为 JSON
                string json = JsonUtility.ToJson(new LogRecordWrapper { logs = logCache.ToArray() }, true);
                File.WriteAllText(filePath, json);

                logCache.Clear();

                // 清理旧文件（保留最近7天的日志）
                CleanOldFiles();
            }
            catch (Exception ex)
            {
                LoggingManager.Instance.Error($"文件写入失败: {ex.Message}", "Logging");
            }
        }

        private LogRecord ConvertToRecord(LogEntry entry)
        {
            return new LogRecord
            {
                Id = Guid.NewGuid().ToString(),
                Message = entry.Message,
                Level = entry.Level.ToString(),
                Category = entry.Category,
                Timestamp = entry.Timestamp,
                StackTrace = entry.StackTrace,
                SceneName = entry.SceneName,
                ObjectName = entry.ObjectName,
                DeviceId = SystemInfo.deviceUniqueIdentifier,
                AppVersion = Application.version
            };
        }

        private void CleanOldFiles()
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-7);
                var files = Directory.GetFiles(dbPath, "logs_*.json");

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingManager.Instance.Warn($"清理旧文件失败: {ex.Message}", "Logging");
            }
        }

        // 查询日志
        public List<LogRecord> QueryLogs(DateTime start, DateTime end, string category = null, LogLevel minLevel = LogLevel.Trace)
        {
            if (!initialized) return new List<LogRecord>();

            var results = new List<LogRecord>();
            var files = Directory.GetFiles(dbPath, "logs_*.json");

            foreach (var file in files)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var wrapper = JsonUtility.FromJson<LogRecordWrapper>(json);

                    var filteredLogs = wrapper.logs.Where(log =>
                        log.Timestamp >= start &&
                        log.Timestamp <= end &&
                        GetLevelValue(log.Level) >= (int)minLevel &&
                        (category == null || log.Category == category)
                    );

                    results.AddRange(filteredLogs);
                }
                catch (Exception ex)
                {
                    LoggingManager.Instance.Warn($"读取日志文件失败: {file}, {ex.Message}", "Logging");
                }
            }

            return results.OrderByDescending(x => x.Timestamp).ToList();
        }

        private int GetLevelValue(string level) => level switch
        {
            "Trace" => 0,
            "Debug" => 1,
            "Info" => 2,
            "Warning" => 3,
            "Error" => 4,
            "Critical" => 5,
            _ => 2
        };

        public void Flush()
        {
            WriteCacheToFile();
        }

        public void Dispose()
        {
            Flush();
        }
    }
}




