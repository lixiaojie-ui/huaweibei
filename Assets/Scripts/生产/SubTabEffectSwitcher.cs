using System;
using UnityEngine;
using UnityEngine.Events;

namespace Daima
{
    /// <summary>
    /// Canvas main3 三个子页签（产线总览 / 生产动画 / 设备信息）的效果总控。
    ///
    /// 关键点：三个页签不是三个独立的开关，而是一个“三选一”开关。
    /// 每次点击都按固定顺序执行：
    ///   1. 复位：关掉所有页签的效果对象、通知上一个页签离开、取消没飞完的相机；
    ///   2. 应用：只打开当前页签的对象、触发当前页签的额外效果；
    ///   3. 相机：飞到当前页签自己的视角点。
    /// 这样无论点多快，画面上都只会留下最后点击的那个页签的效果。
    /// </summary>
    public sealed class SubTabEffectSwitcher : MonoBehaviour
    {
        [Serializable]
        public sealed class TabEffect
        {
            [Tooltip("页签名称，只用于在 Inspector 里辨认")]
            public string name;

            [Tooltip("点这个页签时要显示的对象。切到其他页签时会被自动 SetActive(false)。")]
            public GameObject[] showObjects = new GameObject[0];

            [Tooltip("点这个页签时相机飞到的视角点。留空表示这个页签不移动相机。")]
            public Transform viewPoint;

            [Tooltip("切到这个页签时额外执行的事件，例如 Animator.Play、播放粒子。")]
            public UnityEvent onSelected = new UnityEvent();

            [Tooltip("离开这个页签时额外执行的事件，例如停止动画、恢复默认状态。")]
            public UnityEvent onDeselected = new UnityEvent();
        }

        [Header("三个页签各自的效果")]
        public TabEffect chanxianzonglan = new TabEffect { name = "产线总览" };
        public TabEffect shenchandonghua = new TabEffect { name = "生产动画" };
        public TabEffect shebeixinxi = new TabEffect { name = "设备信息" };

        [Header("相机")]
        [Tooltip("负责飞相机的组件。三个页签必须共用同一个，新的点击才能取消上一次还没飞完的飞行。")]
        public CameraSwitchToPoint cameraFly;

        TabEffect current;

        /// <summary>是否至少配置了一个页签的效果；自动版用它判断要不要让位给手动版。</summary>
        public bool HasAnyEffect()
        {
            return HasEffect(chanxianzonglan) || HasEffect(shenchandonghua) || HasEffect(shebeixinxi);
        }

        static bool HasEffect(TabEffect tab)
        {
            if (tab == null) return false;
            if (tab.viewPoint != null) return true;
            return tab.showObjects != null && tab.showObjects.Length > 0;
        }

        // 下面三个无参方法给按钮的 OnClick 用，方法名会出现在 Inspector 的下拉列表里。
        public void SelectChanxianzonglan() { Select(chanxianzonglan); }
        public void SelectShenchandonghua() { Select(shenchandonghua); }
        public void SelectShebeixinxi()
        {
            PanelSwitcher.CloseAllPanels();
            Select(shebeixinxi);
        }

        public void Select(TabEffect tab)
        {
            if (tab == null) return;

            if (cameraFly == null)
                cameraFly = FindObjectOfType<CameraSwitchToPoint>();

            // 1. 复位：先让上一个页签收尾，再把三个页签的对象全部关掉。
            //    不要只关“上一个”，因为初始状态或手动摆弄后可能有多个页签的对象同时开着。
            if (current != null && current != tab)
                current.onDeselected?.Invoke();

            Hide(chanxianzonglan);
            Hide(shenchandonghua);
            Hide(shebeixinxi);

            // 2. 应用：只打开当前页签的对象。
            current = tab;
            if (tab.showObjects != null)
            {
                for (int i = 0; i < tab.showObjects.Length; i++)
                {
                    GameObject go = tab.showObjects[i];
                    if (go != null) go.SetActive(true);
                }
            }
            tab.onSelected?.Invoke();

            // 3. 相机：有视角点就飞过去；没有视角点就取消可能还在进行的上一次飞行，
            //    否则人已经切到“设备信息”，相机却还在飞往“产线总览”的视角。
            if (cameraFly != null)
            {
                if (tab.viewPoint != null)
                    cameraFly.GoTo(tab.viewPoint);
                else
                    cameraFly.Cancel();
            }
        }

        void Hide(TabEffect tab)
        {
            if (tab == null || tab.showObjects == null) return;

            for (int i = 0; i < tab.showObjects.Length; i++)
            {
                GameObject go = tab.showObjects[i];
                if (go != null) go.SetActive(false);
            }
        }
    }
}
