using System;
using TechCosmos.LoggingSystem.Runtime.Enum;
namespace TechCosmos.LoggingSystem.Runtime.Struct
{
    [System.Serializable]
    public struct LogEntry
    {
        public string Message;          // ��־��Ϣ
        public LogLevel Level;          // ��־����
        public string Category;         // ��־����
        public DateTime Timestamp;      // ʱ���
        public string StackTrace;       // ���ö�ջ
        public string SceneName;        // ��������
        public string ObjectName;       // ��������

        public override string ToString()
        {
            return $"[{Timestamp:HH:mm:ss}] [{Level}] [{Category}] {Message}";
        }
    }
}

