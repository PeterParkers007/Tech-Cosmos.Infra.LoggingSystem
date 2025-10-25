using TechCosmos.LoggingSystem.Runtime.Struct;

namespace TechCosmos.LoggingSystem.Runtime.Interface
{
    public interface ILogOutput
    {
        void Write(LogEntry entry);
        void Flush();
        void Dispose();
    }
}
