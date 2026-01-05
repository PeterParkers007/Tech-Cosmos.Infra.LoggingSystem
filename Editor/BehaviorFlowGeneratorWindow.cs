using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using TechCosmos.LoggingSystem.Runtime.Data;

namespace TechCosmos.LoggingSystem.Editor.Tools
{
    public class BehaviorFlowGeneratorWindow : EditorWindow
    {
        private Vector2 scrollPos;

        // 配置
        private string logFolderPath;
        private string sessionId = "";
        private List<string> availableSessions = new List<string>();

        // 行为流数据
        private BehaviorFlowGraph graph;
        private bool isGenerating = false;

        // 显示选项
        private bool showTimestamps = true;
        private bool showTransitions = true;
        private bool showStatistics = true;

        [MenuItem("Tech-Cosmos/玩家行为流程图")]
        public static void ShowWindow()
        {
            GetWindow<BehaviorFlowGeneratorWindow>("行为流程图");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("玩家行为流程图生成器", EditorStyles.boldLabel);

            DrawControls();

            EditorGUILayout.Space();

            if (graph != null && graph.Nodes.Count > 0)
            {
                DrawBehaviorFlow();
            }
            else if (isGenerating)
            {
                EditorGUILayout.HelpBox("正在生成行为流程图...", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("选择日志文件夹并点击生成按钮", MessageType.Info);
            }
        }

        private void DrawControls()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 日志文件夹选择
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("日志文件夹:", GUILayout.Width(80));
            logFolderPath = EditorGUILayout.TextField(logFolderPath);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("选择日志文件夹", Application.persistentDataPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    logFolderPath = path;
                    ScanForSessions();
                }
            }
            EditorGUILayout.EndHorizontal();

            // 会话选择
            if (availableSessions.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("玩家会话:", GUILayout.Width(80));

                int selectedIndex = Mathf.Max(0, availableSessions.IndexOf(sessionId));
                int newIndex = EditorGUILayout.Popup(selectedIndex, availableSessions.ToArray());
                if (newIndex != selectedIndex)
                {
                    sessionId = availableSessions[newIndex];
                }

                if (GUILayout.Button("扫描", GUILayout.Width(60)))
                {
                    ScanForSessions();
                }
                EditorGUILayout.EndHorizontal();
            }

            // 生成按钮
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = !isGenerating && Directory.Exists(logFolderPath);
            if (GUILayout.Button("生成行为流", GUILayout.Height(30)))
            {
                GenerateBehaviorFlow();
            }
            GUI.enabled = true;

            // 显示选项
            GUILayout.FlexibleSpace();
            showTimestamps = GUILayout.Toggle(showTimestamps, "时间", EditorStyles.toolbarButton);
            showTransitions = GUILayout.Toggle(showTransitions, "转换", EditorStyles.toolbarButton);
            showStatistics = GUILayout.Toggle(showStatistics, "统计", EditorStyles.toolbarButton);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void ScanForSessions()
        {
            availableSessions.Clear();

            if (!Directory.Exists(logFolderPath))
                return;

            var logFiles = Directory.GetFiles(logFolderPath, "logs_*.json");

            HashSet<string> sessionSet = new HashSet<string>();

            foreach (var file in logFiles.Take(10)) // 限制扫描文件数量
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var wrapper = JsonUtility.FromJson<LogRecordWrapper>(json);

                    foreach (var log in wrapper.logs)
                    {
                        // 使用设备ID + 时间作为会话标识
                        string sessionKey = $"{log.DeviceId}_{log.Timestamp:yyyyMMdd}";
                        if (!sessionSet.Contains(sessionKey))
                        {
                            sessionSet.Add(sessionKey);
                            availableSessions.Add(sessionKey);
                        }
                    }
                }
                catch { }
            }

            if (availableSessions.Count > 0)
            {
                sessionId = availableSessions[0];
            }
        }

        private async void GenerateBehaviorFlow()
        {
            isGenerating = true;
            graph = new BehaviorFlowGraph();

            try
            {
                // 分析日志文件
                var logFiles = Directory.GetFiles(logFolderPath, "logs_*.json");

                foreach (var file in logFiles)
                {
                    await AnalyzeLogFileAsync(file);
                }

                // 构建行为流程图
                BuildBehaviorGraph();

                // 分析行为模式
                AnalyzeBehaviorPatterns();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"生成行为流程图失败: {ex.Message}");
            }
            finally
            {
                isGenerating = false;
                Repaint();
            }
        }

        private System.Threading.Tasks.Task AnalyzeLogFileAsync(string filePath)
        {
            return System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    var wrapper = JsonUtility.FromJson<LogRecordWrapper>(json);

                    foreach (var log in wrapper.logs)
                    {
                        // 过滤指定会话
                        string logSession = $"{log.DeviceId}_{log.Timestamp:yyyyMMdd}";
                        if (logSession != sessionId)
                            continue;

                        // 识别行为类型
                        var behaviorType = IdentifyBehaviorType(log);

                        if (behaviorType != BehaviorType.Unknown)
                        {
                            var node = new BehaviorNode
                            {
                                Timestamp = log.Timestamp,
                                BehaviorType = behaviorType,
                                Category = log.Category,
                                Scene = log.SceneName,
                                Details = log.Message
                            };

                            graph.AddNode(node);
                        }
                    }
                }
                catch { }
            });
        }

        private BehaviorType IdentifyBehaviorType(LogRecord log)
        {
            // 根据日志内容识别行为类型
            string message = log.Message.ToLower();

            if (message.Contains("点击") || message.Contains("press") || message.Contains("tap"))
                return BehaviorType.Click;

            if (message.Contains("购买") || message.Contains("buy") || message.Contains("purchase"))
                return BehaviorType.Purchase;

            if (message.Contains("升级") || message.Contains("level up") || message.Contains("upgrade"))
                return BehaviorType.Upgrade;

            if (message.Contains("战斗") || message.Contains("battle") || message.Contains("fight"))
                return BehaviorType.Combat;

            if (message.Contains("任务") || message.Contains("quest") || message.Contains("mission"))
                return BehaviorType.Quest;

            if (message.Contains("登录") || message.Contains("login") || message.Contains("enter"))
                return BehaviorType.Login;

            if (message.Contains("退出") || message.Contains("logout") || message.Contains("exit"))
                return BehaviorType.Logout;

            if (message.Contains("场景") || message.Contains("scene") || message.Contains("level"))
                return BehaviorType.SceneChange;

            return BehaviorType.Unknown;
        }

        private void BuildBehaviorGraph()
        {
            // 按时间排序
            var sortedNodes = graph.Nodes.OrderBy(n => n.Timestamp).ToList();

            // 构建节点间的关系
            for (int i = 0; i < sortedNodes.Count - 1; i++)
            {
                var fromNode = sortedNodes[i];
                var toNode = sortedNodes[i + 1];

                // 计算时间间隔
                float timeSpan = (float)(toNode.Timestamp - fromNode.Timestamp).TotalSeconds;

                // 创建转换
                var transition = new BehaviorTransition
                {
                    FromNode = fromNode,
                    ToNode = toNode,
                    TimeSpan = timeSpan,
                    TransitionType = IdentifyTransitionType(fromNode, toNode)
                };

                graph.AddTransition(transition);
            }
        }

        private TransitionType IdentifyTransitionType(BehaviorNode from, BehaviorNode to)
        {
            // 根据行为类型判断转换类型
            if (from.BehaviorType == BehaviorType.Login && to.BehaviorType == BehaviorType.SceneChange)
                return TransitionType.Normal;

            if (from.BehaviorType == BehaviorType.Purchase && to.BehaviorType == BehaviorType.Upgrade)
                return TransitionType.UpgradeAfterPurchase;

            if (from.BehaviorType == BehaviorType.Combat && to.BehaviorType == BehaviorType.Combat)
                return TransitionType.ContinuousCombat;

            return TransitionType.Normal;
        }

        private void AnalyzeBehaviorPatterns()
        {
            // 分析常见行为序列
            var sequences = FindCommonSequences();

            foreach (var seq in sequences)
            {
                graph.AddPattern(seq);
            }

            // 计算统计数据
            CalculateStatistics();
        }

        private List<BehaviorPattern> FindCommonSequences()
        {
            var patterns = new List<BehaviorPattern>();

            // 查找登录后的常见行为序列
            var loginSequences = graph.GetSequencesStartingWith(BehaviorType.Login, 3);
            if (loginSequences.Count > 0)
            {
                patterns.Add(new BehaviorPattern
                {
                    Name = "新手流程",
                    Sequence = loginSequences[0],
                    Frequency = loginSequences.Count,
                    AverageDuration = (float)loginSequences.Average(s => s.Sum(n => 1))
                });
            }

            // 查找购买模式
            var purchaseSequences = graph.GetSequencesContaining(BehaviorType.Purchase, 5);
            if (purchaseSequences.Count > 0)
            {
                patterns.Add(new BehaviorPattern
                {
                    Name = "购买习惯",
                    Sequence = purchaseSequences[0],
                    Frequency = purchaseSequences.Count,
                    AverageDuration = (float)purchaseSequences.Average(s => s.Sum(n => 1))
                });
            }

            return patterns;
        }

        private void CalculateStatistics()
        {
            if (graph.Nodes.Count == 0)
                return;

            graph.Statistics.TotalBehaviors = graph.Nodes.Count;
            graph.Statistics.AverageSessionDuration = (float)(graph.Nodes.Last().Timestamp - graph.Nodes.First().Timestamp).TotalMinutes;
            graph.Statistics.BehaviorFrequency = graph.Nodes.Count / Mathf.Max(1, graph.Statistics.AverageSessionDuration);

            // 行为类型分布
            graph.Statistics.BehaviorDistribution = graph.Nodes
                .GroupBy(n => n.BehaviorType)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        private void DrawBehaviorFlow()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // 会话信息
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"玩家会话: {sessionId}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"行为总数: {graph.Nodes.Count}");
            EditorGUILayout.LabelField($"会话时长: {graph.Statistics.AverageSessionDuration:F1}分钟");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // 行为流程图
            EditorGUILayout.LabelField("行为流程图", EditorStyles.boldLabel);

            // 按时间分组显示
            var timeGroups = graph.Nodes
                .GroupBy(n => n.Timestamp.ToString("HH:mm"))
                .OrderBy(g => g.Key);

            foreach (var group in timeGroups)
            {
                EditorGUILayout.LabelField($"{group.Key}", EditorStyles.miniBoldLabel);

                foreach (var node in group.OrderBy(n => n.Timestamp))
                {
                    DrawBehaviorNode(node);
                }

                EditorGUILayout.Space(5);
            }

            EditorGUILayout.Space();

            // 行为模式
            if (graph.Patterns.Count > 0)
            {
                EditorGUILayout.LabelField("发现的行为模式", EditorStyles.boldLabel);

                foreach (var pattern in graph.Patterns)
                {
                    DrawBehaviorPattern(pattern);
                }
            }

            // 统计信息
            if (showStatistics && graph.Statistics.BehaviorDistribution.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("行为分布统计", EditorStyles.boldLabel);

                foreach (var kvp in graph.Statistics.BehaviorDistribution.OrderByDescending(x => x.Value))
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(kvp.Key.ToString(), GUILayout.Width(100));
                    EditorGUILayout.LabelField(kvp.Value.ToString(), GUILayout.Width(50));
                    float percentage = (float)kvp.Value / graph.Statistics.TotalBehaviors * 100;
                    EditorGUILayout.LabelField($"{percentage:F1}%");
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndScrollView();

            // 导出按钮
            EditorGUILayout.Space();
            if (GUILayout.Button("导出行为分析报告"))
            {
                ExportBehaviorReport();
            }

            if (GUILayout.Button("导出为Mermaid图表"))
            {
                ExportMermaidDiagram();
            }
        }

        private void DrawBehaviorNode(BehaviorNode node)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            // 时间戳
            if (showTimestamps)
            {
                EditorGUILayout.LabelField($"{node.Timestamp:HH:mm:ss}",
                    GUILayout.Width(60));
            }

            // 行为类型图标
            string icon = GetBehaviorIcon(node.BehaviorType);
            EditorGUILayout.LabelField(icon, GUILayout.Width(20));

            // 行为描述
            string shortDetails = node.Details.Length > 50 ?
                node.Details.Substring(0, 47) + "..." : node.Details;
            EditorGUILayout.LabelField(shortDetails, EditorStyles.wordWrappedMiniLabel);

            // 场景信息
            if (!string.IsNullOrEmpty(node.Scene))
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField($"[{node.Scene}]",
                    EditorStyles.miniLabel, GUILayout.Width(80));
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawBehaviorPattern(BehaviorPattern pattern)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField($"{pattern.Name} (出现{pattern.Frequency}次)",
                EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            foreach (var node in pattern.Sequence)
            {
                EditorGUILayout.LabelField(GetBehaviorIcon(node.BehaviorType),
                    GUILayout.Width(20));
                EditorGUILayout.LabelField(node.BehaviorType.ToString(),
                    EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"平均时长: {pattern.AverageDuration:F1}分钟",
                EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
        }

        private string GetBehaviorIcon(BehaviorType type)
        {
            return type switch
            {
                BehaviorType.Click => "🖱️",
                BehaviorType.Purchase => "💰",
                BehaviorType.Upgrade => "⬆️",
                BehaviorType.Combat => "⚔️",
                BehaviorType.Quest => "📋",
                BehaviorType.Login => "🚪",
                BehaviorType.Logout => "🚶",
                BehaviorType.SceneChange => "🔄",
                _ => "📝"
            };
        }

        private void ExportBehaviorReport()
        {
            string reportPath = EditorUtility.SaveFilePanel("导出行为分析报告", Application.dataPath,
                $"BehaviorReport_{sessionId}", "txt");

            if (!string.IsNullOrEmpty(reportPath))
            {
                using (var writer = new StreamWriter(reportPath))
                {
                    writer.WriteLine($"玩家行为分析报告");
                    writer.WriteLine($"会话ID: {sessionId}");
                    writer.WriteLine($"生成时间: {System.DateTime.Now}");
                    writer.WriteLine($"行为总数: {graph.Nodes.Count}");
                    writer.WriteLine($"会话时长: {graph.Statistics.AverageSessionDuration:F1}分钟");
                    writer.WriteLine("=================================\n");

                    writer.WriteLine("行为时间线:");
                    foreach (var node in graph.Nodes.OrderBy(n => n.Timestamp))
                    {
                        writer.WriteLine($"[{node.Timestamp:HH:mm:ss}] {node.BehaviorType}: {node.Details}");
                    }

                    writer.WriteLine("\n行为模式:");
                    foreach (var pattern in graph.Patterns)
                    {
                        writer.WriteLine($"{pattern.Name}: {string.Join(" → ", pattern.Sequence.Select(n => n.BehaviorType))}");
                    }

                    writer.WriteLine("\n行为分布:");
                    foreach (var kvp in graph.Statistics.BehaviorDistribution.OrderByDescending(x => x.Value))
                    {
                        float percentage = (float)kvp.Value / graph.Statistics.TotalBehaviors * 100;
                        writer.WriteLine($"{kvp.Key}: {kvp.Value}次 ({percentage:F1}%)");
                    }
                }

                EditorUtility.DisplayDialog("成功", $"报告已导出到: {reportPath}", "确定");
            }
        }

        private void ExportMermaidDiagram()
        {
            string mermaidPath = EditorUtility.SaveFilePanel("导出Mermaid图表", Application.dataPath,
                $"BehaviorFlow_{sessionId}", "mmd");

            if (!string.IsNullOrEmpty(mermaidPath))
            {
                using (var writer = new StreamWriter(mermaidPath))
                {
                    writer.WriteLine("```mermaid");
                    writer.WriteLine("graph TD");
                    writer.WriteLine("    %% 玩家行为流程图");

                    // 生成节点
                    int nodeId = 1;
                    var nodeMap = new Dictionary<BehaviorNode, string>();

                    foreach (var node in graph.Nodes.OrderBy(n => n.Timestamp))
                    {
                        string nodeName = $"N{nodeId++}";
                        nodeMap[node] = nodeName;

                        string label = $"{node.BehaviorType}\\n{node.Timestamp:HH:mm}";
                        writer.WriteLine($"    {nodeName}[\"{label}\"]");
                    }

                    // 生成边
                    foreach (var transition in graph.Transitions)
                    {
                        if (nodeMap.ContainsKey(transition.FromNode) && nodeMap.ContainsKey(transition.ToNode))
                        {
                            string label = transition.TimeSpan > 60 ?
                                $"{transition.TimeSpan / 60:F0}分" : $"{transition.TimeSpan:F0}秒";

                            writer.WriteLine($"    {nodeMap[transition.FromNode]} -->|{label}| {nodeMap[transition.ToNode]}");
                        }
                    }

                    writer.WriteLine("```");
                }

                EditorUtility.DisplayDialog("成功",
                    $"Mermaid图表已导出到: {mermaidPath}\n可粘贴到支持Mermaid的编辑器中查看",
                    "确定");
            }
        }
    }

    // 数据结构
    public enum BehaviorType
    {
        Unknown,
        Click,
        Purchase,
        Upgrade,
        Combat,
        Quest,
        Login,
        Logout,
        SceneChange
    }

    public enum TransitionType
    {
        Normal,
        UpgradeAfterPurchase,
        ContinuousCombat
    }

    public class BehaviorNode
    {
        public System.DateTime Timestamp;
        public BehaviorType BehaviorType;
        public string Category;
        public string Scene;
        public string Details;
    }

    public class BehaviorTransition
    {
        public BehaviorNode FromNode;
        public BehaviorNode ToNode;
        public float TimeSpan; // 秒
        public TransitionType TransitionType;
    }

    public class BehaviorPattern
    {
        public string Name;
        public List<BehaviorNode> Sequence;
        public int Frequency;
        public float AverageDuration;
    }

    public class BehaviorFlowGraph
    {
        public List<BehaviorNode> Nodes = new List<BehaviorNode>();
        public List<BehaviorTransition> Transitions = new List<BehaviorTransition>();
        public List<BehaviorPattern> Patterns = new List<BehaviorPattern>();
        public BehaviorStatistics Statistics = new BehaviorStatistics();

        public void AddNode(BehaviorNode node) => Nodes.Add(node);
        public void AddTransition(BehaviorTransition transition) => Transitions.Add(transition);
        public void AddPattern(BehaviorPattern pattern) => Patterns.Add(pattern);

        public List<List<BehaviorNode>> GetSequencesStartingWith(BehaviorType startType, int maxLength)
        {
            var sequences = new List<List<BehaviorNode>>();
            var sortedNodes = Nodes.OrderBy(n => n.Timestamp).ToList();

            for (int i = 0; i < sortedNodes.Count - maxLength; i++)
            {
                if (sortedNodes[i].BehaviorType == startType)
                {
                    sequences.Add(sortedNodes.Skip(i).Take(maxLength).ToList());
                }
            }

            return sequences;
        }

        public List<List<BehaviorNode>> GetSequencesContaining(BehaviorType containsType, int maxLength)
        {
            var sequences = new List<List<BehaviorNode>>();
            var sortedNodes = Nodes.OrderBy(n => n.Timestamp).ToList();

            for (int i = 0; i < sortedNodes.Count - maxLength; i++)
            {
                if (sortedNodes.Skip(i).Take(maxLength).Any(n => n.BehaviorType == containsType))
                {
                    sequences.Add(sortedNodes.Skip(i).Take(maxLength).ToList());
                }
            }

            return sequences;
        }
    }

    public class BehaviorStatistics
    {
        public int TotalBehaviors;
        public float AverageSessionDuration; // 分钟
        public float BehaviorFrequency; // 行为/分钟
        public Dictionary<BehaviorType, int> BehaviorDistribution = new Dictionary<BehaviorType, int>();
    }
}