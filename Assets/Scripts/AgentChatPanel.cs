using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Canvas main6 智能体问答面板：把输入框、发送按钮、回答框和 AgentArtsChat 连起来。
/// 支持多个智能体：面板顶部标签切换，Button robot 进入页面时默认选中机器人智能体。
/// </summary>
public class AgentChatPanel : MonoBehaviour
{
    [Header("第一个智能体（兼容旧配置）")]
    public AgentArtsChat agent;

    [Header("多智能体（填了就优先用这个列表；第 1 个与上面的 agent 相同）")]
    public AgentArtsChat[] agents = new AgentArtsChat[0];
    public Button[] agentTabButtons = new Button[0];
    public Text[] agentTabLabels = new Text[0];
    public string[] agentDisplayNames = new string[0];

    [Tooltip("面板第一次打开时默认选中的智能体下标")]
    public int initialAgentIndex = 0;
    [Tooltip("点击 Button robot 进入 Canvas main6 时选中的智能体下标（第二个智能体通常是 1）")]
    public int robotAgentIndex = 1;

    [Header("标签配色")]
    public Color activeTabColor = new Color(0.235f, 0.851f, 1f, 1f);
    public Color inactiveTabColor = new Color(0.486f, 0.576f, 0.647f, 1f);
    public Color activeTabBackground = new Color(0.075f, 0.263f, 0.360f, 0.72f);
    public Color inactiveTabBackground = new Color(0.027f, 0.086f, 0.145f, 0.58f);

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

    // 每个智能体各保留自己的对话记录，切换标签时不会串台
    readonly List<StringBuilder> histories = new List<StringBuilder>();
    int currentAgentIndex = -1;
    bool initialized;
    bool busy;

    int AgentCount
    {
        get { return agents != null && agents.Length > 0 ? agents.Length : (agent != null ? 1 : 0); }
    }

    AgentArtsChat AgentAt(int index)
    {
        if (agents != null && agents.Length > 0)
            return index >= 0 && index < agents.Length ? agents[index] : null;

        return index == 0 ? agent : null;
    }

    void Awake()
    {
        EnsureInitialized();
    }

    void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;

        histories.Clear();
        for (int i = 0; i < AgentCount; i++) histories.Add(new StringBuilder());
        if (histories.Count == 0) histories.Add(new StringBuilder()); // 配置为空时也能显示提示

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

        // 智能体切换标签
        if (agentTabButtons != null)
        {
            for (int i = 0; i < agentTabButtons.Length; i++)
            {
                int index = i;
                Button button = agentTabButtons[i];
                if (button != null) button.onClick.AddListener(() => SelectAgent(index));
            }
        }

        if (agent == null) agent = AgentAt(0);

        if (AgentCount > 0)
            SelectAgent(Mathf.Clamp(initialAgentIndex, 0, AgentCount - 1), true);
        else
            SetStatus(notConfiguredText);
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

    /// <summary>切换当前使用的智能体，各自保留对话记录。</summary>
    public void SelectAgent(int index)
    {
        EnsureInitialized();
        SelectAgent(index, false);
    }

    void SelectAgent(int index, bool force)
    {
        if (AgentCount == 0) return;
        if (index < 0 || index >= AgentCount) return;
        if (!force && index == currentAgentIndex) return;
        if (busy && !force) return;

        currentAgentIndex = index;
        agent = AgentAt(index);

        if (answerText != null && index < histories.Count)
            answerText.text = histories[index].ToString();

        RefreshTabs();
        SetStatus(readyText);
        ScrollToBottom();

        if (!busy && questionInput != null && gameObject.activeInHierarchy)
            questionInput.ActivateInputField();
    }

    void RefreshTabs()
    {
        if (agentTabButtons == null) return;

        for (int i = 0; i < agentTabButtons.Length; i++)
        {
            Button button = agentTabButtons[i];
            if (button != null)
            {
                button.interactable = !busy;

                Graphic graphic = button.targetGraphic;
                if (graphic != null)
                    graphic.color = i == currentAgentIndex ? activeTabBackground : inactiveTabBackground;
            }

            if (agentTabLabels != null && i < agentTabLabels.Length && agentTabLabels[i] != null)
                agentTabLabels[i].color = i == currentAgentIndex ? activeTabColor : inactiveTabColor;
        }
    }

    public void Ask(string question)
    {
        EnsureInitialized();
        if (busy || string.IsNullOrEmpty(question)) return;

        int index = Mathf.Max(0, currentAgentIndex);
        AgentArtsChat activeAgent = AgentAt(index);

        if (activeAgent == null)
        {
            Append(index, "系统", notConfiguredText);
            return;
        }

        string problem = activeAgent.ConfigProblem();
        if (problem != null)
        {
            Append(index, "系统",
                "配置未生效：" + problem +
                "\n当前接口地址：" + (activeAgent.Url.Length == 0 ? "(空)" : activeAgent.Url) +
                "\n当前 Key 长度：" + activeAgent.Key.Length +
                "\n改完 Inspector 后请 Ctrl+S 保存场景，再重新 Play。");
            return;
        }

        Append(index, "我", question);
        busy = true;
        if (sendButton != null) sendButton.interactable = false;
        SetStatus(thinkingText);
        RefreshTabs();

        activeAgent.Send(question,
            json =>
            {
                Finish();
                Append(index, "智能体", AgentArtsChat.ExtractText(json));
            },
            error =>
            {
                Finish();
                Append(index, "系统", "请求失败：" + error);
                if (error.Contains("401"))
                    Append(index, "系统",
                        "五种认证头都返回 401，说明不是请求头写法问题：\n" +
                        "1. 该 Key 可能未授权调用这个运行时（检查运行时是否已发布、是否绑定该 Key）；\n" +
                        "2. Key 与运行时可能不在同一区域/项目；\n" +
                        "3. 运行时可能要求 IAM 认证而非 API Key：\n" +
                        "   → 免密码方案：authMode 改 ManualToken，从浏览器控制台取 IAM Token 粘贴进去（24小时有效）；\n" +
                        "   → 或 authMode 改 IamToken，填写账号名/用户名/密码自动换 Token。");
            });
    }

    public void NewSession()
    {
        EnsureInitialized();

        int index = Mathf.Max(0, currentAgentIndex);
        AgentArtsChat activeAgent = AgentAt(index);
        if (activeAgent != null) activeAgent.NewSession();

        if (index < histories.Count) histories[index].Length = 0;
        if (answerText != null) answerText.text = string.Empty;

        Append(index, "系统", "已新建会话，上下文已清空。");
        SetStatus(readyText);
    }

    void Finish()
    {
        busy = false;
        if (sendButton != null) sendButton.interactable = true;
        SetStatus(readyText);
        RefreshTabs();

        if (questionInput != null && gameObject.activeInHierarchy)
            questionInput.ActivateInputField();
    }

    void Append(int index, string speaker, string content)
    {
        if (index < 0) index = 0;
        while (histories.Count <= index) histories.Add(new StringBuilder());

        StringBuilder history = histories[index];
        if (history.Length > 0) history.Append('\n');
        history.Append("<color=#6FA9C4>").Append(speaker).Append("</color>\n");
        history.Append(content).Append('\n');

        if (answerText == null) return;
        if (index != Mathf.Max(0, currentAgentIndex)) return; // 不是当前标签的内容，等切回来再显示

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
