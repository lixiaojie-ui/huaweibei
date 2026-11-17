using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Daima
{
    /// <summary>
    /// 三个子页签的“零配置”互斥版：
    /// 不需要挂组件、不需要拖引用、不需要改按钮原有的 OnClick。
    ///
    /// 场景加载后，自动读取三个按钮 OnClick 里已经配好的 SetActive 调用，
    /// 把每个按钮 SetActive 的对象当成它自己的效果清单。每次点击时：
    ///   1. 把另外两个页签清单里的对象 SetActive(false)；
    ///   2. 把当前页签清单里的对象 SetActive(true)（处理两个页签共用一个物体的情况）；
    ///   3. 取消另外两个页签还没飞完的相机，当前页签自己的相机飞行保持不动。
    ///
    /// 所以每个页签的效果统一按这个约定配：
    ///   按钮 OnClick → SetActive(true) → 本页签要显示的对象。
    /// 相机视角照旧用 CameraSwitchToPoint 的 Go / GoTo，每个页签可以各配各的。
    /// </summary>
    public static class SubTabEffectAuto
    {
        static readonly string[] TabNames = { "chanxianzonglan", "shenchandonghua", "shebeixinxi" };

        sealed class TabInfo
        {
            public string name;
            public Button button;
            public readonly List<GameObject> effects = new List<GameObject>();
            public readonly List<CameraSwitchToPoint> cameras = new List<CameraSwitchToPoint>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            // 场景里如果已经挂了并配置了手动版总控，就交给它，避免两套逻辑同时跑。
            foreach (SubTabEffectSwitcher manual in Object.FindObjectsOfType<SubTabEffectSwitcher>(true))
            {
                if (manual != null && manual.HasAnyEffect())
                {
                    Debug.Log("[SubTabEffectAuto] 检测到已配置的 SubTabEffectSwitcher，自动版不启用。");
                    return;
                }
            }

            List<TabInfo> tabs = new List<TabInfo>();

            foreach (string tabName in TabNames)
            {
                Button button = FindButton(tabName);
                if (button == null)
                {
                    Debug.LogWarning("[SubTabEffectAuto] 没找到按钮：" + tabName);
                    continue;
                }

                TabInfo tab = new TabInfo { name = tabName, button = button };
                ReadWiring(tab);
                tabs.Add(tab);

                Debug.Log("[SubTabEffectAuto] " + tabName +
                    "：SetActive 效果对象 " + tab.effects.Count +
                    " 个，相机 " + tab.cameras.Count + " 个");
            }

            foreach (TabInfo tab in tabs)
            {
                TabInfo captured = tab;
                tab.button.onClick.AddListener(() => Apply(tabs, captured));
            }
        }

        static Button FindButton(string tabName)
        {
            foreach (Button button in Object.FindObjectsOfType<Button>(true))
            {
                if (button == null) continue;
                if (button.gameObject.name.ToLowerInvariant().Contains(tabName))
                    return button;
            }
            return null;
        }

        /// <summary>读取按钮 OnClick 里已经配好的持久化事件，整理出这个页签的效果对象和相机。</summary>
        static void ReadWiring(TabInfo tab)
        {
            int count = tab.button.onClick.GetPersistentEventCount();
            for (int i = 0; i < count; i++)
            {
                Object target = tab.button.onClick.GetPersistentTarget(i);
                string method = tab.button.onClick.GetPersistentMethodName(i);
                if (target == null || string.IsNullOrEmpty(method)) continue;

                if (method == "SetActive")
                {
                    GameObject go = target as GameObject;
                    if (go != null && !tab.effects.Contains(go))
                        tab.effects.Add(go);
                    continue;
                }

                CameraSwitchToPoint camera = target as CameraSwitchToPoint;
                if (camera == null)
                {
                    GameObject go = target as GameObject;
                    if (go != null) camera = go.GetComponent<CameraSwitchToPoint>();
                }
                if (camera != null && !tab.cameras.Contains(camera))
                    tab.cameras.Add(camera);
            }
        }

        static void Apply(List<TabInfo> tabs, TabInfo selected)
        {
            // 点“设备信息”页签时，先把世界空间按钮弹出的所有面板收起来。
            if (selected.name == "shebeixinxi") PanelSwitcher.CloseAllPanels();

            // 1. 先关掉另外两个页签的效果。共用的物体如果在别的清单里，会在这里被关掉。
            foreach (TabInfo tab in tabs)
            {
                if (tab == selected) continue;
                foreach (GameObject go in tab.effects)
                    if (go != null) go.SetActive(false);
            }

            // 2. 再打开当前页签的效果。共用的物体会在这里被重新打开。
            foreach (GameObject go in selected.effects)
                if (go != null) go.SetActive(true);

            // 3. 只保留当前页签的相机飞行；其他相机组件还在飞的，取消掉。
            foreach (CameraSwitchToPoint camera in Object.FindObjectsOfType<CameraSwitchToPoint>(true))
            {
                if (camera == null) continue;
                if (selected.cameras.Contains(camera)) continue;
                camera.Cancel();
            }
        }
    }
}
