using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Canvas main6 智能体问答面板：把输入框、发送按钮、回答框和 AgentArtsChat 连起来。
/// </summary>
public class AgentChatPanel : MonoBehaviour
{
    public AgentArtsChat agent;
    public InputField questionInput;
    public Text answerText;
    public Button sendButton;
    public Button newSessionButton;
    public ScrollRect answerScroll;
    public Text statusText;
    public Button[] quickButtons = new Button[0];

    public string readyText = "会话已就绪 · 输入问题后点击发送";
    public string thinkingText = "智能体正在思考…";
    public string notConfiguredText = "尚未配置 AgentArts 运行时接口或 API Key，请在 agentClient 上填写后重试。";

    readonly StringBuilder history = new StringBuilder();
    bool busy;

    void Awake()
    {
        if (sendButton != null) sendButton.onClick.AddListener(SendCurrent);
        if (newSessionButton != null) newSessionButton.onClick.AddListener(NewSession);
        if (questionInput != null) questionInput.onEndEdit.AddListener(OnEndEdit);

        for (int i = 0; i < quickButtons.Length; i++)
        {
            Button button = quickButtons[i];
            if (button == null) continue;

            Text label = button.GetComponentInChildren<Text>(true);
            if (label == null) continue;

            string question = label.text;
            button.onClick.AddListener(() => Ask(question));
        }

        SetStatus(readyText);
    }

    void OnEndEdit(string value)
    {
        if (busy) return;
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            SendCurrent();
    }

    public void SendCurrent()
    {
        if (questionInput == null) return;

        string question = (questionInput.text ?? string.Empty).Trim();
        if (question.Length == 0) return;

        questionInput.text = string.Empty;
        Ask(question);
    }

    public void Ask(string question)
    {
        if (busy || string.IsNullOrEmpty(question)) return;

        if (agent == null)
        {
            Append("系统", notConfiguredText);
            return;
        }

        string problem = agent.ConfigProblem();
        if (problem != null)
        {
            Append("系统",
                "配置未生效：" + problem +
                "\n当前接口地址：" + (agent.Url.Length == 0 ? "(空)" : agent.Url) +
                "\n当前 Key 长度：" + agent.Key.Length +
                "\n改完 Inspector 后请 Ctrl+S 保存场景，再重新 Play。");
            return;
        }

        Append("我", question);
        busy = true;
        if (sendButton != null) sendButton.interactable = false;
        SetStatus(thinkingText);

        agent.Send(question,
            json =>
            {
                Finish();
                Append("智能体", AgentArtsChat.ExtractText(json));
            },
            error =>
            {
                Finish();
                Append("系统", "请求失败：" + error);
                if (error.Contains("401"))
                    Append("系统",
                        "五种认证头都返回 401，说明不是请求头写法问题：\n" +
                        "1. 该 Key 可能未授权调用这个运行时（检查运行时是否已发布、是否绑定该 Key）；\n" +
                        "2. Key 与运行时可能不在同一区域/项目；\n" +
                        "3. 若网关要求 IAM Token 而非 API Key/AppCode，需要先用 AK/SK 换取 Token。");
            });
    }

    public void NewSession()
    {
        if (agent != null) agent.NewSession();

        history.Length = 0;
        if (answerText != null) answerText.text = string.Empty;
        Append("系统", "已新建会话，上下文已清空。");
        SetStatus(readyText);
    }

    void Finish()
    {
        busy = false;
        if (sendButton != null) sendButton.interactable = true;
        SetStatus(readyText);
        if (questionInput != null) questionInput.ActivateInputField();
    }

    void Append(string speaker, string content)
    {
        if (history.Length > 0) history.Append('\n');
        history.Append("<color=#6FA9C4>").Append(speaker).Append("</color>\n");
        history.Append(content).Append('\n');

        if (answerText == null) return;

        answerText.text = history.ToString();
        ScrollToBottom();
    }

    void ScrollToBottom()
    {
        if (answerScroll == null) return;

        Canvas.ForceUpdateCanvases();
        answerScroll.verticalNormalizedPosition = 0f;
    }

    void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }
}
