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
        private const int MAX_CACHE_SIZE = 100; // �ڴ滺��100����д���ļ�

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
                LoggingManager.Instance.Info($"�ļ����ݿ��ʼ�����: {dbPath}", "Logging");
            }
            catch (Exception ex)
            {
                LoggingManager.Instance.Error($"���ݿ��ʼ��ʧ��: {ex.Message}", "Logging");
            }
        }

        public void Write(LogEntry entry)
        {
            if (!initialized) return;

            var record = ConvertToRecord(entry);
            logCache.Add(record);

            // ����ﵽһ��������д���ļ�
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

                // ����־ת��Ϊ JSON
                string json = JsonUtility.ToJson(new LogRecordWrapper { logs = logCache.ToArray() }, true);
                File.WriteAllText(filePath, json);

                logCache.Clear();

                // ������ļ����������7�����־��
                CleanOldFiles();
            }
            catch (Exception ex)
            {
                LoggingManager.Instance.Error($"�ļ�д��ʧ��: {ex.Message}", "Logging");
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
                LoggingManager.Instance.Warn($"������ļ�ʧ��: {ex.Message}", "Logging");
            }
        }

        // ��ѯ��־
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
                    LoggingManager.Instance.Warn($"��ȡ��־�ļ�ʧ��: {file}, {ex.Message}", "Logging");
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




