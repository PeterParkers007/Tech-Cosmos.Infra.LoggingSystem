using System;
namespace TechCosmos.LoggingSystem.Runtime.Data
{
    [System.Serializable]
    public class LogRecord
    {
        public string Id;
        public string Message;
        public string Level;
        public string Category;
        public DateTime Timestamp;
        public string StackTrace;
        public string SceneName;
        public string ObjectName;
        public string DeviceId;
        public string AppVersion;
    }
}
