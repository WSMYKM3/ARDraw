using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DilmerGames.Core.Singletons;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// POSTs finished strokes to <see cref="stroke_server.py"/> (separate from debug log server).
/// </summary>
public class StrokeUploadManager : Singleton<StrokeUploadManager>
{
    private static readonly string SessionId = Guid.NewGuid().ToString("N");

    [Header("Stroke server (stroke_server.py)")]
    [SerializeField]
    private bool enableStrokeUpload = false;

    [Tooltip("Editor / same Mac: http://127.0.0.1:8081/stroke — iPhone: http://<Mac-LAN-IP>:8081/stroke")]
    [SerializeField]
    private string strokeServerUrl = "http://127.0.0.1:8081/stroke";

    private bool _loggedOkOnce;

    public void TryUploadStroke(int fingerId, ARLine line)
    {
        if (!enableStrokeUpload || line == null) return;

        var points = new List<Vector3>(64);
        if (!line.TryCopyWorldPositions(points) || points.Count < 1) return;

        StartCoroutine(PostStrokeCoroutine(fingerId, line.WasSmoothed, points));
    }

    private IEnumerator PostStrokeCoroutine(int fingerId, bool wasSmoothed, List<Vector3> points)
    {
        string json = BuildStrokeJson(fingerId, wasSmoothed, points);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(strokeServerUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 15;

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string body = request.downloadHandler != null ? request.downloadHandler.text : "";
                Debug.LogWarning(
                    $"[StrokeUpload] Failed: {request.error} (HTTP {request.responseCode}) URL={strokeServerUrl} body={body}");
            }
            else if (!_loggedOkOnce)
            {
                _loggedOkOnce = true;
                Debug.Log($"[StrokeUpload] OK (first this session): {strokeServerUrl}");
            }
        }
    }

    private string BuildStrokeJson(int fingerId, bool wasSmoothed, List<Vector3> points)
    {
        var sb = new StringBuilder(256 + points.Count * 40);
        var inv = CultureInfo.InvariantCulture;
        string created = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", inv);

        sb.Append("{\"sessionId\":\"").Append(EscapeJson(SessionId)).Append("\",");
        sb.Append("\"fingerId\":").Append(fingerId).Append(',');
        sb.Append("\"wasSmoothed\":").Append(wasSmoothed ? "true" : "false").Append(',');
        sb.Append("\"platform\":\"").Append(EscapeJson(Application.platform.ToString())).Append("\",");
        sb.Append("\"createdAt\":\"").Append(EscapeJson(created)).Append("\",");
        sb.Append("\"points\":[");

        for (int i = 0; i < points.Count; i++)
        {
            if (i > 0) sb.Append(',');
            Vector3 v = points[i];
            sb.Append('[')
                .Append(v.x.ToString(inv))
                .Append(',')
                .Append(v.y.ToString(inv))
                .Append(',')
                .Append(v.z.ToString(inv))
                .Append(']');
        }

        sb.Append("]}");
        return sb.ToString();
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
