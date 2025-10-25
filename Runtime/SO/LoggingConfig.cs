using System.Collections.Generic;
using UnityEngine;
using TechCosmos.LoggingSystem.Runtime.Enum;
using TechCosmos.LoggingSystem.Runtime.Data;
namespace TechCosmos.LoggingSystem.Runtime.SO
{
    [CreateAssetMenu(fileName = "LoggingConfig", menuName = "Tech-Cosmos/Logging Config")]
    public class LoggingConfig : ScriptableObject
    {
        [Header("基础设置")]
        public LogLevel globalLogLevel = LogLevel.Info;
        public bool enableStackTrace = true;

        [Header("输出目标")]
        public List<LogOutput> outputs = new List<LogOutput> { LogOutput.UnityConsole };

        [Header("文件输出设置")]
        public string logFileName = "game_log";
        public int maxLogFiles = 5;
        public int maxFileSizeMB = 10;

        [Header("网络输出设置")]
        public string serverURL = "";
        public float sendInterval = 5f;

        [Header("分类过滤")]
        public List<LogCategory> categories = new List<LogCategory>();
    }
}


