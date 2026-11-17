using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Daima
{
    /// <summary>
    /// 实时滚动趋势图：批次柱 + 合格率折线。
    /// 每隔 interval 秒整体左移一格，右侧追加一个新样本（仅运行时生效）。
    /// </summary>
    public class QDRollingTrend : MonoBehaviour
    {
        public float interval = 2f;
        public float maxBatch = 80f;
        public float plotHeight = 178f;
        public float barWidth = 14f;

        public RectTransform[] bars;
        public Text[] barLabels;
        public UILineGraphic rateLine;
        public Text currentBatch;
        public Text currentRate;

        public float[] initBatches;
        public float[] initRates;

        float[] batches;
        float[] rates;
        float timer;

        void Start()
        {
            int n = bars != null ? bars.Length : 0;
            if (n == 0) return;

            batches = new float[n];
            rates = new float[n];
            for (int i = 0; i < n; i++)
            {
                if (initBatches != null && i < initBatches.Length) batches[i] = initBatches[i];
                if (initRates != null && i < initRates.Length) rates[i] = initRates[i];
            }
            Apply();
        }

        void Update()
        {
            if (batches == null) return;
            timer += Time.deltaTime;
            if (timer < interval) return;
            timer = 0f;
            Step();
        }

        void Step()
        {
            int n = batches.Length;
            for (int i = 0; i < n - 1; i++)
            {
                batches[i] = batches[i + 1];
                rates[i] = rates[i + 1];
            }

            batches[n - 1] = Mathf.Clamp(batches[n - 2] + Random.Range(-7f, 8f), 36f, 79f);
            rates[n - 1] = Mathf.Clamp(rates[n - 2] + Random.Range(-0.28f, 0.30f), 97.1f, 99.5f);
            Apply();
        }

        void Apply()
        {
            int n = batches.Length;

            for (int i = 0; i < n; i++)
            {
                float h = plotHeight * Mathf.Clamp01(batches[i] / maxBatch);
                if (bars[i] != null) bars[i].sizeDelta = new Vector2(barWidth, Mathf.Max(2f, h));
                if (barLabels != null && i < barLabels.Length && barLabels[i] != null)
                    barLabels[i].text = Mathf.RoundToInt(batches[i]).ToString();
            }

            if (rateLine != null)
            {
                var pts = new List<Vector2>(n);
                for (int i = 0; i < n; i++)
                    pts.Add(new Vector2((i + 0.5f) / n, Mathf.Clamp01((rates[i] - 96f) / 4f)));
                rateLine.SetData(pts, 2f, 4f, new Color(0.208f, 0.878f, 0.753f, 1f));
            }

            if (currentBatch != null) currentBatch.text = Mathf.RoundToInt(batches[n - 1]).ToString();
            if (currentRate != null) currentRate.text = rates[n - 1].ToString("0.00") + "%";
        }
    }
}
