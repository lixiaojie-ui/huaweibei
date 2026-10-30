using UnityEngine;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class OneRow
{
    public string product;
    public string deliver;
    public string stock;
    public string flag;
}

public class TableLoad : MonoBehaviour
{
    [Header("滚动容器")]
    public Transform content;

    [Header("行预制体")]
    public GameObject rowPrefab;

    [Header("测试数据")]
    public List<OneRow> dataList;

    [Header("文字颜色")]
    public Color normalBlue = new Color(0.67f, 0.85f, 1f);
    public Color warnOrange = new Color(1f, 0.76f, 0.28f);

    void Start()
    {
        FillDemoData();
        RefreshTable();
    }

    void FillDemoData()
    {
        dataList = new List<OneRow>
        {
            new OneRow{ product = "2150 料", deliver = "11:50:00", stock = "12010080", flag = "待检" },
            new OneRow{ product = "2220 料", deliver = "11:55:00", stock = "12011400", flag = "待检" },
            new OneRow{ product = "2190 料", deliver = "20:30:00", stock = "4201980",  flag = "待检" },
            new OneRow{ product = "2260 料", deliver = "11:30:00", stock = "29012600", flag = "待检" },
            new OneRow{ product = "2130 料", deliver = "17:30:00", stock = "7000",      flag = "待检" },
            new OneRow{ product = "2520 料", deliver = "18:30:00", stock = "1080",    flag = "待检" },
            new OneRow{ product = "2330 料", deliver = "12:30:00", stock = "740",     flag = "待检" },
            new OneRow{ product = "2140 料", deliver = "19:30:00", stock = "1040",    flag = "待检" },
            new OneRow{ product = "2240 料", deliver = "18:30:00", stock = "1690",    flag = "待检" },
            new OneRow{ product = "2390 料", deliver = "19:30:00", stock = "1040",    flag = "待检" },
            new OneRow{ product = "2380 料", deliver = "15:30:00", stock = "1600",    flag = "待检" },
        };
    }

    public void RefreshTable()
    {
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }

        foreach (var row in dataList)
        {
            GameObject newRow = Instantiate(rowPrefab, content);
            newRow.name = "Row_" + row.product;

            TextMeshProUGUI[] texts = newRow.GetComponentsInChildren<TextMeshProUGUI>();

            if (texts.Length >= 4)
            {
                texts[0].text = row.product;
                texts[1].text = row.deliver;
                texts[2].text = row.stock;
                texts[3].text = row.flag;

                if (row.flag == "待检")
                {
                    texts[3].color = warnOrange;
                }
                else
                {
                    texts[3].color = normalBlue;
                }
            }
        }
    }
}
