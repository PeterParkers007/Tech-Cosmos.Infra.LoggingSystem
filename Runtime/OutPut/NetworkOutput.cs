using System.Collections;  // 添加这个，解决 IEnumerator 问题
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;
using UnityEngine;
using TechCosmos.LoggingSystem.Runtime.Interface;
using TechCosmos.LoggingSystem.Runtime.SO;
using TechCosmos.LoggingSystem.Runtime.Struct;
using TechCosmos.LoggingSystem.Runtime.Data;
using TechCosmos.LoggingSystem.Runtime.Enum;
namespace TechCosmos.LoggingSystem.Runtime.OutPut
{
    public class NetworkOutput : ILogOutput
    {
        private LoggingConfig config;
        private Queue<LogEntry> sendQueue = new Queue<LogEntry>();
        private float lastSendTime;
        private bool isSending;
        private MonoBehaviour coroutineRunner;  // 用于执行协程的MonoBehaviour

        public NetworkOutput(LoggingConfig config)
        {
            this.config = config;

            if (string.IsNullOrEmpty(config.serverURL))
            {
                LoggingManager.Instance.Warn("网络日志输出器: 未配置服务器URL", "Logging");
                return;
            }

            // 获取协程执行器（通常是LoggingSystem本身）
            coroutineRunner = LoggingManager.Instance as MonoBehaviour;
            if (coroutineRunner == null)
            {
                LoggingManager.Instance.Error("NetworkOutput: 无法获取协程执行器", "Logging");
                return;
            }

            // 启动定时发送协程
            coroutineRunner.StartCoroutine(SendLoop());
        }

        public void Write(LogEntry entry)
        {
            if (string.IsNullOrEmpty(config.serverURL)) return;

            // 过滤：只发送重要日志到网络
            if (entry.Level >= LogLevel.Warning)
            {
                lock (sendQueue)
                {
                    sendQueue.Enqueue(entry);
                }
            }
        }

        private IEnumerator SendLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(config.sendInterval);

                if (sendQueue.Count > 0 && !isSending)
                {
                    yield return coroutineRunner.StartCoroutine(SendBatch());
                }
            }
        }

        private IEnumerator SendBatch()
        {
            isSending = true;
            List<LogEntry> batch = new List<LogEntry>();

            lock (sendQueue)
            {
                while (sendQueue.Count > 0 && batch.Count < 50) // 批量发送，最多50条
                {
                    batch.Add(sendQueue.Dequeue());
                }
            }

            if (batch.Count > 0)
            {
                yield return coroutineRunner.StartCoroutine(SendToServer(batch));
            }

            isSending = false;
        }

        private IEnumerator SendToServer(List<LogEntry> logs)
        {
            var logData = new NetworkLogData
            {
                deviceId = SystemInfo.deviceUniqueIdentifier,
                appVersion = Application.version,
                platform = Application.platform.ToString(),
                logs = logs
            };

            string json = JsonUtility.ToJson(logData);
            byte[] postData = System.Text.Encoding.UTF8.GetBytes(json);

            using (var www = new UnityWebRequest(config.serverURL, "POST"))
            {
                www.uploadHandler = new UploadHandlerRaw(postData);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");

                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    // 网络发送失败，降级到文件存储
                    LoggingManager.Instance.Warn($"网络日志发送失败: {www.error}", "Logging");
                    FallbackToFile(logs);
                }
            }
        }

        private void FallbackToFile(List<LogEntry> logs)
        {
            string fallbackPath = Path.Combine(Application.persistentDataPath, "NetworkLogs_Fallback.txt");
            using (var writer = new StreamWriter(fallbackPath, true))
            {
                foreach (var log in logs)
                {
                    writer.WriteLine($"[NETWORK_FALLBACK] {log}");
                }
            }
        }

        public void Flush()
        {
            // 立即发送所有待处理日志
            if (sendQueue.Count > 0 && !isSending && coroutineRunner != null)
            {
                coroutineRunner.StartCoroutine(SendBatch());
            }
        }

        public void Dispose()
        {
            // 退出前发送剩余日志
            Flush();
        }
    }
}


