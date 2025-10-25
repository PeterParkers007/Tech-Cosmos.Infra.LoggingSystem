using System.Collections;  // ����������� IEnumerator ����
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
        private MonoBehaviour coroutineRunner;  // ����ִ��Э�̵�MonoBehaviour

        public NetworkOutput(LoggingConfig config)
        {
            this.config = config;

            if (string.IsNullOrEmpty(config.serverURL))
            {
                LoggingManager.Instance.Warn("������־�����: δ���÷�����URL", "Logging");
                return;
            }

            // ��ȡЭ��ִ������ͨ����LoggingSystem����
            coroutineRunner = LoggingManager.Instance as MonoBehaviour;
            if (coroutineRunner == null)
            {
                LoggingManager.Instance.Error("NetworkOutput: �޷���ȡЭ��ִ����", "Logging");
                return;
            }

            // ������ʱ����Э��
            coroutineRunner.StartCoroutine(SendLoop());
        }

        public void Write(LogEntry entry)
        {
            if (string.IsNullOrEmpty(config.serverURL)) return;

            // ���ˣ�ֻ������Ҫ��־������
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
                while (sendQueue.Count > 0 && batch.Count < 50) // �������ͣ����50��
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
                    // ���緢��ʧ�ܣ��������ļ��洢
                    LoggingManager.Instance.Warn($"������־����ʧ��: {www.error}", "Logging");
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
            // �����������д�������־
            if (sendQueue.Count > 0 && !isSending && coroutineRunner != null)
            {
                coroutineRunner.StartCoroutine(SendBatch());
            }
        }

        public void Dispose()
        {
            // �˳�ǰ����ʣ����־
            Flush();
        }
    }
}


