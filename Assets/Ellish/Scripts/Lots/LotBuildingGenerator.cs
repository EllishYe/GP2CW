using System.Collections.Generic;
using Clipper2Lib;
using UnityEngine;

public class LotBuildingGenerator : MonoBehaviour
{
    [Header("Building Output")]
    public bool generateBuildings = true;
    public float buildingBaseHeight = 0.02f;
    public string buildingObjectPrefix = "Building";

    [Header("Fallback Materials")]
    public Color defaultBuildingColor = new Color(0.72f, 0.72f, 0.76f, 1f);
    public Color residentialColor = new Color(0.78f, 0.70f, 0.62f, 1f);
    public Color commercialColor = new Color(0.42f, 0.54f, 0.68f, 1f);
    public Color industrialColor = new Color(0.56f, 0.56f, 0.52f, 1f);

    [Header("Physics and Layers")]
    public bool addBuildingMeshCollider = true;
    public string buildingLayerName = "Building";

    public List<GameObject> generatedBuildings = new List<GameObject>();

    private readonly Dictionary<UrbanBlockType, Material> fallbackMaterials = new Dictionary<UrbanBlockType, Material>();

    void OnValidate()
    {
        buildingBaseHeight = Mathf.Max(0f, buildingBaseHeight);
    }

    public void Generate(LotAreaGenerator lotAreaGenerator, Transform parent)
    {
        Clear(parent);

        if (!generateBuildings)
            return;

        if (lotAreaGenerator == null)
        {
            Debug.LogWarning("LotBuildingGenerator: LotAreaGenerator is not assigned.");
            return;
        }

        Transform root = parent != null ? parent : transform;
        for (int i = 0; i < lotAreaGenerator.lots.Count; i++)
            CreateBuilding(lotAreaGenerator, lotAreaGenerator.lots[i], root);

        Debug.Log($"LotBuildingGenerator: Generated {generatedBuildings.Count} building object(s).");
    }

    public void Clear(Transform parent)
    {
        for (int i = generatedBuildings.Count - 1; i >= 0; i--)
            DestroyGeneratedObject(generatedBuildings[i]);
        generatedBuildings.Clear();

        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (!child.name.StartsWith(buildingObjectPrefix))
                continue;

            DestroyGeneratedObject(child.gameObject);
        }
    }

    private void CreateBuilding(LotAreaGenerator lotAreaGenerator, LotData lot, Transform parent)
    {
        if (lot == null)
            return;

        Path64 footprint = lot.buildingFootprint != null && lot.buildingFootprint.Count >= 3
            ? lot.buildingFootprint
            : lot.polygon;

        if (footprint == null || footprint.Count < 3)
            return;

        float topHeight = Mathf.Max(buildingBaseHeight + 0.01f, lot.buildingHeight);
        if (!BuildingMeshBuilder.TryBuildExtrudedMesh(footprint, buildingBaseHeight, topHeight, out Mesh mesh, out TriangulateResult result))
        {
            Debug.LogWarning($"LotBuildingGenerator: Building mesh failed for lot {lot.id}: {result}");
            return;
        }

        mesh.name = $"{buildingObjectPrefix}_{lot.id:000}_{lot.urbanBlockType}";

        GameObject buildingObject = new GameObject(mesh.name);
        buildingObject.transform.SetParent(parent, false);

        MeshFilter meshFilter = buildingObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = buildingObject.AddComponent<MeshRenderer>();
        meshFilter.sharedMesh = mesh;
        meshRenderer.sharedMaterial = ResolveMaterial(lotAreaGenerator, lot.urbanBlockType);

        if (addBuildingMeshCollider)
        {
            MeshCollider meshCollider = buildingObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
        }

        AssignLayerIfExists(buildingObject, buildingLayerName);
        lot.meshObject = buildingObject;
        generatedBuildings.Add(buildingObject);
    }

    private Material ResolveMaterial(LotAreaGenerator lotAreaGenerator, UrbanBlockType type)
    {
        UrbanBlockTypeProfile profile = lotAreaGenerator.GetProfile(type);
        if (profile != null && profile.buildingMaterial != null)
            return profile.buildingMaterial;

        if (fallbackMaterials.TryGetValue(type, out Material material) && material != null)
            return material;

        material = CreateFallbackMaterial($"Default {type} Building Material", GetFallbackColor(type));
        fallbackMaterials[type] = material;
        return material;
    }

    private Color GetFallbackColor(UrbanBlockType type)
    {
        switch (type)
        {
            case UrbanBlockType.Residential:
                return residentialColor;
            case UrbanBlockType.Commercial:
                return commercialColor;
            case UrbanBlockType.Industrial:
                return industrialColor;
            default:
                return defaultBuildingColor;
        }
    }

    private static Material CreateFallbackMaterial(string materialName, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        Material material = new Material(shader);
        material.name = materialName;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);

        return material;
    }

    private static void AssignLayerIfExists(GameObject target, string layerName)
    {
        if (target == null || string.IsNullOrWhiteSpace(layerName)) return;

        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0)
        {
            Debug.LogWarning($"LotBuildingGenerator: Layer '{layerName}' does not exist. Building object stays on its current layer.");
            return;
        }

        target.layer = layer;
    }

    private static void DestroyGeneratedObject(GameObject target)
    {
        if (target == null) return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }
}
