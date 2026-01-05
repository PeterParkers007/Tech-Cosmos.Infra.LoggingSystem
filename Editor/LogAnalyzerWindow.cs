using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using TechCosmos.LoggingSystem.Runtime.Data;
using TechCosmos.LoggingSystem.Runtime.Enum;
using System;

namespace TechCosmos.LoggingSystem.Editor.Tools
{
    public class LogAnalyzerWindow : EditorWindow
    {
        private string logFolderPath;
        private Vector2 scrollPos;
        private AnalysisResult result;
        private bool isAnalyzing = false;

        [MenuItem("Tech-Cosmos/日志工具/一键日志分析")]
        public static void ShowWindow()
        {
            GetWindow<LogAnalyzerWindow>("日志分析工具");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("一键日志分析工具", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 自动检测日志文件夹
            if (string.IsNullOrEmpty(logFolderPath))
            {
                logFolderPath = GetDefaultLogPath();
            }

            // 日志文件夹选择
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("日志文件夹:", GUILayout.Width(80));
            logFolderPath = EditorGUILayout.TextField(logFolderPath);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("选择日志文件夹", logFolderPath, "");
                if (!string.IsNullOrEmpty(path))
                    logFolderPath = path;
            }

            if (GUILayout.Button("打开", EditorStyles.miniButton, GUILayout.Width(50)))
            {
                if (Directory.Exists(logFolderPath))
                {
                    EditorUtility.RevealInFinder(logFolderPath);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 分析按钮
            if (!isAnalyzing)
            {
                if (GUILayout.Button("开始分析", GUILayout.Height(30)))
                {
                    isAnalyzing = true;
                    AnalyzeLogs();
                }
            }
            else
            {
                EditorGUILayout.LabelField("分析中...", EditorStyles.centeredGreyMiniLabel);
                Repaint();
            }

            EditorGUILayout.Space();

            // 显示分析结果
            if (result != null)
            {
                DrawAnalysisResults();
            }
        }

        private string GetDefaultLogPath()
        {
            // 尝试多个可能的日志路径
            string[] possiblePaths = new string[]
            {
                Path.Combine(Application.persistentDataPath, "Logs"),
                Path.Combine(Application.dataPath, "../Logs"),
                Path.Combine(Application.dataPath, "Logs"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "../LocalLow", Application.companyName, Application.productName, "Logs")
            };

            foreach (var path in possiblePaths)
            {
                if (Directory.Exists(path))
                {
                    return path;
                }
            }

            return Path.Combine(Application.persistentDataPath, "Logs");
        }

        private async void AnalyzeLogs()
        {
            result = new AnalysisResult();

            try
            {
                if (!Directory.Exists(logFolderPath))
                {
                    if (EditorUtility.DisplayDialog("文件夹不存在", $"日志文件夹不存在:\n{logFolderPath}\n是否创建？", "创建", "取消"))
                    {
                        Directory.CreateDirectory(logFolderPath);
                    }
                    else
                    {
                        isAnalyzing = false;
                        return;
                    }
                }

                // 获取所有日志文件
                var logFiles = Directory.GetFiles(logFolderPath, "*.json").ToList();
                logFiles.AddRange(Directory.GetFiles(logFolderPath, "*.txt"));
                logFiles.AddRange(Directory.GetFiles(logFolderPath, "*.log"));

                result.TotalLogFiles = logFiles.Count;

                if (logFiles.Count == 0)
                {
                    EditorUtility.DisplayDialog("无日志文件", $"在 {logFolderPath} 中未找到日志文件\n请确保日志系统已启用文件输出", "确定");
                    isAnalyzing = false;
                    return;
                }

                foreach (var file in logFiles)
                {
                    await System.Threading.Tasks.Task.Run(() => ProcessLogFile(file));
                }

                // 计算统计数据
                CalculateStatistics();
            }
            catch (Exception ex)
            {
                Debug.LogError($"日志分析失败: {ex.Message}");
            }
            finally
            {
                isAnalyzing = false;
                Repaint();
            }
        }

        private void ProcessLogFile(string filePath)
        {
            try
            {
                string extension = Path.GetExtension(filePath).ToLower();

                if (extension == ".json")
                {
                    ProcessJsonLogFile(filePath);
                }
                else if (extension == ".txt" || extension == ".log")
                {
                    ProcessTextLogFile(filePath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"处理日志文件失败: {filePath}, {ex.Message}");
            }
        }

        private void ProcessJsonLogFile(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);

                // 尝试解析为LogRecordWrapper
                if (json.Contains("\"logs\":"))
                {
                    var wrapper = JsonUtility.FromJson<LogRecordWrapper>(json);

                    lock (result)
                    {
                        foreach (var log in wrapper.logs)
                        {
                            AddLogToResult(log);
                        }
                    }
                }
            }
            catch { }
        }

        private void ProcessTextLogFile(string filePath)
        {
            try
            {
                var lines = File.ReadAllLines(filePath);

                lock (result)
                {
                    foreach (var line in lines)
                    {
                        if (line.Contains("[Error]") || line.Contains("[ERROR]"))
                            result.ErrorCount++;
                        else if (line.Contains("[Warning]") || line.Contains("[WARN]"))
                            result.WarningCount++;
                        else if (line.Contains("[Info]") || line.Contains("[INFO]"))
                            result.InfoCount++;
                        else if (line.Contains("[Debug]") || line.Contains("[DEBUG]"))
                            result.DebugCount++;

                        result.TotalLogs++;
                    }
                }
            }
            catch { }
        }

        private void AddLogToResult(LogRecord log)
        {
            // 按级别统计
            if (!result.LevelCounts.ContainsKey(log.Level))
                result.LevelCounts[log.Level] = 0;
            result.LevelCounts[log.Level]++;

            // 按分类统计
            if (!string.IsNullOrEmpty(log.Category))
            {
                if (!result.CategoryCounts.ContainsKey(log.Category))
                    result.CategoryCounts[log.Category] = 0;
                result.CategoryCounts[log.Category]++;
            }

            // 按场景统计
            if (!string.IsNullOrEmpty(log.SceneName))
            {
                if (!result.SceneCounts.ContainsKey(log.SceneName))
                    result.SceneCounts[log.SceneName] = 0;
                result.SceneCounts[log.SceneName]++;
            }

            // 时间分布
            var hour = log.Timestamp.Hour;
            if (!result.HourlyDistribution.ContainsKey(hour))
                result.HourlyDistribution[hour] = 0;
            result.HourlyDistribution[hour]++;

            result.TotalLogs++;
        }

        private void CalculateStatistics()
        {
            // 计算错误率
            int totalErrors = 0;
            if (result.LevelCounts.ContainsKey("Error")) totalErrors += result.LevelCounts["Error"];
            if (result.LevelCounts.ContainsKey("Critical")) totalErrors += result.LevelCounts["Critical"];

            result.ErrorRate = result.TotalLogs > 0 ? (float)totalErrors / result.TotalLogs * 100 : 0;

            // 找出最频繁的分类
            result.TopProblematicCategory = result.CategoryCounts
                .OrderByDescending(x => x.Value)
                .FirstOrDefault();

            // 找出最频繁的场景
            result.TopProblematicScene = result.SceneCounts
                .OrderByDescending(x => x.Value)
                .FirstOrDefault();

            // 高峰时段
            result.PeakHour = result.HourlyDistribution
                .OrderByDescending(x => x.Value)
                .FirstOrDefault();
        }

        private void DrawAnalysisResults()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"分析完成！共分析 {result.TotalLogs} 条日志", EditorStyles.boldLabel);

            // 总体统计
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("总体统计", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"日志文件数: {result.TotalLogFiles}");
            EditorGUILayout.LabelField($"总日志条数: {result.TotalLogs}");
            EditorGUILayout.LabelField($"错误率: {result.ErrorRate:F2}%");

            // 级别分布
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("日志级别分布", EditorStyles.boldLabel);
            foreach (var kvp in result.LevelCounts.OrderBy(x => x.Key))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(kvp.Key, GUILayout.Width(80));
                EditorGUILayout.LabelField(kvp.Value.ToString(), GUILayout.Width(60));
                float percentage = result.TotalLogs > 0 ? (float)kvp.Value / result.TotalLogs * 100 : 0;
                EditorGUILayout.LabelField($"{percentage:F1}%");
                EditorGUILayout.EndHorizontal();
            }

            // 问题分类Top 5
            if (result.CategoryCounts.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("问题最多的分类 (Top 5)", EditorStyles.boldLabel);
                var topCategories = result.CategoryCounts
                    .OrderByDescending(x => x.Value)
                    .Take(5);

                foreach (var category in topCategories)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(category.Key, GUILayout.Width(150));
                    EditorGUILayout.LabelField(category.Value.ToString());
                    EditorGUILayout.EndHorizontal();
                }
            }

            // 高峰时段
            if (result.HourlyDistribution.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("日志高峰时段", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"{result.PeakHour.Key}:00 - {result.PeakHour.Key + 1}:00: {result.PeakHour.Value} 条");
            }

            // 导出建议
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("分析建议", EditorStyles.boldLabel);
            if (result.ErrorRate > 5)
                EditorGUILayout.HelpBox($"错误率较高({result.ErrorRate:F1}%)，建议检查相关逻辑", MessageType.Warning);

            if (result.TotalLogFiles > 20)
                EditorGUILayout.HelpBox($"日志文件较多({result.TotalLogFiles}个)，建议清理旧日志", MessageType.Info);

            EditorGUILayout.EndScrollView();

            // 导出按钮
            EditorGUILayout.Space();
            if (GUILayout.Button("导出分析报告"))
            {
                ExportAnalysisReport();
            }
        }

        private void ExportAnalysisReport()
        {
            string reportPath = EditorUtility.SaveFilePanel("保存分析报告", Application.dataPath, "LogAnalysisReport", "txt");
            if (!string.IsNullOrEmpty(reportPath))
            {
                string report = GenerateReportText();
                File.WriteAllText(reportPath, report);
                EditorUtility.DisplayDialog("成功", $"报告已导出到: {reportPath}", "确定");
            }
        }

        private string GenerateReportText()
        {
            return $@"日志分析报告
生成时间: {DateTime.Now}
日志文件夹: {logFolderPath}
====================

总体统计:
- 日志文件数: {result.TotalLogFiles}
- 总日志条数: {result.TotalLogs}
- 错误率: {result.ErrorRate:F2}%

级别分布:
{string.Join("\n", result.LevelCounts.OrderBy(x => x.Key).Select(kvp => $"- {kvp.Key}: {kvp.Value} 条 ({(result.TotalLogs > 0 ? (float)kvp.Value / result.TotalLogs * 100 : 0):F1}%)"))}

问题分类Top 10:
{(result.CategoryCounts.Count > 0 ? string.Join("\n", result.CategoryCounts.OrderByDescending(x => x.Value).Take(10).Select(c => $"- {c.Key}: {c.Value} 条")) : "无分类数据")}

场景分布Top 5:
{(result.SceneCounts.Count > 0 ? string.Join("\n", result.SceneCounts.OrderByDescending(x => x.Value).Take(5).Select(s => $"- {s.Key}: {s.Value} 条")) : "无场景数据")}

{(result.HourlyDistribution.Count > 0 ? $"时间分布:\n- 高峰时段: {result.PeakHour.Key}:00 - {result.PeakHour.Key + 1}:00 ({result.PeakHour.Value} 条)\n" : "")}

建议:
1. 重点关注错误率较高的模块
2. {(result.ErrorRate > 5 ? "错误率偏高，需要优化" : "错误率正常")}
3. {(result.TotalLogFiles > 20 ? "建议定期清理旧日志文件" : "日志文件数量正常")}
";
        }

        private class AnalysisResult
        {
            public int TotalLogFiles = 0;
            public int TotalLogs = 0;
            public float ErrorRate = 0;

            // 文本日志统计
            public int ErrorCount = 0;
            public int WarningCount = 0;
            public int InfoCount = 0;
            public int DebugCount = 0;

            public Dictionary<string, int> LevelCounts = new Dictionary<string, int>();
            public Dictionary<string, int> CategoryCounts = new Dictionary<string, int>();
            public Dictionary<string, int> SceneCounts = new Dictionary<string, int>();
            public Dictionary<int, int> HourlyDistribution = new Dictionary<int, int>();

            public KeyValuePair<string, int> TopProblematicCategory;
            public KeyValuePair<string, int> TopProblematicScene;
            public KeyValuePair<int, int> PeakHour;
        }
    }
}