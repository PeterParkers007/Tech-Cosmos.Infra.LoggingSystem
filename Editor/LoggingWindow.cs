using UnityEditor;
using UnityEngine;
using TechCosmos.LoggingSystem.Runtime.Enum;
using TechCosmos.LoggingSystem.Runtime.SO;
namespace TechCosmos.LoggingSystem.Editor
{
    public class LoggingWindow : EditorWindow
    {
        private LoggingConfig config;
        private Vector2 scrollPos;

        [MenuItem("Tech-Cosmos/LoggingSystem/Logging Window")]
        public static void ShowWindow() => GetWindow<LoggingWindow>("Logging System");

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // 配置编辑界面
            config = (LoggingConfig)EditorGUILayout.ObjectField("Config", config, typeof(LoggingConfig), false);

            if (config != null)
            {
                DrawConfigEditor();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawConfigEditor()
        {
            // 详细的配置编辑界面
            EditorGUILayout.LabelField("日志系统配置", EditorStyles.boldLabel);

            config.globalLogLevel = (LogLevel)EditorGUILayout.EnumPopup("全局日志级别", config.globalLogLevel);
            config.enableStackTrace = EditorGUILayout.Toggle("启用堆栈跟踪", config.enableStackTrace);

            // 更多配置项...
        }
    }
}
