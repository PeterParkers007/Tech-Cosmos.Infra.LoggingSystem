using System.Collections.Generic;
using UnityEngine;
using TechCosmos.LoggingSystem.Runtime.Enum;
using TechCosmos.LoggingSystem.Runtime.Data;
namespace TechCosmos.LoggingSystem.Runtime.SO
{
    [CreateAssetMenu(fileName = "LoggingConfig", menuName = "Tech-Cosmos/Logging Config")]
    public class LoggingConfig : ScriptableObject
    {
        [Header("��������")]
        public LogLevel globalLogLevel = LogLevel.Info;
        public bool enableStackTrace = true;

        [Header("���Ŀ��")]
        public List<LogOutput> outputs = new List<LogOutput> { LogOutput.UnityConsole };

        [Header("�ļ��������")]
        public string logFileName = "game_log";
        public int maxLogFiles = 5;
        public int maxFileSizeMB = 10;

        [Header("�����������")]
        public string serverURL = "";
        public float sendInterval = 5f;

        [Header("�������")]
        public List<LogCategory> categories = new List<LogCategory>();
    }
}


