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

        [MenuItem("Tech-Cosmos/LoggingSystem/Log Analysis Tool")]
        public static void ShowWindow()
        {
            GetWindow<LogAnalyzerWindow>("日志分析工具");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("一键日志分析工具", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 日志文件夹选择
            EditorGUILayout.BeginHorizontal();
            logFolderPath = EditorGUILayout.TextField("日志文件夹", logFolderPath);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("选择日志文件夹", Application.persistentDataPath, "");
                if (!string.IsNullOrEmpty(path))
                    logFolderPath = path;
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
            }

            EditorGUILayout.Space();

            // 显示分析结果
            if (result != null)
            {
                DrawAnalysisResults();
            }
        }

        private async void AnalyzeLogs()
        {
            result = new AnalysisResult();

            try
            {
                if (!Directory.Exists(logFolderPath))
                {
                    EditorUtility.DisplayDialog("错误", "日志文件夹不存在", "确定");
                    isAnalyzing = false;
                    return;
                }

                // 获取所有日志文件
                var logFiles = Directory.GetFiles(logFolderPath, "logs_*.json");
                result.TotalLogFiles = logFiles.Length;

                foreach (var file in logFiles)
                {
                    await ProcessLogFileAsync(file);
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

        private System.Threading.Tasks.Task ProcessLogFileAsync(string filePath)
        {
            return System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    var wrapper = JsonUtility.FromJson<LogRecordWrapper>(json);

                    foreach (var log in wrapper.logs)
                    {
                        // 按级别统计
                        if (!result.LevelCounts.ContainsKey(log.Level))
                            result.LevelCounts[log.Level] = 0;
                        result.LevelCounts[log.Level]++;

                        // 按分类统计
                        if (!result.CategoryCounts.ContainsKey(log.Category))
                            result.CategoryCounts[log.Category] = 0;
                        result.CategoryCounts[log.Category]++;

                        // 按场景统计
                        if (!result.SceneCounts.ContainsKey(log.SceneName))
                            result.SceneCounts[log.SceneName] = 0;
                        result.SceneCounts[log.SceneName]++;

                        // 时间分布
                        var hour = log.Timestamp.Hour;
                        if (!result.HourlyDistribution.ContainsKey(hour))
                            result.HourlyDistribution[hour] = 0;
                        result.HourlyDistribution[hour]++;

                        result.TotalLogs++;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"处理日志文件失败: {filePath}, {ex.Message}");
                }
            });
        }

        private void CalculateStatistics()
        {
            // 找出最频繁的错误
            if (result.LevelCounts.ContainsKey("Error"))
                result.ErrorRate = (float)result.LevelCounts["Error"] / result.TotalLogs * 100;

            // 找出问题最多的分类
            result.TopProblematicCategory = result.CategoryCounts
                .OrderByDescending(x => x.Value)
                .FirstOrDefault();

            // 找出问题最多的场景
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
                float percentage = (float)kvp.Value / result.TotalLogs * 100;
                EditorGUILayout.LabelField($"{percentage:F1}%");
                EditorGUILayout.EndHorizontal();
            }

            // 问题分类Top 5
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

            // 高峰时段
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("日志高峰时段", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"{result.PeakHour.Key}:00 - {result.PeakHour.Key + 1}:00: {result.PeakHour.Value} 条");

            // 导出建议
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("分析建议", EditorStyles.boldLabel);
            if (result.ErrorRate > 5)
                EditorGUILayout.HelpBox($"错误率较高({result.ErrorRate:F1}%)，建议检查 {result.TopProblematicCategory.Key} 相关逻辑", MessageType.Warning);

            if (result.TopProblematicScene.Value > result.TotalLogs * 0.3)
                EditorGUILayout.HelpBox($"场景 {result.TopProblematicScene.Key} 日志过多，可能存在性能问题", MessageType.Info);

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
====================

总体统计:
- 日志文件数: {result.TotalLogFiles}
- 总日志条数: {result.TotalLogs}
- 错误率: {result.ErrorRate:F2}%

级别分布:
{string.Join("\n", result.LevelCounts.OrderBy(x => x.Key).Select(kvp => $"- {kvp.Key}: {kvp.Value} 条 ({(float)kvp.Value / result.TotalLogs * 100:F1}%)"))}

问题分类Top 10:
{string.Join("\n", result.CategoryCounts.OrderByDescending(x => x.Value).Take(10).Select(c => $"- {c.Key}: {c.Value} 条"))}

场景分布Top 5:
{string.Join("\n", result.SceneCounts.OrderByDescending(x => x.Value).Take(5).Select(s => $"- {s.Key}: {s.Value} 条"))}

时间分布:
- 高峰时段: {result.PeakHour.Key}:00 - {result.PeakHour.Key + 1}:00 ({result.PeakHour.Value} 条)

建议:
1. 重点关注 {result.TopProblematicCategory.Key} 分类的问题
2. 检查场景 {result.TopProblematicScene.Key} 的性能表现
3. 错误率 {(result.ErrorRate > 5 ? "偏高，需要优化" : "正常")}
";
        }

        private class AnalysisResult
        {
            public int TotalLogFiles = 0;
            public int TotalLogs = 0;
            public float ErrorRate = 0;

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