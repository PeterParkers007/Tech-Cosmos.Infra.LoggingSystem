using TechCosmos.LoggingSystem.Runtime.Interface;
using TechCosmos.LoggingSystem.Runtime.Struct;
using TechCosmos.LoggingSystem.Runtime.Enum;
namespace TechCosmos.LoggingSystem.Runtime.OutPut
{
    // Unity¿ØÖÆÌ¨Êä³ö
    public class UnityConsoleOutput : ILogOutput
    {
        public void Write(LogEntry entry)
        {
            string coloredMessage = $"<color={GetColor(entry.Level)}>{entry}</color>";

            switch (entry.Level)
            {
                case LogLevel.Warning:
                    UnityEngine.Debug.LogWarning(coloredMessage);
                    break;
                case LogLevel.Error:
                case LogLevel.Critical:
                    UnityEngine.Debug.LogError(coloredMessage);
                    break;
                default:
                    UnityEngine.Debug.Log(coloredMessage);
                    break;
            }
        }

        private string GetColor(LogLevel level) => level switch
        {
            LogLevel.Trace => "gray",
            LogLevel.Debug => "cyan",
            LogLevel.Info => "white",
            LogLevel.Warning => "yellow",
            LogLevel.Error => "orange",
            LogLevel.Critical => "red",
            _ => "white"
        };

        public void Flush() { }
        public void Dispose() { }
    }
}
