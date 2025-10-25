namespace TechCosmos.LoggingSystem.Runtime.Enum
{
    public enum LogLevel
    {
        Trace,      // 追踪 - 最详细的调试信息
        Debug,      // 调试 - 开发期调试信息  
        Info,       // 信息 - 正常运行信息
        Warning,    // 警告 - 不影响运行的异常
        Error,      // 错误 - 影响功能的错误
        Critical    // 严重 - 导致系统崩溃的错误
    }
}
