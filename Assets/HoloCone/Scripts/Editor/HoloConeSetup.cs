#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>在场景中一键生成全息锥台：外层锥、内层锥、地面光环。</summary>
public static class HoloConeSetup
{
    private const string MaterialDir = "Assets/HoloCone/Materials";

    [MenuItem("Tools/Holo/生成全息锥台")]
    public static void Create()
    {
        EnsureFolder();

        var root = new GameObject("HoloConeRoot");
        Undo.RegisterCreatedObjectUndo(root, "Create HoloCone");

        var coneMat = GetOrCreateMaterial("M_HoloCone", "Holo/HoloCone");
        var innerMat = GetOrCreateMaterial("M_HoloConeInner", "Holo/HoloCone");
        innerMat.SetFloat("_Alpha", 0.30f);
        innerMat.SetFloat("_FogStrength", 0.45f);
        innerMat.SetFloat("_Intensity", 1.1f);
        EditorUtility.SetDirty(innerMat);

        var glowMat = GetOrCreateMaterial("M_HoloGroundGlow", "Holo/GroundGlow");

        // 外层锥台
        CreateCone(root.transform, "HoloCone", coneMat, 1.0f, Vector3.zero);

        // 内层锥台，叠出雾的浓度层次
        CreateCone(root.transform, "HoloCone_Inner", innerMat, 0.92f, new Vector3(0f, 0.02f, 0f));

        // 地面光环，贴地 Quad
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "HoloGroundGlow";
        quad.transform.SetParent(root.transform, false);
        quad.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
        quad.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        quad.transform.localScale = new Vector3(8f, 8f, 1f);

        var collider = quad.GetComponent<Collider>();
        if (collider != null) Object.DestroyImmediate(collider);
        quad.GetComponent<MeshRenderer>().sharedMaterial = glowMat;

        Undo.RegisterCreatedObjectUndo(quad, "Create GroundGlow");

        AssetDatabase.SaveAssets();
        Selection.activeGameObject = root;

        Debug.Log("[Holo] 已生成全息锥台。相机建议 FOV 35，稍微仰视；记得打开 Bloom。");
    }

    private static void CreateCone(Transform parent, string name, Material mat, float scale, Vector3 offset)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create Cone");
        go.transform.SetParent(parent, false);
        go.transform.localScale = Vector3.one * scale;
        go.transform.localPosition = offset;

        var cone = go.AddComponent<HoloConeMesh>();
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;

        // 材质赋值后重建一次，把高度同步进 _Height
        cone.Build();
    }

    private static Material GetOrCreateMaterial(string name, string shaderName)
    {
        string path = $"{MaterialDir}/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        var shader = Shader.Find(shaderName);
        if (shader == null)
        {
            Debug.LogError($"[Holo] 找不到 Shader: {shaderName}，请确认已导入 HoloCone/Shaders。");
            return null;
        }

        var mat = new Material(shader);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/HoloCone"))
            AssetDatabase.CreateFolder("Assets", "HoloCone");
        if (!AssetDatabase.IsValidFolder(MaterialDir))
            AssetDatabase.CreateFolder("Assets/HoloCone", "Materials");
    }
}
#endif
