using UnityEngine;
using TechCosmos.LoggingSystem.Runtime.Enum;
namespace TechCosmos.LoggingSystem.Runtime.Data
{
    [System.Serializable]
    public class LogCategory
    {
        public string name;
        public LogLevel minLevel = LogLevel.Info;
        public Color color = Color.white;
    }
}
