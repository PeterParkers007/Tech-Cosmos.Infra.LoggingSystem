using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;
using UnityEngine;
using TechCosmos.LoggingSystem.Runtime.SO;
using TechCosmos.LoggingSystem.Runtime.OutPut;
using TechCosmos.LoggingSystem.Runtime.Interface;
using TechCosmos.LoggingSystem.Runtime.Enum;
using TechCosmos.LoggingSystem.Runtime.Struct;

namespace TechCosmos.LoggingSystem.Runtime
{
    public class LoggingManager : MonoBehaviour
    {
        [SerializeField] private LoggingConfig config;

        // 输出器集合
        private List<ILogOutput> outputters = new List<ILogOutput>();
        private Queue<LogEntry> logQueue = new Queue<LogEntry>();

        public static LoggingManager Instance { get; private set; }

        // 新增：实时日志事件
        public static event Action<LogEntry> OnLogReceived;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeOutputters();
                // 记录日志系统启动
                LogInternal("日志系统启动", LogLevel.Info, "Logging");
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeOutputters()
        {
            foreach (var output in config.outputs)
            {
                ILogOutput outputter = output switch
                {
                    LogOutput.UnityConsole => new UnityConsoleOutput(),
                    LogOutput.File => new FileOutput(config),
                    LogOutput.Network => new NetworkOutput(config),
                    LogOutput.Database => new DatabaseOutput(config),
                    _ => new UnityConsoleOutput()
                };
                outputters.Add(outputter);
            }
        }

        public void Log(string message, LogLevel level = LogLevel.Info, string category = "General")
        {
            // 级别过滤
            if (level < config.globalLogLevel) return;

            // 分类过滤
            var categoryConfig = config.categories.Find(c => c.name == category);
            if (categoryConfig != null && level < categoryConfig.minLevel) return;

            var entry = new LogEntry
            {
                Message = message,
                Level = level,
                Category = category,
                Timestamp = DateTime.Now,
                StackTrace = config.enableStackTrace ? StackTraceUtility.ExtractStackTrace() : "",
                SceneName = SceneManager.GetActiveScene().name,
                ObjectName = gameObject.name
            };

            ProcessLogEntry(entry);
        }

        // 新增：内部日志方法（不过滤）
        private void LogInternal(string message, LogLevel level, string category)
        {
            var entry = new LogEntry
            {
                Message = message,
                Level = level,
                Category = category,
                Timestamp = DateTime.Now,
                SceneName = SceneManager.GetActiveScene().name,
                ObjectName = "LoggingSystem"
            };

            ProcessLogEntry(entry);
        }

        // 新增：处理日志条目（包含实时推送）
        private void ProcessLogEntry(LogEntry entry)
        {
            logQueue.Enqueue(entry);

            // 触发实时日志事件
            OnLogReceived?.Invoke(entry);

            ProcessLogQueue();
        }

        private void ProcessLogQueue()
        {
            while (logQueue.Count > 0)
            {
                var entry = logQueue.Dequeue();
                foreach (var outputter in outputters)
                {
                    outputter.Write(entry);
                }
            }
        }

        // 便捷方法 - 修复：调用公共Log方法
        public void Trace(string message, string category = "General") => Log(message, LogLevel.Trace, category);
        public void Debug(string message, string category = "General") => Log(message, LogLevel.Debug, category);
        public void Info(string message, string category = "General") => Log(message, LogLevel.Info, category);
        public void Warn(string message, string category = "General") => Log(message, LogLevel.Warning, category);
        public void Error(string message, string category = "General") => Log(message, LogLevel.Error, category);
        public void Critical(string message, string category = "General") => Log(message, LogLevel.Critical, category);

        // 新增：性能标记方法
        [System.Diagnostics.Conditional("ENABLE_PERFORMANCE_LOGGING")]
        public static void MarkPerformance(string marker, float timeMs, string category = "Performance")
        {
            if (Instance != null && timeMs > 16.7f) // 超过一帧时间
            {
                Instance.Warn($"性能警告: {marker} 耗时 {timeMs:F1}ms", category);
            }
        }

        // 新增：行为记录方法
        public static void LogBehavior(string behavior, string details = "", string playerId = "")
        {
            if (Instance != null)
            {
                Instance.Info($"BEHAVIOR: {behavior} - {details}", "Behavior");
            }
        }
    }
}