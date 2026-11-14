using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 华为云 AgentArts 智能体运行时的最小 Unity 客户端。
/// 只负责发请求，界面交互交给 AgentChatPanel。
/// </summary>
public class AgentArtsChat : MonoBehaviour
{
    /// <summary>网关要求的鉴权头形式，401 时可逐个切换排查。</summary>
    public enum AuthMode
    {
        Bearer = 0,      // Authorization: Bearer <Key>
        RawKey = 1,      // Authorization: <Key>
        XAuthToken = 2,  // X-Auth-Token: <Key>
        XApiKey = 3,     // X-Api-Key: <Key>
        AppCode = 5,     // X-Apig-AppCode: <Key>（华为云 API 网关的 AppCode 简易认证）
        Auto = 4         // 依次尝试上面各种，并在控制台打印成功的那一种
    }

    [Header("运行时接口（AgentArts 控制台 -> 智能体运行时 -> 基本信息 -> 运行时接口，整条复制）")]
    public string invokeUrl = "https://{gateway_domain}/runtimes/{runtime_name}/invocations";

    [Header("API Key（只填 Key 本身，不要带 Bearer）")]
    public string apiKey = "";

    [Header("认证方式（遇到 401 选 Auto，会自动试四种并打印成功的那种）")]
    public AuthMode authMode = AuthMode.Bearer;

    [Header("其他")]
    [Tooltip("区分终端用户，英文/数字/-/_，≤128 字符")]
    public string userId = "unity-editor-001";
    public int timeoutSeconds = 60;
    [Tooltip("网络错误或 5xx 时的重试次数")]
    public int maxRetry = 2;

    // 同一次对话复用同一个 SessionId，Agent 侧才会关联上下文
    private string sessionId;

    // 请求体：AgentArts 运行时用 input 字段；同时带一份 message 兼容自定义服务
    [Serializable]
    private class Payload
    {
        public string input;
        public string message;
    }

    void Awake()
    {
        NewSession();
    }

    public void NewSession()
    {
        sessionId = Guid.NewGuid().ToString("N");
    }

    /// <summary>去掉首尾空格后的接口地址。</summary>
    public string Url
    {
        get { return (invokeUrl ?? string.Empty).Trim(); }
    }

    /// <summary>去掉首尾空格后的 API Key。</summary>
    public string Key
    {
        get { return (apiKey ?? string.Empty).Trim(); }
    }

    /// <summary>接口和 Key 是否已经填写完成。</summary>
    public bool IsConfigured
    {
        get { return ConfigProblem() == null; }
    }

    /// <summary>返回配置问题描述；配置正常时返回 null。</summary>
    public string ConfigProblem()
    {
        string url = Url;
        string key = Key;

        if (key.Length == 0) return "API Key 为空";
        if (url.Length == 0) return "Invoke Url 为空";
        if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return "Invoke Url 不是以 http 开头：" + url;
        if (url.Contains("{"))
            return "Invoke Url 还是占位符：" + url;
        if (!url.Contains("/invocations"))
            return "Invoke Url 缺少 /invocations：" + url;
        return null;
    }

    /// <summary>发送一句话，ok 返回原始响应 JSON，fail 返回错误详情。</summary>
    public void Send(string text, Action<string> ok, Action<string> fail)
    {
        StartCoroutine(CoSend(text, ok, fail));
    }

    private IEnumerator CoSend(string text, Action<string> ok, Action<string> fail)
    {
        string body = JsonUtility.ToJson(new Payload { input = text, message = text });
        byte[] raw = Encoding.UTF8.GetBytes(body); // 必须显式 UTF-8，否则中文会乱码

        AuthMode[] modes = authMode == AuthMode.Auto
            ? new[] { AuthMode.Bearer, AuthMode.RawKey, AuthMode.XAuthToken, AuthMode.XApiKey, AuthMode.AppCode }
            : new[] { authMode };

        string lastError = null;

        foreach (AuthMode mode in modes)
        {
            for (int attempt = 0; attempt <= maxRetry; attempt++)
            {
                long code = 0;
                string error = null;

                using (var req = new UnityWebRequest(Url, UnityWebRequest.kHttpVerbPOST))
                {
                    req.uploadHandler = new UploadHandlerRaw(raw);
                    req.downloadHandler = new DownloadHandlerBuffer();
                    req.SetRequestHeader("Content-Type", "application/json");
                    ApplyAuth(req, mode);
                    req.SetRequestHeader("X-Hw-Agentarts-Session-Id", sessionId);
                    req.SetRequestHeader("X-Hw-Agentgateway-User-Id", userId);
                    req.timeout = timeoutSeconds;

                    yield return req.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
                    bool success = req.result == UnityWebRequest.Result.Success;
#else
                    bool success = !req.isNetworkError && !req.isHttpError;
#endif

                    if (success)
                    {
                        if (modes.Length > 1)
                        {
                            authMode = mode;
                            Debug.Log("[AgentArts] 认证成功的方式：" + mode);
                        }
                        ok?.Invoke(req.downloadHandler.text);
                        yield break;
                    }

                    code = req.responseCode;
                    error = $"[HTTP {code}] {req.error}\n{req.downloadHandler.text}";
                }

                lastError = error;

                // 401 换个认证方式再试；网络错误和 5xx 才重试同一种
                if (code == 401) break;

                bool retryable = code == 0 || code >= 500;
                if (!retryable || attempt == maxRetry)
                {
                    fail?.Invoke(error);
                    yield break;
                }

                yield return new WaitForSecondsRealtime(1f + attempt);
            }
        }

        fail?.Invoke(lastError ?? "请求失败");
    }

    void ApplyAuth(UnityWebRequest req, AuthMode mode)
    {
        switch (mode)
        {
            case AuthMode.RawKey:
                req.SetRequestHeader("Authorization", Key);
                break;
            case AuthMode.XAuthToken:
                req.SetRequestHeader("X-Auth-Token", Key);
                break;
            case AuthMode.XApiKey:
                req.SetRequestHeader("X-Api-Key", Key);
                break;
            case AuthMode.AppCode:
                req.SetRequestHeader("X-Apig-AppCode", Key);
                break;
            default:
                req.SetRequestHeader("Authorization", "Bearer " + Key);
                break;
        }
    }

    /// <summary>
    /// 容错取值：不同 Agent 返回字段不一样（output/reply/answer/content...），
    /// 依次尝试，都取不到就把原始 JSON 原样返回，方便确认字段名。
    /// </summary>
    public static string ExtractText(string json)
    {
        if (string.IsNullOrEmpty(json)) return "(空响应)";

        foreach (var key in new[] { "output", "reply", "answer", "content", "text", "result" })
        {
            var m = Regex.Match(json, "\"" + key + "\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");
            if (!m.Success) continue;
            try { return Regex.Unescape(m.Groups[1].Value); }
            catch { return m.Groups[1].Value; }
        }
        return json;
    }
}
