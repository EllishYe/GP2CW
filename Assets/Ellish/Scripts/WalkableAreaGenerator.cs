using Clipper2Lib;
using UnityEngine;

public class WalkableAreaGenerator : MonoBehaviour
{
    private const float ClipperScale = 1000f;

    [Header("Walkable Area")]
    public bool generateWalkableMesh = true;
    public float walkableMeshHeight = 0f;
    public Material walkableMaterial;
    public string walkableObjectName = "NonRoad_Walkable_Mesh";
    public string walkableLayerName = "Walkable";
    public bool addWalkableMeshCollider = true;

    [HideInInspector] public GameObject walkableMeshObject;
    [HideInInspector] public Paths64 walkableArea;

    void OnValidate()
    {
        walkableMeshHeight = Mathf.Max(0f, walkableMeshHeight);
    }

    public void Generate(RoadNetworkGenerator roadNetworkGenerator, Transform parent)
    {
        Clear(parent);

        if (!generateWalkableMesh)
            return;

        if (roadNetworkGenerator == null)
        {
            Debug.LogWarning("WalkableAreaGenerator: RoadNetworkGenerator is not assigned.");
            return;
        }

        Paths64 roadFootprint = roadNetworkGenerator.FootprintPaths;
        if (roadFootprint == null || roadFootprint.Count == 0)
        {
            Debug.LogWarning("WalkableAreaGenerator: Road footprint is empty. Generate roads before generating walkable area.");
            return;
        }

        walkableArea = BuildNonRoadWalkableArea(roadNetworkGenerator.mapSize, roadFootprint);
        if (!RoadMeshBuilder.TryBuildRoadMesh(walkableArea, walkableMeshHeight, out Mesh mesh, out TriangulateResult result))
        {
            Debug.LogWarning($"WalkableAreaGenerator: Walkable mesh triangulation failed: {result}");
            return;
        }

        mesh.name = "Non-Road Walkable Mesh";
        walkableMeshObject = new GameObject(walkableObjectName);
        walkableMeshObject.transform.SetParent(parent != null ? parent : transform, false);

        MeshFilter meshFilter = walkableMeshObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = walkableMeshObject.AddComponent<MeshRenderer>();
        meshFilter.sharedMesh = mesh;
        if (walkableMaterial != null)
            meshRenderer.sharedMaterial = walkableMaterial;

        if (addWalkableMeshCollider)
        {
            MeshCollider meshCollider = walkableMeshObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
        }

        AssignLayerIfExists(walkableMeshObject, walkableLayerName);
    }

    public void Clear(Transform parent)
    {
        walkableArea = null;

        if (walkableMeshObject == null && parent != null)
        {
            Transform existing = parent.Find(walkableObjectName);
            if (existing != null)
                walkableMeshObject = existing.gameObject;
        }

        if (walkableMeshObject == null) return;

        if (Application.isPlaying)
            Destroy(walkableMeshObject);
        else
            DestroyImmediate(walkableMeshObject);

        walkableMeshObject = null;
    }

    private static Paths64 BuildNonRoadWalkableArea(int mapSize, Paths64 roadFootprint)
    {
        Paths64 mapBounds = new Paths64 { BuildMapBoundsPath(mapSize) };
        return Clipper.Difference(mapBounds, roadFootprint, FillRule.NonZero);
    }

    private static Path64 BuildMapBoundsPath(int mapSize)
    {
        long extent = Mathf.Max(1, mapSize) * (long)ClipperScale;
        return new Path64
        {
            new Point64(-extent, -extent),
            new Point64(extent, -extent),
            new Point64(extent, extent),
            new Point64(-extent, extent)
        };
    }

    private static void AssignLayerIfExists(GameObject target, string layerName)
    {
        if (target == null || string.IsNullOrWhiteSpace(layerName)) return;

        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0)
        {
            Debug.LogWarning($"WalkableAreaGenerator: Layer '{layerName}' does not exist. Walkable object stays on its current layer.");
            return;
        }

        SetLayerRecursively(target, layer);
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        target.layer = layer;
        for (int i = 0; i < target.transform.childCount; i++)
            SetLayerRecursively(target.transform.GetChild(i).gameObject, layer);
    }
}
