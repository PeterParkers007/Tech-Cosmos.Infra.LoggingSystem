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

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeOutputters();
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

            logQueue.Enqueue(entry);

            // 立即处理或批量处理
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

        // 便捷方法
        public void Trace(string message, string category = "General") => Log(message, LogLevel.Trace, category);
        public void Debug(string message, string category = "General") => Log(message, LogLevel.Debug, category);
        public void Info(string message, string category = "General") => Log(message, LogLevel.Info, category);
        public void Warn(string message, string category = "General") => Log(message, LogLevel.Warning, category);
        public void Error(string message, string category = "General") => Log(message, LogLevel.Error, category);
        public void Critical(string message, string category = "General") => Log(message, LogLevel.Critical, category);
    }
}
