using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HoloConeMesh : MonoBehaviour
{
    [Header("形状")]
    [SerializeField] private float topRadius = 3.20f;
    [SerializeField] private float bottomRadius = 1.45f;
    [SerializeField] private float height = 5.0f;
    [Range(8, 128)]
    [SerializeField] private int segments = 64;

    [Header("自动同步")]
    [Tooltip("把高度同步到材质的 _Height，避免渐变错位")]
    [SerializeField] private bool syncHeightToMaterial = true;

    private void OnValidate()
    {
        if (Application.isPlaying) return;
        Build();
    }

    private void Awake()
    {
        if (GetComponent<MeshFilter>().sharedMesh == null) Build();
    }

    [ContextMenu("重新生成")]
    public void Build()
    {
        var filter = GetComponent<MeshFilter>();
        if (filter == null) return;

        var verts = new List<Vector3>();
        var normals = new List<Vector3>();
        var tris = new List<int>();

        // 两圈顶点：row 0 = 底部，row 1 = 顶部
        for (int row = 0; row <= 1; row++)
        {
            float radius = Mathf.Lerp(bottomRadius, topRadius, row);
            for (int s = 0; s <= segments; s++)
            {
                float angle = s / (float)segments * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                verts.Add(new Vector3(cos * radius, row * height, sin * radius));
                normals.Add(new Vector3(cos, 0f, sin));
            }
        }

        for (int s = 0; s < segments; s++)
        {
            int bottom = s;
            int top = s + segments + 1;

            tris.Add(bottom);     tris.Add(top);      tris.Add(bottom + 1);
            tris.Add(bottom + 1); tris.Add(top);      tris.Add(top + 1);
        }

        var mesh = new Mesh { name = "HoloCone" };
        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();

        if (Application.isPlaying) Destroy(filter.sharedMesh);
        else DestroyImmediate(filter.sharedMesh);

        filter.sharedMesh = mesh;

        if (syncHeightToMaterial) SyncMaterialHeight();
    }

    private void SyncMaterialHeight()
    {
        var renderer = GetComponent<MeshRenderer>();
        if (renderer == null || renderer.sharedMaterial == null) return;

        var mat = renderer.sharedMaterial;
        if (mat.HasProperty("_Height")) mat.SetFloat("_Height", height);
    }
}
