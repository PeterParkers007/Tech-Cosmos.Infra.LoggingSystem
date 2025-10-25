using System;
using TechCosmos.LoggingSystem.Runtime.Enum;
namespace TechCosmos.LoggingSystem.Runtime.Struct
{
    [System.Serializable]
    public struct LogEntry
    {
        public string Message;          // 日志消息
        public LogLevel Level;          // 日志级别
        public string Category;         // 日志分类
        public DateTime Timestamp;      // 时间戳
        public string StackTrace;       // 调用堆栈
        public string SceneName;        // 场景名称
        public string ObjectName;       // 对象名称

        public override string ToString()
        {
            return $"[{Timestamp:HH:mm:ss}] [{Level}] [{Category}] {Message}";
        }
    }
}

