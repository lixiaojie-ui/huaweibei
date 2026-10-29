using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 表格自动滚动：运行后在顶部/底部之间往返匀速滚动，两端停留片刻。
/// 挂在带 ScrollRect 的物体上，scrollRect 不填则自动取自身组件。
/// </summary>
public class AutoScrollTable : MonoBehaviour
{
    [Header("不填则自动取自身 ScrollRect")]
    public ScrollRect scrollRect;

    [Header("滚动速度（每秒滚过的比例，0.05 ~ 0.15 比较合适）")]
    public float speed = 0.08f;

    [Header("滚动到顶部/底部后的停留秒数")]
    public float pauseTime = 1.5f;

    float timer;
    int dir = -1; // verticalNormalizedPosition: 1=顶部, 0=底部，-1 表示向下滚

    void Awake()
    {
        if (scrollRect == null)
            scrollRect = GetComponent<ScrollRect>();
    }

    void Update()
    {
        if (scrollRect == null || scrollRect.content == null || scrollRect.viewport == null)
            return;

        // 内容没超出视口时不滚动
        if (scrollRect.content.rect.height <= scrollRect.viewport.rect.height)
            return;

        if (timer > 0f)
        {
            timer -= Time.deltaTime;
            return;
        }

        float v = scrollRect.verticalNormalizedPosition + dir * speed * Time.deltaTime;

        if (v <= 0f) { v = 0f; dir = 1; timer = pauseTime; }        // 到底，回头向上
        else if (v >= 1f) { v = 1f; dir = -1; timer = pauseTime; }  // 到顶，回头向下

        scrollRect.verticalNormalizedPosition = v;
    }
}
