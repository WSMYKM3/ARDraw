using System;
using System.Collections;
using System.Linq;
using System.Text;
using DilmerGames.Core.Singletons;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class ARDebugManager : Singleton<ARDebugManager>
{   
    [SerializeField]
    private TextMeshProUGUI debugAreaText = null;

    [SerializeField]
    private bool enableDebug = false;

    [SerializeField]
    private int maxLines = 8;

    [Header("Remote Debug Server")]
    [SerializeField]
    private bool enableRemoteDebug = false;

    [Tooltip("Play mode on this Mac + server on same machine: http://127.0.0.1:8080/log. Phone on Wi‑Fi: http://<PC-LAN-IP>:8080/log")]
    [SerializeField]
    private string remoteServerUrl = "http://127.0.0.1:8080/log";

    private bool _loggedRemoteOkThisSession;

    void OnEnable()
    {
        debugAreaText.enabled = enableDebug;
        _loggedRemoteOkThisSession = false;
    }

    void Start()
    {
        if (!enableRemoteDebug) return;
        Debug.Log($"[ARDebugManager] Remote logging ON → {remoteServerUrl}");
        LogInfo($"Remote debug ping ({Application.platform})");
    }

    public void LogInfo(string message)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        if (enableDebug && debugAreaText != null)
        {
            ClearLines();
            debugAreaText.text += $"{timestamp}: <color=\"white\">{message}</color>\n";
        }
        SendToRemoteServer("info", timestamp, message);
    }

    public void LogError(string message)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        if (enableDebug && debugAreaText != null)
        {
            ClearLines();
            debugAreaText.text += $"{timestamp}: <color=\"red\">{message}</color>\n";
        }
        SendToRemoteServer("error", timestamp, message);
    }

    public void LogWarning(string message)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        if (enableDebug && debugAreaText != null)
        {
            ClearLines();
            debugAreaText.text += $"{timestamp}: <color=\"yellow\">{message}</color>\n";
        }
        SendToRemoteServer("warning", timestamp, message);
    }

    private void ClearLines()
    {
        if(debugAreaText.text.Split('\n').Count() >= maxLines)
        {
            debugAreaText.text = string.Empty;
        }
    }

    private void SendToRemoteServer(string level, string timestamp, string message)
    {
        if (!enableRemoteDebug) return;
        StartCoroutine(PostLogCoroutine(level, timestamp, message));
    }

    private IEnumerator PostLogCoroutine(string level, string timestamp, string message)
    {
        string json = $"{{\"timestamp\":\"{EscapeJson(timestamp)}\",\"level\":\"{EscapeJson(level)}\",\"message\":\"{EscapeJson(message)}\"}}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(remoteServerUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 10;

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string body = request.downloadHandler != null ? request.downloadHandler.text : "";
                Debug.LogWarning(
                    $"[ARDebugManager] Remote log failed: {request.error} (HTTP {request.responseCode}) URL={remoteServerUrl} body={body}");
            }
            else if (!_loggedRemoteOkThisSession)
            {
                _loggedRemoteOkThisSession = true;
                Debug.Log($"[ARDebugManager] Remote log OK (first this session): {remoteServerUrl}");
            }
        }
    }

    private static string EscapeJson(string str)
    {
        if (string.IsNullOrEmpty(str)) return str;
        return str
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }
}