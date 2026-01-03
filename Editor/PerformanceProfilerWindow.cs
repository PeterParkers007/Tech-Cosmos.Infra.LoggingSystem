using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEditor;
using TechCosmos.LoggingSystem.Runtime.Struct;
using TechCosmos.LoggingSystem.Runtime;

namespace TechCosmos.LoggingSystem.Editor.Tools
{
    public class PerformanceProfilerWindow : EditorWindow
    {
        private Vector2 scrollPos;

        // 性能数据
        private Dictionary<string, PerformanceData> performanceData = new Dictionary<string, PerformanceData>();
        private List<PerformanceHotspot> hotspots = new List<PerformanceHotspot>();

        // 阈值设置
        private float warningThresholdMs = 16.7f; // 60fps的一帧时间
        private float criticalThresholdMs = 33.3f; // 30fps的一帧时间
        private int minSampleCount = 10;

        // 采样设置
        private bool isProfiling = false;
        private float profileDuration = 60f; // 采样60秒
        private float elapsedTime = 0f;

        [MenuItem("Tech-Cosmos/LoggingSystem/性能热点分析")]
        public static void ShowWindow()
        {
            GetWindow<PerformanceProfilerWindow>("性能分析");
        }

        private void OnEnable()
        {
            // 订阅更新
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (isProfiling && EditorApplication.isPlaying)
            {
                elapsedTime += Time.deltaTime;

                if (elapsedTime >= profileDuration)
                {
                    StopProfiling();
                }

                Repaint();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("性能热点自动标记", EditorStyles.boldLabel);

            DrawControls();

            EditorGUILayout.Space();

            if (hotspots.Count > 0)
            {
                DrawHotspots();
            }
            else if (isProfiling)
            {
                EditorGUILayout.HelpBox($"采集中... {elapsedTime:F1}/{profileDuration:F0}秒", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("点击开始采集性能数据", MessageType.Info);
            }
        }

        private void DrawControls()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            // 控制按钮
            if (!isProfiling)
            {
                if (GUILayout.Button("开始采集", GUILayout.Width(100)))
                {
                    StartProfiling();
                }
            }
            else
            {
                if (GUILayout.Button("停止采集", GUILayout.Width(100)))
                {
                    StopProfiling();
                }
            }

            // 阈值设置
            EditorGUILayout.LabelField("警告阈值(ms):", GUILayout.Width(90));
            warningThresholdMs = EditorGUILayout.FloatField(warningThresholdMs, GUILayout.Width(60));

            EditorGUILayout.LabelField("严重阈值(ms):", GUILayout.Width(90));
            criticalThresholdMs = EditorGUILayout.FloatField(criticalThresholdMs, GUILayout.Width(60));

            // 采样时间
            EditorGUILayout.LabelField("采样时间(s):", GUILayout.Width(70));
            profileDuration = EditorGUILayout.FloatField(profileDuration, GUILayout.Width(60));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("清除数据", GUILayout.Width(80)))
            {
                ClearData();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawHotspots()
        {
            EditorGUILayout.LabelField($"发现 {hotspots.Count} 个性能热点", EditorStyles.boldLabel);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // 按严重程度排序
            var sortedHotspots = hotspots
                .OrderByDescending(h => h.Severity)
                .ThenByDescending(h => h.AverageTimeMs)
                .ToList();

            foreach (var hotspot in sortedHotspots)
            {
                DrawHotspotItem(hotspot);
            }

            EditorGUILayout.EndScrollView();

            // 导出按钮
            EditorGUILayout.Space();
            if (GUILayout.Button("导出性能报告"))
            {
                ExportPerformanceReport();
            }
        }

        private void DrawHotspotItem(PerformanceHotspot hotspot)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 标题行
            EditorGUILayout.BeginHorizontal();

            // 严重程度指示器
            string severityText = hotspot.Severity switch
            {
                PerformanceSeverity.Critical => "[严重]",
                PerformanceSeverity.Warning => "[警告]",
                _ => "[注意]"
            };

            Color severityColor = hotspot.Severity switch
            {
                PerformanceSeverity.Critical => Color.red,
                PerformanceSeverity.Warning => Color.yellow,
                _ => Color.white
            };

            GUI.color = severityColor;
            EditorGUILayout.LabelField(severityText, GUILayout.Width(50));
            GUI.color = Color.white;

            EditorGUILayout.LabelField(hotspot.Category, EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            EditorGUILayout.LabelField($"{hotspot.AverageTimeMs:F2}ms", GUILayout.Width(80));
            EditorGUILayout.LabelField($"x{hotspot.SampleCount}", GUILayout.Width(40));

            EditorGUILayout.EndHorizontal();

            // 详细信息
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField($"位置: {hotspot.Location}");
            EditorGUILayout.LabelField($"调用次数: {hotspot.SampleCount}");
            EditorGUILayout.LabelField($"总耗时: {hotspot.TotalTimeMs:F2}ms");
            EditorGUILayout.LabelField($"单次耗时范围: {hotspot.MinTimeMs:F2}ms - {hotspot.MaxTimeMs:F2}ms");

            // 时间分布图
            if (hotspot.TimeSamples.Count > 1)
            {
                EditorGUILayout.LabelField("时间趋势:");
                DrawTimeChart(hotspot.TimeSamples);
            }

            // 建议
            if (hotspot.Suggestions.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("优化建议:");
                foreach (var suggestion in hotspot.Suggestions)
                {
                    EditorGUILayout.LabelField($"• {suggestion}", EditorStyles.wordWrappedMiniLabel);
                }
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawTimeChart(List<float> timeSamples)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 40);

            if (timeSamples.Count < 2) return;

            float maxTime = timeSamples.Max();
            float minTime = timeSamples.Min();
            float range = maxTime - minTime;
            if (range < 0.1f) range = 0.1f;

            float width = rect.width / (timeSamples.Count - 1);

            // 绘制阈值线
            float warningY = rect.y + rect.height * (1 - warningThresholdMs / (maxTime * 1.2f));
            float criticalY = rect.y + rect.height * (1 - criticalThresholdMs / (maxTime * 1.2f));

            EditorGUI.DrawRect(new Rect(rect.x, warningY, rect.width, 1), Color.yellow);
            EditorGUI.DrawRect(new Rect(rect.x, criticalY, rect.width, 1), Color.red);

            // 绘制曲线
            Handles.BeginGUI();
            Vector3[] points = new Vector3[timeSamples.Count];

            for (int i = 0; i < timeSamples.Count; i++)
            {
                float normalized = (timeSamples[i] - minTime) / range;
                float y = rect.y + rect.height * (1 - normalized);
                float x = rect.x + i * width;
                points[i] = new Vector3(x, y, 0);
            }

            Handles.color = Color.green;
            Handles.DrawAAPolyLine(2f, points);

            Handles.EndGUI();
        }

        private void StartProfiling()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("提示", "请在运行模式下进行性能分析", "确定");
                return;
            }

            ClearData();
            isProfiling = true;
            elapsedTime = 0f;

            // 开始监听性能日志
            // 需要扩展LoggingManager以支持性能标记
            PerformanceMarker.StartProfiling();
        }

        private void StopProfiling()
        {
            isProfiling = false;
            PerformanceMarker.StopProfiling();

            // 分析数据
            AnalyzePerformanceData();

            // 生成热点报告
            DetectHotspots();
        }

        private void AnalyzePerformanceData()
        {
            // 这里需要从性能日志中分析数据
            // 实际实现需要与LoggingManager集成
        }

        private void DetectHotspots()
        {
            hotspots.Clear();

            foreach (var kvp in performanceData)
            {
                var data = kvp.Value;

                if (data.SampleCount < minSampleCount)
                    continue;

                float avgTime = data.TotalTime / data.SampleCount;

                PerformanceSeverity severity = avgTime switch
                {
                    float t when t >= criticalThresholdMs => PerformanceSeverity.Critical,
                    float t when t >= warningThresholdMs => PerformanceSeverity.Warning,
                    _ => PerformanceSeverity.Normal
                };

                if (severity > PerformanceSeverity.Normal)
                {
                    var hotspot = new PerformanceHotspot
                    {
                        Category = data.Category,
                        Location = kvp.Key,
                        AverageTimeMs = avgTime * 1000,
                        TotalTimeMs = data.TotalTime * 1000,
                        MinTimeMs = data.MinTime * 1000,
                        MaxTimeMs = data.MaxTime * 1000,
                        SampleCount = data.SampleCount,
                        Severity = severity,
                        TimeSamples = data.TimeSamples.Select(t => t * 1000).ToList(),
                        Suggestions = GenerateSuggestions(data)
                    };

                    hotspots.Add(hotspot);
                }
            }
        }

        private List<string> GenerateSuggestions(PerformanceData data)
        {
            var suggestions = new List<string>();

            if (data.SampleCount > 100 && data.AverageTime > 0.01f)
            {
                suggestions.Add("高频调用，考虑缓存或批量处理");
            }

            if (data.MaxTime > 0.1f)
            {
                suggestions.Add("存在峰值性能问题，检查是否偶发阻塞操作");
            }

            if (data.Category.Contains("Physics"))
            {
                suggestions.Add("物理相关，检查碰撞体数量和复杂度");
            }

            if (data.Category.Contains("Render"))
            {
                suggestions.Add("渲染相关，检查DrawCall和面数");
            }

            if (data.Category.Contains("Network"))
            {
                suggestions.Add("网络相关，考虑数据压缩或减少请求频率");
            }

            return suggestions;
        }

        private void ClearData()
        {
            performanceData.Clear();
            hotspots.Clear();
        }

        private void ExportPerformanceReport()
        {
            string reportPath = EditorUtility.SaveFilePanel("导出性能报告", Application.dataPath,
                $"PerformanceReport_{System.DateTime.Now:yyyyMMdd_HHmmss}", "txt");

            if (!string.IsNullOrEmpty(reportPath))
            {
                using (var writer = new System.IO.StreamWriter(reportPath))
                {
                    writer.WriteLine($"性能分析报告");
                    writer.WriteLine($"生成时间: {System.DateTime.Now}");
                    writer.WriteLine($"采样时长: {elapsedTime:F1}秒");
                    writer.WriteLine($"热点数量: {hotspots.Count}");
                    writer.WriteLine("=================================\n");

                    foreach (var hotspot in hotspots.OrderByDescending(h => h.Severity))
                    {
                        writer.WriteLine($"[{hotspot.Severity}] {hotspot.Category}");
                        writer.WriteLine($"位置: {hotspot.Location}");
                        writer.WriteLine($"平均耗时: {hotspot.AverageTimeMs:F2}ms");
                        writer.WriteLine($"总耗时: {hotspot.TotalTimeMs:F2}ms");
                        writer.WriteLine($"调用次数: {hotspot.SampleCount}");
                        writer.WriteLine($"建议: {string.Join("; ", hotspot.Suggestions)}");
                        writer.WriteLine();
                    }
                }

                EditorUtility.DisplayDialog("成功", $"报告已导出到: {reportPath}", "确定");
            }
        }
    }

    // 性能标记工具类（需要在代码中手动添加标记）
    public static class PerformanceMarker
    {
        private static Stopwatch stopwatch = new Stopwatch();
        private static string currentMarker = "";

        [Conditional("ENABLE_PERFORMANCE_PROFILING")]
        public static void Begin(string markerName, string category = "Performance")
        {
            currentMarker = $"{category}|{markerName}";
            stopwatch.Restart();
        }

        [Conditional("ENABLE_PERFORMANCE_PROFILING")]
        public static void End()
        {
            stopwatch.Stop();
            float elapsedMs = stopwatch.ElapsedMilliseconds;

            // 记录到性能日志
            LoggingManager.Instance?.Debug($"Performance: {currentMarker} - {elapsedMs}ms", "Performance");
        }

        public static void StartProfiling() { }
        public static void StopProfiling() { }
    }

    public class PerformanceData
    {
        public string Category;
        public int SampleCount;
        public float TotalTime;
        public float MinTime = float.MaxValue;
        public float MaxTime;
        public List<float> TimeSamples = new List<float>();
        public float AverageTime => SampleCount > 0 ? TotalTime / SampleCount : 0;
    }

    public class PerformanceHotspot
    {
        public string Category;
        public string Location;
        public float AverageTimeMs;
        public float TotalTimeMs;
        public float MinTimeMs;
        public float MaxTimeMs;
        public int SampleCount;
        public PerformanceSeverity Severity;
        public List<float> TimeSamples;
        public List<string> Suggestions;
    }

    public enum PerformanceSeverity
    {
        Normal,
        Warning,
        Critical
    }
}