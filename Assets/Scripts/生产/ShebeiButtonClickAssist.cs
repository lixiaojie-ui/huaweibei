using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Daima
{
    /// <summary>
    /// Canvas shebei 是 World Space 画布：设备按钮摆在世界里，离相机远的时候
    /// 在屏幕上只有几个像素，靠按钮自己的小矩形根本点不中，调 Raycast Padding
    /// 也受画布/按钮缩放影响，大小不一。
    ///
    /// 这里加一个零配置兜底：左键点击时，如果没点在 UI 上，就找屏幕上离鼠标
    /// 最近的设备按钮（64 像素以内），直接触发它的 onClick。这样不论远近、
    /// 不论按钮在屏幕上多小，点它附近都能选中。不需要挂组件、不需要改场景。
    /// </summary>
    public static class ShebeiButtonClickAssist
    {
        const float HitRadiusPixels = 90f;
        const string CanvasKeyword = "shebei";

        static Canvas canvas;
        static Button[] buttons;
        static readonly List<RaycastResult> RaycastResults = new List<RaycastResult>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            canvas = FindCanvas();
            if (canvas == null)
            {
                Debug.LogWarning("[ShebeiButtonClickAssist] 没找到 Canvas shebei，点击兜底不启用。");
                return;
            }

            buttons = canvas.GetComponentsInChildren<Button>(true);
            if (buttons.Length == 0)
            {
                Debug.LogWarning("[ShebeiButtonClickAssist] Canvas shebei 下没有 Button，点击兜底不启用。");
                return;
            }

            // 按钮图片是整张 PNG，四周透明边很大；等屏幕大小放大后，
            // 透明区域会互相盖住，点 A 却命中 B 的透明区，打开错误的面板。
            // 这里把每个按钮的 Raycast 区域按图片里可见内容的包围盒收缩，
            // 透明边不再抢点击；点在按钮附近但没点中时，下面的兜底仍会选最近的按钮。
            for (int i = 0; i < buttons.Length; i++)
            {
                Image image = buttons[i].targetGraphic as Image;
                if (image == null) image = buttons[i].GetComponent<Image>();
                if (image == null || image.rectTransform == null) continue;

                Vector4 pad = PaddingOf(image.sprite);   // (左, 下, 右, 上)，都是 0~1
                Rect rect = image.rectTransform.rect;
                image.raycastPadding = new Vector4(
                    pad.x * rect.width,
                    pad.y * rect.height,
                    pad.z * rect.width,
                    pad.w * rect.height);
            }

            // Canvas main3 顶层的 Image / Image (1)… 和背景画布的全屏图挡在设备按钮前面，
            // 它们没有点击处理，却会吃掉点击。关掉它们的 Raycast，让点击落到下面的按钮上。
            DisableDecorationRaycast();

            GameObject runner = new GameObject("[ShebeiButtonClickAssist]");
            Object.DontDestroyOnLoad(runner);
            runner.AddComponent<Runner>();
            Debug.Log("[ShebeiButtonClickAssist] 已启用：按钮 " + buttons.Length +
                " 个，已关闭上层装饰图射线，点击区域收缩到可见内容，附近 90 像素内兜底选择最近按钮。");
        }

        static void DisableDecorationRaycast()
        {
            foreach (Canvas c in Object.FindObjectsOfType<Canvas>(true))
            {
                if (c == null) continue;

                if (c.name == "Canvas main3")
                {
                    // 只处理直接子物体里的 Image / Image (1) / Image (2) / Image (3) / Image (5)，
                    // Panel / Panel1… 是信息面板，保持可挡点击。
                    foreach (Transform child in c.transform)
                    {
                        if (!child.name.StartsWith("Image")) continue;
                        Graphic graphic = child.GetComponent<Graphic>();
                        if (graphic != null && !IsInteractive(graphic.gameObject))
                            graphic.raycastTarget = false;
                    }
                }
                else if (c.name == "背景画布")
                {
                    foreach (Graphic graphic in c.GetComponentsInChildren<Graphic>(true))
                    {
                        if (graphic != null && !IsInteractive(graphic.gameObject))
                            graphic.raycastTarget = false;
                    }
                }
            }
        }

        /// <summary>
        /// 可见内容四周的透明边占比（左, 下, 右, 上），用来把 Raycast 区域收进可见全息图。
        /// 数值来自图片 alpha 包围盒；未知图按四边 0.25 收缩，避免透明边太大。
        /// </summary>
        static Vector4 PaddingOf(Sprite sprite)
        {
            string key = sprite != null
                ? (sprite.texture != null ? sprite.texture.name : sprite.name)
                : null;
            if (!string.IsNullOrEmpty(key))
            {
                int dot = key.LastIndexOf('.');
                if (dot > 0) key = key.Substring(0, dot);

                switch (key)
                {
                    case "1": return new Vector4(0.3952f, 0.3919f, 0.3711f, 0.3685f);
                    case "2": return new Vector4(0.3789f, 0.4290f, 0.3802f, 0.3314f);
                    case "4": return new Vector4(0.3848f, 0.3887f, 0.3815f, 0.3717f);
                    case "5": return new Vector4(0.3848f, 0.3919f, 0.3789f, 0.3783f);
                    case "6": return new Vector4(0.3809f, 0.3919f, 0.3815f, 0.3783f);
                    case "7": return new Vector4(0.3835f, 0.3919f, 0.3835f, 0.3783f);
                }
            }
            return new Vector4(0.25f, 0.25f, 0.25f, 0.25f);
        }

        /// <summary>
        /// 最上层只是装饰图（没有点击处理，也不在信息面板里）时返回 true。
        /// Canvas main3 顶层的 Image / Image (1)… 和背景画布的 Image 属于这种，
        /// 它们会把下面 World Space 按钮的点击吃掉，需要跳过。
        /// </summary>
        static bool IsDecoration(GameObject go)
        {
            if (go == null) return false;
            if (IsInteractive(go)) return false;
            if (IsInsidePanel(go)) return false;

            Canvas canvas = go.GetComponentInParent<Canvas>();
            if (canvas == null) return false;
            return canvas.name == "Canvas main3" || canvas.name == "背景画布";
        }

        static bool IsInteractive(GameObject go)
        {
            if (go == null) return false;
            if (ExecuteEvents.GetEventHandler<IPointerClickHandler>(go) != null) return true;
            if (go.GetComponentInParent<Selectable>() != null) return true;
            if (go.GetComponentInParent<ScrollRect>() != null) return true;
            if (go.GetComponentInParent<EventTrigger>() != null) return true;
            return false;
        }

        /// <summary>点到已打开的信息面板（Panel / Panel1…）上，不应该再穿透到设备按钮。</summary>
        static bool IsInsidePanel(GameObject go)
        {
            Transform t = go.transform;
            while (t != null)
            {
                if (t.name.StartsWith("Panel")) return true;
                if (t.GetComponent<Canvas>() != null) return false;
                t = t.parent;
            }
            return false;
        }

        static Canvas FindCanvas()
        {
            foreach (Canvas c in Object.FindObjectsOfType<Canvas>(true))
            {
                if (c != null && c.name.ToLowerInvariant().Contains(CanvasKeyword))
                    return c;
            }
            return null;
        }

        sealed class Runner : MonoBehaviour
        {
            void Update()
            {
                if (!Input.GetMouseButtonDown(0)) return;
                if (EventSystem.current == null) return;
                if (canvas == null || !canvas.isActiveAndEnabled) return;

                // 鼠标下面是别的 UI 时不要穿透。但 Canvas main3 / 背景画布 上的
                // 装饰图（Image、Image (1)…）没有点击处理，却挡在最前面，
                // 会把设备按钮的点击吃掉——这种装饰图要忽略，继续走下面的最近按钮兜底。
                PointerEventData pointer = new PointerEventData(EventSystem.current)
                {
                    position = Input.mousePosition
                };
                RaycastResults.Clear();
                EventSystem.current.RaycastAll(pointer, RaycastResults);
                if (RaycastResults.Count > 0)
                {
                    // 有已打开的信息面板挡着，就不要穿透到设备按钮。
                    for (int i = 0; i < RaycastResults.Count; i++)
                    {
                        if (IsInsidePanel(RaycastResults[i].gameObject))
                            return;
                    }

                    // 最上层是装饰图（Canvas main3 的 Image、背景画布）时忽略它，
                    // 继续按最近的设备按钮兜底；真正的交互 UI 在最上层则交给它。
                    GameObject top = RaycastResults[0].gameObject;
                    if (!IsDecoration(top)) return;
                }

                Camera cam = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
                if (cam == null) return;

                Vector2 mouse = Input.mousePosition;
                Button nearest = null;
                float nearestDistance = HitRadiusPixels;

                for (int i = 0; i < buttons.Length; i++)
                {
                    Button button = buttons[i];
                    if (button == null || !button.isActiveAndEnabled || !button.IsInteractable()) continue;

                    Vector3 screen = RectTransformUtility.WorldToScreenPoint(cam, button.transform.position);
                    if (screen.z <= 0f) continue;   // 在相机背后

                    float distance = Vector2.Distance(mouse, screen);
                    if (distance > nearestDistance) continue;

                    nearestDistance = distance;
                    nearest = button;
                }

                if (nearest != null)
                {
                    Debug.Log("[ShebeiButtonClickAssist] 兜底点击：" + nearest.name);
                    nearest.onClick.Invoke();
                }
            }
        }
    }
}
