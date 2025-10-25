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

        [MenuItem("Tech-Cosmos/Logging System")]
        public static void ShowWindow() => GetWindow<LoggingWindow>("Logging System");

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // ���ñ༭����
            config = (LoggingConfig)EditorGUILayout.ObjectField("Config", config, typeof(LoggingConfig), false);

            if (config != null)
            {
                DrawConfigEditor();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawConfigEditor()
        {
            // ��ϸ�����ñ༭����
            EditorGUILayout.LabelField("��־ϵͳ����", EditorStyles.boldLabel);

            config.globalLogLevel = (LogLevel)EditorGUILayout.EnumPopup("ȫ����־����", config.globalLogLevel);
            config.enableStackTrace = EditorGUILayout.Toggle("���ö�ջ����", config.enableStackTrace);

            // ����������...
        }
    }
}
