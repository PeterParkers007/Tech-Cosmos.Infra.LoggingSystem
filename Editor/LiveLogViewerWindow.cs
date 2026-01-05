using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using TechCosmos.LoggingSystem.Runtime.Struct;
using TechCosmos.LoggingSystem.Runtime.Enum;
using System;

namespace TechCosmos.LoggingSystem.Editor.Tools
{
    public class LiveLogViewerWindow : EditorWindow
    {
        private Vector2 scrollPos;
        private Vector2 filterScrollPos;

        private List<LogEntry> allLogs = new List<LogEntry>();
        private List<LogEntry> filteredLogs = new List<LogEntry>();

        // 过滤条件
        private string searchText = "";
        private LogLevel minLogLevel = LogLevel.Trace;
        private string selectedCategory = "All";
        private HashSet<string> categories = new HashSet<string>();

        // 显示选项
        private bool showTimestamp = true;
        private bool showCategory = true;
        private bool showScene = false;
        private bool autoScroll = true;

        // 统计
        private int totalLogsReceived = 0;
        private float lastUpdateTime = 0;

        private static LiveLogViewerWindow instance;

        [MenuItem("Tech-Cosmos/日志工具/实时日志查看器")]
        public static void ShowWindow()
        {
            instance = GetWindow<LiveLogViewerWindow>("实时日志");
            instance.minSize = new Vector2(800, 500);
        }

        private void OnEnable()
        {
            // 订阅日志事件
            TechCosmos.LoggingSystem.Runtime.LoggingManager.OnLogReceived += ReceiveLog;

            EditorApplication.playModeStateChanged += OnPlayModeChanged;

            if (EditorApplication.isPlaying)
            {
                StartReceivingLogs();
            }
        }

        private void OnDisable()
        {
            // 取消订阅
            TechCosmos.LoggingSystem.Runtime.LoggingManager.OnLogReceived -= ReceiveLog;

            StopReceivingLogs();
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                StartReceivingLogs();
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                StopReceivingLogs();
                ClearLogs();
            }
        }

        private void StartReceivingLogs()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void StopReceivingLogs()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (Time.realtimeSinceStartup - lastUpdateTime > 0.5f)
            {
                lastUpdateTime = Time.realtimeSinceStartup;
                Repaint();
            }
        }

        // 日志接收回调
        public void ReceiveLog(TechCosmos.LoggingSystem.Runtime.Struct.LogEntry entry)
        {
            AddLog(entry);
        }

        private void AddLog(TechCosmos.LoggingSystem.Runtime.Struct.LogEntry entry)
        {
            allLogs.Add(entry);
            totalLogsReceived++;

            // 更新分类列表
            if (!categories.Contains(entry.Category))
            {
                categories.Add(entry.Category);
            }

            // 应用过滤
            ApplyFilters();

            if (autoScroll)
            {
                EditorApplication.delayCall += () => {
                    scrollPos.y = Mathf.Infinity;
                };
            }
        }

        private void ClearLogs()
        {
            allLogs.Clear();
            filteredLogs.Clear();
            categories.Clear();
            totalLogsReceived = 0;
            ApplyFilters();
        }

        private void OnGUI()
        {
            DrawToolbar();

            EditorGUILayout.BeginVertical();

            DrawFilters();

            EditorGUILayout.Space();

            DrawLogList();

            EditorGUILayout.EndVertical();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // 统计信息
            EditorGUILayout.LabelField($"日志数: {totalLogsReceived} | 显示: {filteredLogs.Count}",
                GUILayout.Width(150));

            // 显示选项
            showTimestamp = GUILayout.Toggle(showTimestamp, "时间", EditorStyles.toolbarButton);
            showCategory = GUILayout.Toggle(showCategory, "分类", EditorStyles.toolbarButton);
            showScene = GUILayout.Toggle(showScene, "场景", EditorStyles.toolbarButton);
            autoScroll = GUILayout.Toggle(autoScroll, "自动滚动", EditorStyles.toolbarButton);

            GUILayout.FlexibleSpace();

            // 操作按钮
            if (GUILayout.Button("清除", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                ClearLogs();
            }

            if (GUILayout.Button("导出", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                ExportLogs();
            }

            if (GUILayout.Button("测试", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                SendTestLogs();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void SendTestLogs()
        {
            var manager = TechCosmos.LoggingSystem.Runtime.LoggingManager.Instance;
            if (manager != null)
            {
                manager.Info("测试信息日志", "Test");
                manager.Warn("测试警告日志", "Test");
                manager.Error("测试错误日志", "Test");
                manager.Debug("测试调试日志", "Test");
            }
        }

        private void DrawFilters()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();

            // 搜索框
            EditorGUILayout.LabelField("搜索:", GUILayout.Width(40));
            string newSearchText = EditorGUILayout.TextField(searchText);
            if (newSearchText != searchText)
            {
                searchText = newSearchText;
                ApplyFilters();
            }

            // 日志级别过滤
            EditorGUILayout.LabelField("级别:", GUILayout.Width(40));
            LogLevel newMinLevel = (LogLevel)EditorGUILayout.EnumPopup(minLogLevel, GUILayout.Width(100));
            if (newMinLevel != minLogLevel)
            {
                minLogLevel = newMinLevel;
                ApplyFilters();
            }

            // 分类过滤
            EditorGUILayout.LabelField("分类:", GUILayout.Width(40));
            string[] categoryOptions = new[] { "All" }.Concat(categories.OrderBy(c => c)).ToArray();
            int selectedIndex = Array.IndexOf(categoryOptions, selectedCategory);
            int newIndex = EditorGUILayout.Popup(selectedIndex, categoryOptions, GUILayout.Width(150));
            if (newIndex != selectedIndex)
            {
                selectedCategory = categoryOptions[newIndex];
                ApplyFilters();
            }

            EditorGUILayout.EndHorizontal();

            // 快速过滤器按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("仅错误", EditorStyles.miniButton))
            {
                minLogLevel = LogLevel.Error;
                ApplyFilters();
            }
            if (GUILayout.Button("仅警告", EditorStyles.miniButton))
            {
                minLogLevel = LogLevel.Warning;
                ApplyFilters();
            }
            if (GUILayout.Button("重置过滤", EditorStyles.miniButton))
            {
                searchText = "";
                minLogLevel = LogLevel.Trace;
                selectedCategory = "All";
                ApplyFilters();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawLogList()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));

            if (filteredLogs.Count == 0)
            {
                EditorGUILayout.LabelField("暂无日志", EditorStyles.centeredGreyMiniLabel);
                if (totalLogsReceived == 0)
                {
                    EditorGUILayout.HelpBox("提示：运行游戏后日志将显示在这里\n确保LoggingConfig启用了日志输出", MessageType.Info);
                }
            }
            else
            {
                for (int i = 0; i < filteredLogs.Count; i++)
                {
                    DrawLogEntry(filteredLogs[i], i);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawLogEntry(TechCosmos.LoggingSystem.Runtime.Struct.LogEntry entry, int index)
        {
            Color bgColor = GetLogColor(entry.Level);
            bool isEven = index % 2 == 0;

            Rect rect = EditorGUILayout.BeginHorizontal();

            // 背景色
            if (isEven)
            {
                EditorGUI.DrawRect(rect, new Color(bgColor.r, bgColor.g, bgColor.b, 0.1f));
            }

            // 日志内容
            string logText = "";

            if (showTimestamp)
                logText += $"[{entry.Timestamp:HH:mm:ss.fff}] ";

            logText += $"[{entry.Level.ToString().Substring(0, 1)}] ";

            if (showCategory)
                logText += $"[{entry.Category}] ";

            if (showScene)
                logText += $"[{entry.SceneName}] ";

            logText += entry.Message;

            GUIStyle style = entry.Level >= LogLevel.Error ? EditorStyles.boldLabel : EditorStyles.label;

            EditorGUILayout.LabelField(logText, style);

            EditorGUILayout.EndHorizontal();

            // 鼠标悬停显示完整信息
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                ShowLogDetails(entry);
            }
        }

        private Color GetLogColor(LogLevel level)
        {
            return level switch
            {
                LogLevel.Trace => Color.gray,
                LogLevel.Debug => Color.cyan,
                LogLevel.Info => Color.white,
                LogLevel.Warning => Color.yellow,
                LogLevel.Error => new Color(1f, 0.5f, 0f), // 橙色
                LogLevel.Critical => Color.red,
                _ => Color.white
            };
        }

        private void ShowLogDetails(TechCosmos.LoggingSystem.Runtime.Struct.LogEntry entry)
        {
            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent("复制完整信息"), false, () =>
            {
                string fullInfo = $@"日志详情:
时间: {entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}
级别: {entry.Level}
分类: {entry.Category}
场景: {entry.SceneName}
对象: {entry.ObjectName}
消息: {entry.Message}";

                if (!string.IsNullOrEmpty(entry.StackTrace))
                {
                    fullInfo += $"\n堆栈: {entry.StackTrace}";
                }

                EditorGUIUtility.systemCopyBuffer = fullInfo;
            });

            menu.ShowAsContext();
        }

        private void ApplyFilters()
        {
            filteredLogs = allLogs.Where(log =>
            {
                // 级别过滤
                if (log.Level < minLogLevel)
                    return false;

                // 分类过滤
                if (selectedCategory != "All" && log.Category != selectedCategory)
                    return false;

                // 搜索文本过滤
                if (!string.IsNullOrEmpty(searchText))
                {
                    if (!log.Message.Contains(searchText, StringComparison.OrdinalIgnoreCase) &&
                        !log.Category.Contains(searchText, StringComparison.OrdinalIgnoreCase) &&
                        !log.SceneName.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                        return false;
                }

                return true;
            }).ToList();
        }

        private void ExportLogs()
        {
            string exportPath = EditorUtility.SaveFilePanel("导出日志", Application.dataPath,
                $"Logs_{DateTime.Now:yyyyMMdd_HHmmss}", "txt");

            if (!string.IsNullOrEmpty(exportPath))
            {
                using (var writer = new System.IO.StreamWriter(exportPath))
                {
                    writer.WriteLine($"日志导出时间: {DateTime.Now}");
                    writer.WriteLine($"总日志数: {totalLogsReceived}");
                    writer.WriteLine($"过滤后: {filteredLogs.Count}");
                    writer.WriteLine("=================================");

                    foreach (var log in filteredLogs)
                    {
                        writer.WriteLine($"[{log.Timestamp:HH:mm:ss.fff}] [{log.Level}] [{log.Category}] {log.Message}");
                        if (!string.IsNullOrEmpty(log.StackTrace))
                        {
                            writer.WriteLine($"堆栈: {log.StackTrace}");
                        }
                    }
                }

                EditorUtility.DisplayDialog("成功", $"日志已导出到: {exportPath}", "确定");
            }
        }
    }
}