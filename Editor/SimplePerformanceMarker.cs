using System.Diagnostics;
using UnityEngine;

namespace TechCosmos.LoggingSystem.Runtime.Tools
{
    public static class SimplePerformanceMarker
    {
        private static Stopwatch stopwatch = new Stopwatch();

        public static void Begin(string markerName)
        {
            stopwatch.Restart();
        }

        public static float End(string markerName)
        {
            stopwatch.Stop();
            float elapsedMs = stopwatch.ElapsedMilliseconds;

            // 自动记录到日志
            if (LoggingManager.Instance != null && elapsedMs > 16.7f)
            {
                LoggingManager.Instance.Warn($"性能: {markerName} 耗时 {elapsedMs:F1}ms", "Performance");
            }

            return elapsedMs;
        }

        public static float Profile(System.Action action, string markerName)
        {
            Begin(markerName);
            action?.Invoke();
            return End(markerName);
        }
    }
}