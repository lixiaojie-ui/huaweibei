using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>大屏图表构建的公共工具（仅编辑器使用）。</summary>
public static class DashUI
{
    public static readonly Color GREEN = new Color(0.259f, 0.871f, 0.620f, 1f);
    public static readonly Color CYAN = new Color(0.235f, 0.851f, 1.000f, 1f);
    public static readonly Color ORANGE = new Color(1.000f, 0.706f, 0.329f, 1f);
    public static readonly Color PURPLE = new Color(0.706f, 0.549f, 1.000f, 1f);
    public static readonly Color RED = new Color(1.000f, 0.561f, 0.478f, 1f);
    public static readonly Color TC = new Color(0.906f, 0.949f, 0.980f, 1f);
    public static readonly Color SUB = new Color(0.486f, 0.576f, 0.647f, 1f);
    public static readonly Color GRID = new Color(1f, 1f, 1f, 0.06f);
    public static readonly Color TITLEC = new Color(0.812f, 0.933f, 0.976f, 1f);
    public static readonly Color ENC = new Color(0.45f, 0.6f, 0.68f, 0.6f);
    public static readonly Color CARD = new Color(0.06f, 0.14f, 0.22f, 0.55f);

    static Font _font;
    static Sprite _ring, _barH, _barV, _barG, _barO;

    public static Font F
    {
        get
        {
            if (_font != null) return _font;
            try { _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            if (_font == null)
            {
                try { _font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { }
            }
            if (_font == null) Debug.LogWarning("[DashUI] 未找到内置字体，文字可能无法显示");
            return _font;
        }
    }

    static Sprite Load(string n)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/" + n + ".png");
    }

    public static Sprite Ring { get { if (_ring == null) _ring = Load("ring"); return _ring; } }
    public static Sprite BarH { get { if (_barH == null) _barH = Load("bar_h_round"); return _barH; } }
    public static Sprite BarV { get { if (_barV == null) _barV = Load("bar_v_round"); return _barV; } }
    public static Sprite BarG { get { if (_barG == null) _barG = Load("bar_v_green"); return _barG; } }
    public static Sprite BarO { get { if (_barO == null) _barO = Load("bar_v_orange"); return _barO; } }

    public static GameObject New(string name, RectTransform parent, float x, float y, float w, float h,
                                 float px = 0f, float py = 1f, float deg = 0f)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        go.layer = 5;
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(px, py);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
        rt.localRotation = Quaternion.Euler(0f, 0f, deg);
        rt.localScale = Vector3.one;
        Undo.RegisterCreatedObjectUndo(go, "大屏图表");
        return go;
    }

    public static RectTransform Img(RectTransform p, string n, float x, float y, float w, float h,
                                    Color c, Sprite s, float px = 0f, float py = 1f, float deg = 0f)
    {
        var go = New(n, p, x, y, w, h, px, py, deg);
        var im = go.AddComponent<Image>();
        im.color = c;
        im.sprite = s;
        im.raycastTarget = false;
        if (s != null && (s.border.sqrMagnitude > 0f)) im.type = Image.Type.Sliced;
        return go.GetComponent<RectTransform>();
    }

    /// <summary>环形进度（从 12 点方向顺时针填充）。</summary>
    public static RectTransform RingFill(RectTransform p, string n, float cx, float cy, float r,
                                         Color c, float amount)
    {
        var rt = Img(p, n, cx - r, cy + r, r * 2f, r * 2f, c, Ring, 0.5f, 0.5f, 0f);
        var im = rt.GetComponent<Image>();
        im.type = Image.Type.Filled;
        im.fillMethod = Image.FillMethod.Radial360;
        im.fillOrigin = 2;
        im.fillClockwise = true;
        im.fillAmount = amount;
        return rt;
    }

    public static RectTransform Txt(RectTransform p, string n, float x, float y, float w, float h,
                                    string s, int fs, Color c,
                                    TextAnchor al = TextAnchor.MiddleLeft, bool bold = false)
    {
        var go = New(n, p, x, y, w, h);
        var tx = go.AddComponent<Text>();
        tx.font = F;
        tx.fontSize = fs;
        tx.text = s;
        tx.color = c;
        tx.alignment = al;
        tx.raycastTarget = false;
        tx.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        tx.horizontalOverflow = HorizontalWrapMode.Overflow;
        tx.verticalOverflow = VerticalWrapMode.Overflow;
        return go.GetComponent<RectTransform>();
    }

    public static void Seg(RectTransform p, float x1, float y1, float x2, float y2, float w, Color c)
    {
        float dx = x2 - x1, dy = y2 - y1;
        float len = Mathf.Sqrt(dx * dx + dy * dy);
        if (len < 0.01f) return;
        float deg = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
        Img(p, "seg", (x1 + x2) / 2f, (y1 + y2) / 2f, len, w, c, null, 0.5f, 0.5f, deg);
    }

    public static void SegDash(RectTransform p, float x1, float y1, float x2, float y2,
                               float w, Color c, float dash, float gap, float fromTop = 0f)
    {
        float dx = x2 - x1, dy = y2 - y1;
        float len = Mathf.Sqrt(dx * dx + dy * dy);
        if (len < 0.01f) return;
        float nx = dx / len, ny = dy / len;
        float s = fromTop;
        while (s < len)
        {
            float e = Mathf.Min(s + dash, len);
            Seg(p, x1 + nx * s, y1 + ny * s, x1 + nx * e, y1 + ny * e, w, c);
            s = e + gap;
        }
    }

    public static void Dot(RectTransform p, float cx, float cy, float rad, Color c)
    {
        Img(p, "dot", cx - rad, cy + rad, rad * 2f, rad * 2f, c, null);
    }

    public static void Title(RectTransform p, string cn, string en)
    {
        Img(p, "tbar", 12, -10, 3, 14, CYAN, null);
        Txt(p, "title", 22, -7, 320, 20, cn, 15, TITLEC);
        Txt(p, "tEn", 22, -27, 320, 13, en, 10, ENC);
    }

    public static RectTransform FindRoot(string name)
    {
        var all = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var go in all)
        {
            if (go.name == name && go.scene.IsValid())
            {
                var rt = go.GetComponent<RectTransform>();
                if (rt != null) return rt;
            }
        }
        return null;
    }

    public static RectTransform Child(Transform parent, string name)
    {
        if (parent == null) return null;
        var t = parent.Find(name);
        if (t == null) return null;
        return t.GetComponent<RectTransform>();
    }

    public static void Clear(RectTransform rt)
    {
        if (rt == null) return;
        var list = new List<GameObject>();
        for (int i = 0; i < rt.childCount; i++) list.Add(rt.GetChild(i).gameObject);
        foreach (var go in list) Undo.DestroyObjectImmediate(go);
    }

    /// <summary>把容器自身 Image 的底色设为全透明（不要背景）。</summary>
    public static void ClearBg(RectTransform rt)
    {
        if (rt == null) return;
        var im = rt.GetComponent<Image>();
        if (im != null)
        {
            var c = im.color;
            c.a = 0f;
            im.color = c;
        }
    }

    public static float X(Transform t) { return ((RectTransform)t).sizeDelta.x; }
    public static float Y(Transform t) { return ((RectTransform)t).sizeDelta.y; }
}
