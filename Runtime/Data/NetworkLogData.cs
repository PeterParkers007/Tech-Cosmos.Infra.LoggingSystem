using System.Collections.Generic;
using TechCosmos.LoggingSystem.Runtime.Struct;
namespace TechCosmos.LoggingSystem.Runtime.Data
{
    [System.Serializable]
    public class NetworkLogData
    {
        public string deviceId;
        public string appVersion;
        public string platform;
        public List<LogEntry> logs;
    }
}
