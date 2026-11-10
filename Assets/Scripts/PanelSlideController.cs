using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Daima
{
    /// <summary>
    /// 点击屏幕中间空白处：当前页面左右两侧面板向屏幕外自然散开；
    /// 点击圆环上的 TileButtonN（1/2/4/5）：面板向中间靠拢回到原位。
    /// </summary>
    public class PanelSlideController : MonoBehaviour
    {
        sealed class Item
        {
            public RectTransform rect;
            public float homeX;
            public float offsetX;   // 散开时的位移量（左为负、右为正）
        }

        static readonly int[] Pages = { 1, 2, 4, 5 };

        const float Duration = 1.2f;   // 散开/收拢动画时长（秒）
        const float CenterHalfWidth = 0.15f;    // 屏幕宽度的比例
        const float CenterHalfHeight = 0.18f;   // 屏幕高度的比例

        readonly List<Item> items = new List<Item>();
        bool hidden;
        Coroutine running;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            GameObject go = new GameObject("PanelSlideController");
            DontDestroyOnLoad(go);
            go.AddComponent<PanelSlideController>();
        }

        void Start()
        {
            // 其它页在启动时被 TilePanelSwitcher 隐藏，GameObject.Find 找不到，必须包含未激活对象
            Canvas[] canvases = FindObjectsOfType<Canvas>(true);

            for (int i = 0; i < Pages.Length; i++)
            {
                int n = Pages[i];
                string mainName = "Canvas main" + n;
                Transform main = null;

                for (int j = 0; j < canvases.Length; j++)
                {
                    if (canvases[j] == null) continue;
                    if (canvases[j].gameObject.name != mainName) continue;

                    main = canvases[j].transform;
                    break;
                }

                if (main == null) continue;

                Add(main.Find("Canvas left" + n), -1f);
                Add(main.Find("Canvas right" + n), 1f);
            }

            Button[] buttons = FindObjectsOfType<Button>(true);
            foreach (Button button in buttons)
            {
                if (button == null) continue;

                int n = ParseTileIndex(button.gameObject.name);
                if (n <= 0 || !HasPage(n)) continue;

                button.onClick.AddListener(Restore);
            }
        }

        void Add(Transform t, float dir)
        {
            if (t == null) return;

            RectTransform rect = t as RectTransform;
            if (rect == null) return;

            float width = rect.rect.width > 1f ? rect.rect.width : 1920f;
            items.Add(new Item
            {
                rect = rect,
                homeX = rect.anchoredPosition.x,
                offsetX = dir * width
            });
        }

        bool HasPage(int index)
        {
            for (int i = 0; i < Pages.Length; i++)
                if (Pages[i] == index) return true;
            return false;
        }

        void Update()
        {
            if (!Input.GetMouseButtonDown(0)) return;
            if (hidden) return;

            Vector2 p = Input.mousePosition;
            if (Mathf.Abs(p.x - Screen.width * 0.5f) > Screen.width * CenterHalfWidth) return;
            if (Mathf.Abs(p.y - Screen.height * 0.5f) > Screen.height * CenterHalfHeight) return;
            if (OverButton(p)) return;

            Play(true);
        }

        void Restore()
        {
            if (!hidden) return;
            Play(false);
        }

        void Play(bool toHidden)
        {
            hidden = toHidden;
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(Animate(toHidden));
        }

        IEnumerator Animate(bool toHidden)
        {
            int n = items.Count;
            float[] from = new float[n];

            for (int i = 0; i < n; i++)
                from[i] = items[i].rect.anchoredPosition.x;

            float elapsed = 0f;
            while (elapsed < Duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / Duration));

                for (int i = 0; i < n; i++)
                {
                    Item item = items[i];
                    float target = toHidden ? item.homeX + item.offsetX : item.homeX;
                    Vector2 pos = item.rect.anchoredPosition;
                    item.rect.anchoredPosition = new Vector2(Mathf.Lerp(from[i], target, k), pos.y);
                }

                yield return null;
            }

            for (int i = 0; i < n; i++)
            {
                Item item = items[i];
                float target = toHidden ? item.homeX + item.offsetX : item.homeX;
                Vector2 pos = item.rect.anchoredPosition;
                item.rect.anchoredPosition = new Vector2(target, pos.y);
            }

            running = null;
        }

        /// <summary>指针是否落在按钮上（此时不触发散开）。</summary>
        static bool OverButton(Vector2 screenPosition)
        {
            EventSystem es = EventSystem.current;
            if (es == null) return false;

            var data = new PointerEventData(es) { position = screenPosition };
            var hits = new List<RaycastResult>();
            es.RaycastAll(data, hits);

            for (int i = 0; i < hits.Count; i++)
            {
                GameObject go = hits[i].gameObject;
                if (go == null) continue;
                if (go.GetComponentInParent<Button>() != null) return true;
            }

            return false;
        }

        static int ParseTileIndex(string name)
        {
            const string prefix = "TileButton";
            if (string.IsNullOrEmpty(name) ||
                !name.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase)) return 0;

            int value = 0;
            int digits = 0;
            for (int i = prefix.Length; i < name.Length && char.IsDigit(name[i]); i++)
            {
                value = value * 10 + (name[i] - '0');
                digits++;
            }

            return digits > 0 ? value : 0;
        }
    }
}
