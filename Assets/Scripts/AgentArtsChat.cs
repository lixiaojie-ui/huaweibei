using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
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
        Auto = 4,        // 依次尝试 API Key 相关各种，并在控制台打印成功的那一种
        IamToken = 6,    // 先用账号密码换取 IAM Token，再用 X-Auth-Token 调用
        AkSk = 7,        // 用 AK/SK 对请求做华为云 V4 签名，直接调用，无需密码
        ManualToken = 8  // 直接粘贴已获取的 IAM Token（24小时有效，无需密码）
    }

    [Header("运行时接口（AgentArts 控制台 -> 智能体运行时 -> 基本信息 -> 运行时接口，整条复制）")]
    public string invokeUrl = "https://{gateway_domain}/runtimes/{runtime_name}/invocations";

    [Header("API Key（只填 Key 本身，不要带 Bearer）")]
    public string apiKey = "";

    [Header("认证方式（遇到 401 选 Auto；若 Auto 全失败选 AkSk 或 IamToken）")]
    public AuthMode authMode = AuthMode.Bearer;

    [Header("AK/SK 配置（仅 authMode=AkSk 时使用，推荐，无需密码）")]
    [Tooltip("访问密钥 ID（控制台 -> 我的凭证 -> 访问密钥）")]
    public string accessKey = "";
    [Tooltip("秘密访问密钥")]
    public string secretKey = "";
    [Tooltip("区域，如 cn-southwest-2")]
    public string akSkRegion = "cn-southwest-2";
    [Tooltip("服务名，AgentArts 通常填 agentarts")]
    public string akSkService = "agentarts";

    [Header("IAM Token 配置（仅 authMode=IamToken 时使用，需填账号密码）")]
    [Tooltip("IAM 地址，按区域填，如 https://iam.cn-southwest-2.myhuaweicloud.com")]
    public string iamEndpoint = "https://iam.cn-southwest-2.myhuaweicloud.com";
    [Tooltip("华为云账号名（domain），不是 IAM 用户名")]
    public string iamDomain = "";
    [Tooltip("IAM 用户名")]
    public string iamUsername = "";
    [Tooltip("IAM 用户密码")]
    public string iamPassword = "";
    [Tooltip("项目名，通常等于区域名，如 cn-southwest-2")]
    public string iamProject = "cn-southwest-2";

    [Header("手动 Token（仅 authMode=ManualToken 时使用，无需密码）")]
    [Tooltip("从华为云控制台或 API Explorer 获取的 IAM Token，粘贴到这里，24小时有效")]
    public string manualToken = "";

    [Header("其他")]
    [Tooltip("区分终端用户，英文/数字/-/_，≤128 字符")]
    public string userId = "unity-editor-001";
    public int timeoutSeconds = 60;
    [Tooltip("网络错误或 5xx 时的重试次数")]
    public int maxRetry = 2;
    [Tooltip("把智能体返回的原始 SSE 打印到 Console，排查字段问题时打开")]
    public bool logRawResponse = true;

    // 同一次对话复用同一个 SessionId，Agent 侧才会关联上下文
    private string sessionId;

    // IAM Token 缓存（有效期 24 小时，避免每次请求都换）
    private string cachedIamToken;
    private float cachedIamTokenTime;

    // 请求体：AgentArts 低代码智能体要求字段名为 query
    [Serializable]
    private class Payload
    {
        public string query;
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

        if (url.Length == 0) return "Invoke Url 为空";
        if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return "Invoke Url 不是以 http 开头：" + url;
        if (url.Contains("{"))
            return "Invoke Url 还是占位符：" + url;
        if (!url.Contains("/invocations"))
            return "Invoke Url 缺少 /invocations：" + url;

        if (authMode == AuthMode.AkSk)
        {
            if (string.IsNullOrEmpty(accessKey)) return "accessKey（AK）为空";
            if (string.IsNullOrEmpty(secretKey)) return "secretKey（SK）为空";
            if (string.IsNullOrEmpty(akSkRegion)) return "akSkRegion 为空";
        }
        else if (authMode == AuthMode.IamToken)
        {
            if (string.IsNullOrEmpty(iamDomain)) return "iamDomain（账号名）为空";
            if (string.IsNullOrEmpty(iamUsername)) return "iamUsername 为空";
            if (string.IsNullOrEmpty(iamPassword)) return "iamPassword 为空";
            if (string.IsNullOrEmpty(iamProject)) return "iamProject 为空";
        }
        else if (authMode == AuthMode.ManualToken)
        {
            if (string.IsNullOrEmpty(manualToken)) return "manualToken 为空，请粘贴 IAM Token";
        }
        else if (Key.Length == 0)
        {
            return "API Key 为空（AkSk/IamToken/ManualToken 模式除外）";
        }
        return null;
    }

    /// <summary>发送一句话，ok 返回原始响应 JSON，fail 返回错误详情。</summary>
    public void Send(string text, Action<string> ok, Action<string> fail)
    {
        StartCoroutine(CoSend(text, ok, fail));
    }

    private IEnumerator CoSend(string text, Action<string> ok, Action<string> fail)
    {
        string body = JsonUtility.ToJson(new Payload { query = text });
        byte[] raw = Encoding.UTF8.GetBytes(body); // 必须显式 UTF-8，否则中文会乱码

        // IamToken 模式：先换取 IAM Token（带缓存，24 小时有效）
        string iamToken = null;
        if (authMode == AuthMode.IamToken)
        {
            yield return CoGetIamToken(t => iamToken = t, err =>
            {
                fail?.Invoke("获取 IAM Token 失败：" + err);
            });
            if (string.IsNullOrEmpty(iamToken)) yield break;
        }

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
                    req.SetRequestHeader("Accept", "text/event-stream");
                    req.SetRequestHeader("X-Hw-Agentarts-Session-Id", sessionId);
                    req.SetRequestHeader("X-Hw-Agentgateway-User-Id", userId);

                    if (mode == AuthMode.IamToken)
                    {
                        req.SetRequestHeader("X-Auth-Token", iamToken);
                    }
                    else if (mode == AuthMode.AkSk)
                    {
                        ApplyAkSkSignature(req, raw);
                    }
                    else if (mode == AuthMode.ManualToken)
                    {
                        req.SetRequestHeader("X-Auth-Token", manualToken);
                    }
                    else
                    {
                        ApplyAuth(req, mode);
                    }

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

                        if (logRawResponse)
                            Debug.Log("[AgentArts] 原始响应:\n" + req.downloadHandler.text);

                        string answer, sseError;
                        ParseSse(req.downloadHandler.text, out answer, out sseError);
                        if (!string.IsNullOrEmpty(sseError)) fail?.Invoke(sseError);
                        else ok?.Invoke(answer);
                        yield break;
                    }

                    code = req.responseCode;
                    error = $"[HTTP {code}] {req.error}\n{req.downloadHandler.text}";
                }

                lastError = error;

                // IamToken 的 401 说明 token 过期，清缓存后重新获取
                if (mode == AuthMode.IamToken && code == 401)
                {
                    cachedIamToken = null;
                    yield return CoGetIamToken(t => iamToken = t, err => { });
                    if (!string.IsNullOrEmpty(iamToken)) continue;
                }

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

    /// <summary>
    /// 用账号密码换取华为云 IAM Token。
    /// Token 在响应头 X-Subject-Token 中，有效期 24 小时。
    /// </summary>
    private IEnumerator CoGetIamToken(Action<string> ok, Action<string> fail)
    {
        // 命中缓存且未过期（24 小时 = 86400 秒）
        if (!string.IsNullOrEmpty(cachedIamToken) &&
            Time.realtimeSinceStartup - cachedIamTokenTime < 86400f)
        {
            ok?.Invoke(cachedIamToken);
            yield break;
        }

        string endpoint = (iamEndpoint ?? string.Empty).Trim();
        string domain = (iamDomain ?? string.Empty).Trim();
        string user = (iamUsername ?? string.Empty).Trim();
        string pass = (iamPassword ?? string.Empty);
        string project = (iamProject ?? string.Empty).Trim();

        if (endpoint.Length == 0) { fail?.Invoke("iamEndpoint 为空"); yield break; }
        if (domain.Length == 0) { fail?.Invoke("iamDomain（账号名）为空"); yield break; }
        if (user.Length == 0) { fail?.Invoke("iamUsername 为空"); yield break; }
        if (pass.Length == 0) { fail?.Invoke("iamPassword 为空"); yield break; }
        if (project.Length == 0) { fail?.Invoke("iamProject 为空"); yield break; }

        string tokenUrl = endpoint.TrimEnd('/') + "/v3/auth/tokens";
        string tokenBody = "{\"auth\":{\"identity\":{\"methods\":[\"password\"],\"password\":{\"user\":{\"name\":\""
            + EscapeJson(user) + "\",\"password\":\"" + EscapeJson(pass)
            + "\",\"domain\":{\"name\":\"" + EscapeJson(domain) + "\"}}}},\"scope\":{\"project\":{\"name\":\""
            + EscapeJson(project) + "\"}}}}";
        byte[] tokenRaw = Encoding.UTF8.GetBytes(tokenBody);

        using (var req = new UnityWebRequest(tokenUrl, UnityWebRequest.kHttpVerbPOST))
        {
            req.uploadHandler = new UploadHandlerRaw(tokenRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 30;

            yield return req.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            bool success = req.result == UnityWebRequest.Result.Success;
#else
            bool success = !req.isNetworkError && !req.isHttpError;
#endif

            if (!success)
            {
                fail?.Invoke($"[HTTP {req.responseCode}] {req.error}\n{req.downloadHandler.text}");
                yield break;
            }

            string token = req.GetResponseHeader("X-Subject-Token");
            if (string.IsNullOrEmpty(token))
            {
                fail?.Invoke("IAM 响应头中没有 X-Subject-Token，响应：" + req.downloadHandler.text);
                yield break;
            }

            cachedIamToken = token;
            cachedIamTokenTime = Time.realtimeSinceStartup;
            Debug.Log("[AgentArts] IAM Token 获取成功，长度：" + token.Length);
            ok?.Invoke(token);
        }
    }

    static string EscapeJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
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

    // ============================================================ 华为云 AK/SK V4 签名

    /// <summary>
    /// 对请求做华为云 SDK-HMAC-SHA256 V4 签名，写入 Authorization 头。
    /// 参考：https://support.huaweicloud.com/devg-apisign/api-sign-spec.html
    /// </summary>
    void ApplyAkSkSignature(UnityWebRequest req, byte[] body)
    {
        string host = GetHost(Url);
        string payloadHash = Sha256Hex(body);

        // 设置参与签名的头（Host 由 Unity 自动设置，这里不手动设置避免冲突）
        req.SetRequestHeader("X-Content-Sha256", payloadHash);

        // 收集要签名的头：content-type / host / x-content-sha256 / x-hw-agentarts-session-id / x-hw-agentgateway-user-id
        var headers = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "content-type", "application/json" },
            { "host", host },
            { "x-content-sha256", payloadHash },
            { "x-hw-agentarts-session-id", sessionId },
            { "x-hw-agentgateway-user-id", userId }
        };

        // 构造 SignedHeaders
        var signedHeaders = new StringBuilder();
        foreach (var kv in headers)
        {
            if (signedHeaders.Length > 0) signedHeaders.Append(';');
            signedHeaders.Append(kv.Key.ToLowerInvariant());
        }

        // 构造 CanonicalHeaders
        var canonicalHeaders = new StringBuilder();
        foreach (var kv in headers)
        {
            canonicalHeaders.Append(kv.Key.ToLowerInvariant()).Append(':').Append(kv.Value.Trim()).Append('\n');
        }

        // 构造 CanonicalRequest
        string uri = GetCanonicalUri(Url);
        var canonicalRequest = new StringBuilder();
        canonicalRequest.Append("POST\n");
        canonicalRequest.Append(uri).Append('\n');
        canonicalRequest.Append("\n");                       // query string（无）
        canonicalRequest.Append(canonicalHeaders.ToString()).Append('\n');
        canonicalRequest.Append(signedHeaders.ToString()).Append('\n');
        canonicalRequest.Append(payloadHash);

        string canonicalRequestHash = Sha256Hex(Encoding.UTF8.GetBytes(canonicalRequest.ToString()));

        DateTime now = DateTime.UtcNow;
        string timestamp = now.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture);
        string date = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        string scope = date + "/" + akSkRegion + "/" + akSkService + "/sdk_request";

        var stringToSign = new StringBuilder();
        stringToSign.Append("SDK-HMAC-SHA256\n");
        stringToSign.Append(timestamp).Append('\n');
        stringToSign.Append(scope).Append('\n');
        stringToSign.Append(canonicalRequestHash);

        byte[] kDate = HmacSha256(Encoding.UTF8.GetBytes(secretKey), date);
        byte[] kRegion = HmacSha256(kDate, akSkRegion);
        byte[] kService = HmacSha256(kRegion, akSkService);
        byte[] kSigning = HmacSha256(kService, "sdk_request");
        byte[] signatureBytes = HmacSha256(kSigning, stringToSign.ToString());
        string signature = HexEncode(signatureBytes);

        string authorization = string.Format(CultureInfo.InvariantCulture,
            "SDK-HMAC-SHA256 Access={0}, SignedHeaders={1}, Signature={2}",
            accessKey, signedHeaders.ToString(), signature);

        req.SetRequestHeader("Authorization", authorization);
        req.SetRequestHeader("X-Sdk-Date", timestamp);
    }

    static string GetHost(string url)
    {
        try
        {
            var uri = new Uri(url);
            return uri.Host;
        }
        catch { return ""; }
    }

    static string GetCanonicalUri(string url)
    {
        try
        {
            var uri = new Uri(url);
            return Uri.EscapeDataString(uri.AbsolutePath).Replace("%2F", "/");
        }
        catch { return "/"; }
    }

    static string HexEncode(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (byte b in bytes) sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
        return sb.ToString();
    }

    static string Sha256Hex(byte[] data)
    {
        using (var sha = SHA256.Create())
        {
            return HexEncode(sha.ComputeHash(data));
        }
    }

    static byte[] HmacSha256(byte[] key, string data)
    {
        using (var hmac = new HMACSHA256(key))
        {
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        }
    }

    /// <summary>
    /// 解析 SSE 流。分片形如：
    ///   data: {"event":"message","data":{"content":"...","message":""}}
    ///   data: {"event":"error","data":{"code":100002,"message":"..."}}
    ///   data: {"event":"done"}
    /// 只有 event=error 才算失败，其余按 content（兜底 message）拼接；相同分片会去重。
    /// </summary>
    private static void ParseSse(string raw, out string answer, out string error)
    {
        answer = null;
        error = null;

        if (string.IsNullOrEmpty(raw))
        {
            error = "响应为空";
            return;
        }

        // 不是流式，直接当普通 JSON / 文本处理
        if (!raw.Contains("data:"))
        {
            string ev0 = JsonValue(raw, "event");
            if (ev0 == "error") { error = ExtractSseError(raw); return; }
            answer = raw;
            return;
        }

        var sb = new StringBuilder();
        string last = null;

        foreach (string line in raw.Split('\n'))
        {
            string t = line.Trim();
            if (!t.StartsWith("data:")) continue;

            string json = t.Substring(5).Trim();
            if (json.Length == 0) continue;

            string ev = JsonValue(json, "event");

            if (ev == "error")
            {
                error = ExtractSseError(json);
                return;
            }
            if (ev == "done") break;

            // 正常分片：优先 content，其次 message（两者都在 data 对象里）
            string text = JsonValue(json, "content");
            if (string.IsNullOrEmpty(text)) text = JsonValue(json, "message");
            if (string.IsNullOrEmpty(text)) continue;

            // 有些实现每个分片都发累计全文，这里去掉重复拼接
            if (text == last) continue;

            sb.Append(text);
            last = text;
        }

        answer = sb.Length > 0 ? sb.ToString() : raw;
    }

    /// <summary>取 JSON 里某个键的字符串值；值为 null、数字或不存在的都返回 null。</summary>
    static string JsonValue(string json, string key)
    {
        var m = Regex.Match(json, "\"" + key + "\"\\s*:\\s*(\"(?:[^\"\\\\]|\\\\.)*\")");
        if (!m.Success) return null;

        string s = m.Groups[1].Value;
        s = s.Substring(1, s.Length - 2); // 去掉两侧引号

        try { return Regex.Unescape(s); }
        catch { return s; }
    }

    /// <summary>从错误 JSON 里取出 code 和 message，拼成一句人能看懂的提示。</summary>
    static string ExtractSseError(string json)
    {
        string msg = JsonValue(json, "message");
        if (string.IsNullOrEmpty(msg)) msg = JsonValue(json, "error_msg");
        if (string.IsNullOrEmpty(msg)) return "AgentArts 返回错误：\n" + json;

        var c = Regex.Match(json, "\"code\"\\s*:\\s*(\\d+)");
        string code = c.Success ? c.Groups[1].Value : null;

        return "AgentArts 返回错误" + (code != null ? "（code " + code + "）" : "") + "：\n" + msg;
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
