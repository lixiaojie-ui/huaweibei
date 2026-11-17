using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FloatingBillboard : MonoBehaviour
{
    public Camera viewCamera;   // 拖入 相机 / Camera

    [Tooltip("按钮矩形在屏幕上固定的高度（像素），每个按钮按自己的距离反向缩放。0 = 关闭这个功能。")]
    public float buttonScreenHeight = 100f;

    sealed class ScaledLabel
    {
        public RectTransform rect;
        public Vector3 baseLocalScale;
        public float baseWorldHeight;   // 场景里原始缩放下的世界高度
    }

    readonly List<ScaledLabel> labels = new List<ScaledLabel>();

    sealed class Hologram
    {
        public Transform transform;
        public Vector3 contentCenter;   // 场景里摆好的可见内容中心（世界坐标）
        public Vector3 scaledOffset;    // 轴心 -> 可见内容中心，已乘缩放
    }

    readonly List<Hologram> holograms = new List<Hologram>();

    // 这些 PNG 的可见全息图不一定正好在图片中心，
    // 图片轴心（图片中心）和可见内容中心对不上时，旋转会绕着错误的点甩动。
    // 值是可见内容中心在图片里的归一化坐标（从左下角算起，alpha 包围盒中心）。
    static readonly Dictionary<string, Vector2> ContentUV = new Dictionary<string, Vector2>
    {
        { "1",  new Vector2(0.5120f, 0.5117f) },
        { "2",  new Vector2(0.4993f, 0.5488f) },
        { "4",  new Vector2(0.5016f, 0.5085f) },
        { "5",  new Vector2(0.5029f, 0.5068f) },
        { "6",  new Vector2(0.4997f, 0.5068f) },
        { "7",  new Vector2(0.5000f, 0.5068f) },
        { "8",  new Vector2(0.4833f, 0.5183f) },
        { "10", new Vector2(0.8659f, 0.2037f) },
        { "11", new Vector2(0.4862f, 0.3000f) },
        { "12", new Vector2(0.3010f, 0.3176f) },
        { "13", new Vector2(0.8688f, 0.7935f) },
        { "14", new Vector2(0.4747f, 0.7514f) },
        { "15", new Vector2(0.1039f, 0.8023f) },
    };

    void Awake()
    {
        // 图片如果挂在按钮下面，按钮的非等比缩放会把转向后的图片拉歪，
        // 所以先挂回画布（保留世界位置，场景里摆好的样子不变）。
        // 画布自身是等比缩放，图片转向相机时才是刚体旋转。
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Transform t = renderers[i].transform;
            if (t.parent != transform)
                t.SetParent(transform, true);
        }

        // 场景里摆好的位置记下来。运行时父按钮会被转向相机，
        // 图片如果只是跟着父物体转，就会绕着按钮轴心甩出去。
        foreach (SpriteRenderer renderer in renderers)
        {
            Sprite sprite = renderer.sprite;
            if (sprite == null) continue;

            Vector2 uv = ContentUVOf(sprite);
            Bounds bounds = sprite.bounds;
            Vector3 local = new Vector3(
                Mathf.Lerp(bounds.min.x, bounds.max.x, uv.x),
                Mathf.Lerp(bounds.min.y, bounds.max.y, uv.y),
                0f);

            Transform t = renderer.transform;
            Vector3 scaledOffset = Vector3.Scale(t.lossyScale, local);
            holograms.Add(new Hologram
            {
                transform = t,
                contentCenter = t.position + t.rotation * scaledOffset,
                scaledOffset = scaledOffset,
            });
        }

        // 设备按钮：记下场景里原始的缩放和世界高度，运行时按距离反向缩放，
        // 让每个按钮在屏幕上始终是 buttonScreenHeight 像素高。
        // 只对 Canvas shebei 生效：这个脚本在别的标签画布上也挂着，
        // 那些画布不是设备按钮，不能被这次改动影响。
        if (gameObject.name.ToLowerInvariant().Contains("shebei"))
        {
            foreach (Transform child in transform)
            {
                Button button = child.GetComponent<Button>();
                RectTransform rect = child as RectTransform;
                if (button == null || rect == null) continue;

                labels.Add(new ScaledLabel
                {
                    rect = rect,
                    baseLocalScale = rect.localScale,
                    baseWorldHeight = rect.rect.height * rect.lossyScale.y,
                });
            }

            if (labels.Count > 0)
            {
                Debug.Log("[FloatingBillboard] 按钮等屏幕大小已启用：按钮 " + labels.Count +
                    " 个，目标屏幕高度 " + buttonScreenHeight + " 像素。", this);
            }
        }
    }

    void LateUpdate()
    {
        if (viewCamera == null) return;
        Quaternion viewRotation = viewCamera.transform.rotation;

        // 画布本身不转：一转，所有标注都会绕画布中心甩出去。
        // 每个直接子物体（按钮）绕自己的轴心原地转向相机。
        foreach (Transform child in transform)
            child.rotation = viewRotation;

        // 按钮做“等屏幕尺寸”：透视下，物体在屏幕上看起来的高度 =
        // 世界高度 / (2 * 相机距离 * tan(FOV/2)) * 屏幕高度。
        // 所以按这个公式反推缩放系数，距离越远把按钮放大越多，
        // 所有按钮在屏幕上就一样大，不会因为前后位置不同而变小。
        if (buttonScreenHeight > 0f && labels.Count > 0)
        {
            bool orthographic = viewCamera.orthographic;
            Vector3 camPosition = viewCamera.transform.position;
            Vector3 camForward = viewCamera.transform.forward;
            float tanScale = 2f * Mathf.Tan(viewCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float orthoScale = 2f * Mathf.Max(0.0001f, viewCamera.orthographicSize);

            for (int i = 0; i < labels.Count; i++)
            {
                ScaledLabel label = labels[i];
                if (label.rect == null || label.baseWorldHeight <= 0.0001f) continue;

                float depth = Vector3.Dot(label.rect.position - camPosition, camForward);
                if (!orthographic && depth <= 0.01f) continue;

                float pixelsPerUnit = orthographic
                    ? Screen.height / orthoScale
                    : Screen.height / (depth * tanScale);

                float k = buttonScreenHeight / (label.baseWorldHeight * pixelsPerUnit);
                label.rect.localScale = label.baseLocalScale * k;
            }
        }

        // 后插入的全息图片：绕“可见内容中心”转向相机，而不是绕图片轴心，
        // 这样运行时看到的全息图位置和场景里摆的完全一致，不会被父按钮带偏。
        for (int i = 0; i < holograms.Count; i++)
        {
            Hologram hologram = holograms[i];
            if (hologram.transform == null) continue;
            hologram.transform.rotation = viewRotation;
            hologram.transform.position = hologram.contentCenter - viewRotation * hologram.scaledOffset;
        }
    }

    static Vector2 ContentUVOf(Sprite sprite)
    {
        string key = sprite.texture != null ? sprite.texture.name : sprite.name;
        if (string.IsNullOrEmpty(key)) key = sprite.name;
        if (!string.IsNullOrEmpty(key))
        {
            int dot = key.LastIndexOf('.');
            if (dot > 0) key = key.Substring(0, dot);

            Vector2 uv;
            if (ContentUV.TryGetValue(key, out uv)) return uv;
        }
        return new Vector2(0.5f, 0.5f);
    }
}
