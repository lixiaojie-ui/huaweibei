using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Daima.EditorTools
{
    /// <summary>
    /// 在 Canvas main6 的智能体面板里加入第二个智能体：
    /// agentClient2（第二个 AgentArts 运行时）+ 顶部“智能助手 / 机器人助手”切换标签。
    /// Button robot 进入 main6 时默认选中第二个智能体。
    /// 只执行一次；也可以在菜单「工具/智能体」里手动重新执行。
    /// </summary>
    [InitializeOnLoad]
    public static class AgentSecondAgentSetup
    {
        const string MenuPath = "工具/智能体/配置第二个智能体（Button robot）";
        const string DoneKeyPrefix = "Daima.AgentSecondAgent.SetupDone.v1.";

        static AgentSecondAgentSetup()
        {
            EditorApplication.delayCall += TryAutoSetup;
        }

        [MenuItem(MenuPath)]
        public static void RunManual()
        {
            Setup(true);
        }

        static void TryAutoSetup()
        {
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TryAutoSetup;
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.name != "SampleScene") return;
            if (EditorPrefs.GetBool(DoneKeyPrefix + scene.path, false)) return;

            Setup(false);
        }

        static void Setup(bool manual)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded) return;

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[Agent2] Play 模式下不修改场景，请先退出 Play。");
                return;
            }

            Transform main6 = FindDeep("Canvas main6");
            Transform page = main6 != null ? main6.Find("agentPage") : null;
            Transform root = page != null ? page.Find("agentRoot") : null;
            Transform body = root != null ? root.Find("body") : null;
            Transform client1Tf = page != null ? page.Find("agentClient") : null;
            Transform quickTemplate = body != null ? body.Find("quick0") : null;

            if (main6 == null || page == null || root == null || body == null ||
                client1Tf == null || quickTemplate == null)
            {
                if (manual)
                    Debug.LogError("[Agent2] 场景结构不完整：需要 Canvas main6/agentPage/agentRoot/body/" +
                                   "agentClient/quick0。");
                return;
            }

            AgentChatPanel panel = root.GetComponent<AgentChatPanel>();
            AgentArtsChat agent1 = client1Tf.GetComponent<AgentArtsChat>();
            if (panel == null || agent1 == null)
            {
                if (manual)
                    Debug.LogError("[Agent2] agentRoot 上没有 AgentChatPanel，或 agentClient 上没有 AgentArtsChat。");
                return;
            }

            bool sceneWasDirty = scene.isDirty;
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("配置第二个智能体");

            // ---------- 第二个智能体运行时 ----------
            Transform client2Tf = page.Find("agentClient2");
            GameObject client2Go;

            if (client2Tf == null)
            {
                client2Go = new GameObject("agentClient2", typeof(RectTransform));
                client2Go.transform.SetParent(page, false);
                Undo.RegisterCreatedObjectUndo(client2Go, "Create agentClient2");
            }
            else
            {
                client2Go = client2Tf.gameObject;
            }

            RectTransform client2Rect = client2Go.GetComponent<RectTransform>();
            client2Rect.anchorMin = new Vector2(0f, 1f);
            client2Rect.anchorMax = new Vector2(0f, 1f);
            client2Rect.pivot = new Vector2(0f, 1f);
            client2Rect.anchoredPosition = Vector2.zero;
            client2Rect.sizeDelta = Vector2.zero;
            client2Rect.localScale = Vector3.one;

            AgentArtsChat agent2 = client2Go.GetComponent<AgentArtsChat>();
            bool isNewClient = agent2 == null;
            if (isNewClient) agent2 = client2Go.AddComponent<AgentArtsChat>();

            // 只在第一次创建时写默认值，之后用户填的 Invoke Url / API Key 不会被覆盖
            if (isNewClient)
            {
                agent2.invokeUrl = "";
                agent2.apiKey = "";
                agent2.authMode = AgentArtsChat.AuthMode.Bearer;
                agent2.userId = "unity-robot-002";
                agent2.timeoutSeconds = 120;
                agent2.maxRetry = 2;
                agent2.logRawResponse = true;
            }

            // ---------- 顶部切换标签 ----------
            Button tab0 = CreateTab(body, quickTemplate, "agentTab0", new Vector2(560f, -24f), "智能助手");
            Button tab1 = CreateTab(body, quickTemplate, "agentTab1", new Vector2(722f, -24f), "机器人助手");

            Text label0 = tab0 != null ? tab0.GetComponentInChildren<Text>(true) : null;
            Text label1 = tab1 != null ? tab1.GetComponentInChildren<Text>(true) : null;

            // ---------- 写回面板引用 ----------
            panel.agent = agent1;
            panel.agents = new[] { agent1, agent2 };
            panel.agentTabButtons = new[] { tab0, tab1 };
            panel.agentTabLabels = new[] { label0, label1 };
            panel.agentDisplayNames = new[] { "智能助手", "机器人助手" };
            panel.initialAgentIndex = 1;
            panel.robotAgentIndex = 1;

            EditorUtility.SetDirty(panel);
            EditorUtility.SetDirty(agent2);
            EditorUtility.SetDirty(client2Go);
            if (tab0 != null) EditorUtility.SetDirty(tab0.gameObject);
            if (tab1 != null) EditorUtility.SetDirty(tab1.gameObject);

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(scene);

            if (!sceneWasDirty)
            {
                EditorSceneManager.SaveScene(scene);
                EditorPrefs.SetBool(DoneKeyPrefix + scene.path, true);
            }
            else
            {
                Debug.LogWarning("[Agent2] 场景里原本有未保存改动，本次没有自动保存。" +
                                 "第二个智能体已加入内存，请确认后 Ctrl+S 保存场景。");
            }

            Debug.Log("[Agent2] 第二个智能体已接入 Canvas main6：\n" +
                      "1) 点 Button robot 进入 main6 时默认选中“机器人助手”；\n" +
                      "2) 面板顶部可在“智能助手 / 机器人助手”之间切换；\n" +
                      "3) 第二个运行时的 Invoke Url 和 API Key 请填在 agentClient2 组件上。");
        }

        static Button CreateTab(Transform body, Transform template, string name,
                                Vector2 position, string labelText)
        {
            Transform existing = body.Find(name);
            GameObject go;

            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = Object.Instantiate(template.gameObject, body);
                go.name = name;
                go.transform.SetSiblingIndex(template.GetSiblingIndex() + 1);
                Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            }

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(148f, 30f);
            rect.localScale = Vector3.one;

            Transform labelTf = go.transform.Find("label");
            if (labelTf == null)
            {
                var labelGo = new GameObject("label", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(Text));
                labelGo.transform.SetParent(go.transform, false);
                labelTf = labelGo.transform;
                Undo.RegisterCreatedObjectUndo(labelGo, "Create tab label");
            }

            RectTransform labelRect = labelTf.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = Vector2.zero;

            Text text = labelTf.GetComponent<Text>();
            if (text == null) text = labelTf.gameObject.AddComponent<Text>();
            text.text = labelText;
            text.fontSize = 12;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.486f, 0.576f, 0.647f, 1f);
            text.raycastTarget = false;

            Button button = go.GetComponent<Button>();
            if (button == null) button = go.AddComponent<Button>();

            Graphic graphic = go.GetComponent<Graphic>();
            if (graphic != null) button.targetGraphic = graphic;

            return button;
        }

        static Transform FindDeep(string name)
        {
            Transform[] all = Object.FindObjectsOfType<Transform>(true);
            foreach (Transform t in all)
            {
                if (t != null && t.name == name) return t;
            }
            return null;
        }
    }
}
