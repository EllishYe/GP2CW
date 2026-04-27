using System.Collections.Generic;
using Clipper2Lib;
using UnityEngine;
using UnityEngine.Rendering;

public class LotTestGenerator : MonoBehaviour
{
    private const float ClipperScale = 1000f;

    [Header("Temporary Test Output")]
    public bool generateBuildingTestVolumes = true;
    public bool generateParkTestVolumes = true;
    public string buildingObjectPrefix = "LotTest_Building";
    public string parkObjectPrefix = "LotTest_Park";

    [Header("Footprint")]
    public float buildingInsetDistance = 3f;

    [Header("Heights")]
    public float buildingBaseHeight = 0.02f;
    public float buildingHeight = 18f;
    public float parkBaseHeight = 0.02f;
    public float parkHeight = 0.12f;

    [Header("Materials")]
    public Material buildingMaterial;
    public Material parkMaterial;
    public Color fallbackBuildingColor = new Color(0.72f, 0.72f, 0.76f, 1f);
    public Color fallbackParkColor = new Color(0.38f, 0.72f, 0.32f, 1f);

    [Header("Physics and Layers")]
    public bool addMeshCollider = true;
    public string buildingLayerName = "Building";
    public string parkLayerName = "Walkable";

    public List<GameObject> generatedObjects = new List<GameObject>();

    private Material fallbackBuildingMaterialInstance;
    private Material fallbackParkMaterialInstance;

    void OnValidate()
    {
        buildingInsetDistance = Mathf.Max(0f, buildingInsetDistance);
        buildingBaseHeight = Mathf.Max(0f, buildingBaseHeight);
        buildingHeight = Mathf.Max(buildingBaseHeight + 0.01f, buildingHeight);
        parkBaseHeight = Mathf.Max(0f, parkBaseHeight);
        parkHeight = Mathf.Max(parkBaseHeight + 0.01f, parkHeight);
    }

    public void Generate(BlockAreaGenerator blockAreaGenerator, Transform parent)
    {
        Clear(parent);

        if (blockAreaGenerator == null)
        {
            Debug.LogWarning("LotTestGenerator: BlockAreaGenerator is not assigned.");
            return;
        }

        blockAreaGenerator.ApplyDebugOverridesFromScene();

        Transform root = parent != null ? parent : transform;

        if (generateBuildingTestVolumes)
            GenerateBuildingVolumes(blockAreaGenerator.GetBuildingBlocks(), root);

        if (generateParkTestVolumes)
            GenerateParkVolumes(blockAreaGenerator.GetParkBlocks(), root);
    }

    public void Clear(Transform parent)
    {
        for (int i = generatedObjects.Count - 1; i >= 0; i--)
            DestroyGeneratedObject(generatedObjects[i]);
        generatedObjects.Clear();

        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (!child.name.StartsWith(buildingObjectPrefix) && !child.name.StartsWith(parkObjectPrefix))
                continue;

            DestroyGeneratedObject(child.gameObject);
        }
    }

    private void GenerateBuildingVolumes(List<BlockData> buildingBlocks, Transform parent)
    {
        for (int i = 0; i < buildingBlocks.Count; i++)
        {
            BlockData block = buildingBlocks[i];
            Path64 footprint = BuildInsetFootprint(block.polygon, buildingInsetDistance);
            if (footprint == null || footprint.Count < 3)
                continue;

            if (!TryBuildExtrudedMesh(footprint, buildingBaseHeight, buildingHeight, out Mesh mesh))
                continue;

            mesh.name = $"{buildingObjectPrefix}_{block.id:000}";
            CreateVolumeObject(mesh.name, mesh, parent, ResolveBuildingMaterial(), buildingLayerName);
        }
    }

    private void GenerateParkVolumes(List<BlockData> parkBlocks, Transform parent)
    {
        for (int i = 0; i < parkBlocks.Count; i++)
        {
            BlockData block = parkBlocks[i];
            if (!TryBuildExtrudedMesh(block.polygon, parkBaseHeight, parkHeight, out Mesh mesh))
                continue;

            mesh.name = $"{parkObjectPrefix}_{block.id:000}";
            CreateVolumeObject(mesh.name, mesh, parent, ResolveParkMaterial(), parkLayerName);
        }
    }

    private GameObject CreateVolumeObject(string objectName, Mesh mesh, Transform parent, Material material, string layerName)
    {
        GameObject volumeObject = new GameObject(objectName);
        volumeObject.transform.SetParent(parent, false);

        MeshFilter meshFilter = volumeObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = volumeObject.AddComponent<MeshRenderer>();
        meshFilter.sharedMesh = mesh;
        meshRenderer.sharedMaterial = material;

        if (addMeshCollider)
        {
            MeshCollider meshCollider = volumeObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
        }

        AssignLayerIfExists(volumeObject, layerName);
        generatedObjects.Add(volumeObject);
        return volumeObject;
    }

    private Material ResolveBuildingMaterial()
    {
        if (buildingMaterial != null)
            return buildingMaterial;

        if (fallbackBuildingMaterialInstance == null)
            fallbackBuildingMaterialInstance = CreateFallbackMaterial("LotTest Default Building Material", fallbackBuildingColor);

        return fallbackBuildingMaterialInstance;
    }

    private Material ResolveParkMaterial()
    {
        if (parkMaterial != null)
            return parkMaterial;

        if (fallbackParkMaterialInstance == null)
            fallbackParkMaterialInstance = CreateFallbackMaterial("LotTest Default Park Material", fallbackParkColor);

        return fallbackParkMaterialInstance;
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

    private static Path64 BuildInsetFootprint(Path64 source, float insetDistance)
    {
        if (source == null || source.Count < 3)
            return null;

        if (insetDistance <= 0f)
            return source;

        ClipperOffset offset = new ClipperOffset();
        offset.AddPath(source, JoinType.Round, EndType.Polygon);

        Paths64 insetPaths = new Paths64();
        offset.Execute(-insetDistance * ClipperScale, insetPaths);

        return FindLargestPositivePath(insetPaths);
    }

    private static Path64 FindLargestPositivePath(Paths64 paths)
    {
        if (paths == null || paths.Count == 0)
            return null;

        Path64 bestPath = null;
        double bestArea = 0d;
        for (int i = 0; i < paths.Count; i++)
        {
            Path64 path = paths[i];
            if (path == null || path.Count < 3)
                continue;

            double area = Clipper.Area(path);
            if (area > bestArea)
            {
                bestArea = area;
                bestPath = path;
            }
        }

        return bestPath;
    }

    private static bool TryBuildExtrudedMesh(Path64 footprint, float baseHeight, float topHeight, out Mesh mesh)
    {
        mesh = null;
        if (footprint == null || footprint.Count < 3)
            return false;

        Paths64 paths = new Paths64 { footprint };
        TriangulateResult result = Clipper.Triangulate(paths, out Paths64 triangles);
        if (result != TriangulateResult.success || triangles == null || triangles.Count == 0)
        {
            Debug.LogWarning($"LotTestGenerator: Triangulation failed: {result}");
            return false;
        }

        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> indices = new List<int>();
        Dictionary<Point64, int> topPointToIndex = new Dictionary<Point64, int>();
        Dictionary<Point64, int> bottomPointToIndex = new Dictionary<Point64, int>();

        for (int i = 0; i < triangles.Count; i++)
        {
            Path64 triangle = triangles[i];
            if (triangle == null || triangle.Count < 3)
                continue;

            int aTop = GetOrAddVertex(triangle[0], topHeight, vertices, uvs, topPointToIndex);
            int bTop = GetOrAddVertex(triangle[1], topHeight, vertices, uvs, topPointToIndex);
            int cTop = GetOrAddVertex(triangle[2], topHeight, vertices, uvs, topPointToIndex);
            AddTriangleFacingUp(vertices, indices, aTop, bTop, cTop);

            int aBottom = GetOrAddVertex(triangle[0], baseHeight, vertices, uvs, bottomPointToIndex);
            int bBottom = GetOrAddVertex(triangle[1], baseHeight, vertices, uvs, bottomPointToIndex);
            int cBottom = GetOrAddVertex(triangle[2], baseHeight, vertices, uvs, bottomPointToIndex);
            AddTriangleFacingDown(vertices, indices, aBottom, bBottom, cBottom);
        }

        for (int i = 0; i < footprint.Count; i++)
        {
            Point64 current = footprint[i];
            Point64 next = footprint[(i + 1) % footprint.Count];

            int currentBottom = GetOrAddVertex(current, baseHeight, vertices, uvs, bottomPointToIndex);
            int nextBottom = GetOrAddVertex(next, baseHeight, vertices, uvs, bottomPointToIndex);
            int currentTop = GetOrAddVertex(current, topHeight, vertices, uvs, topPointToIndex);
            int nextTop = GetOrAddVertex(next, topHeight, vertices, uvs, topPointToIndex);

            indices.Add(currentBottom);
            indices.Add(currentTop);
            indices.Add(nextTop);

            indices.Add(currentBottom);
            indices.Add(nextTop);
            indices.Add(nextBottom);
        }

        mesh = new Mesh();
        mesh.name = "Extruded Lot Test Mesh";
        if (vertices.Count > 65535)
            mesh.indexFormat = IndexFormat.UInt32;

        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(indices, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return true;
    }

    private static int GetOrAddVertex(Point64 point, float height, List<Vector3> vertices, List<Vector2> uvs, Dictionary<Point64, int> pointToIndex)
    {
        if (pointToIndex.TryGetValue(point, out int index))
            return index;

        float x = point.X / ClipperScale;
        float y = point.Y / ClipperScale;
        Vector3 world = RoadCoordinateUtility.PlanarToWorld(x, y, height);

        index = vertices.Count;
        pointToIndex.Add(point, index);
        vertices.Add(world);
        uvs.Add(new Vector2(x, y));
        return index;
    }

    private static void AddTriangleFacingUp(List<Vector3> vertices, List<int> indices, int a, int b, int c)
    {
        Vector3 normal = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
        if (normal.y < 0f)
        {
            indices.Add(a);
            indices.Add(c);
            indices.Add(b);
        }
        else
        {
            indices.Add(a);
            indices.Add(b);
            indices.Add(c);
        }
    }

    private static void AddTriangleFacingDown(List<Vector3> vertices, List<int> indices, int a, int b, int c)
    {
        Vector3 normal = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
        if (normal.y > 0f)
        {
            indices.Add(a);
            indices.Add(c);
            indices.Add(b);
        }
        else
        {
            indices.Add(a);
            indices.Add(b);
            indices.Add(c);
        }
    }

    private static void AssignLayerIfExists(GameObject target, string layerName)
    {
        if (target == null || string.IsNullOrWhiteSpace(layerName)) return;

        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0)
        {
            Debug.LogWarning($"LotTestGenerator: Layer '{layerName}' does not exist. Generated object stays on its current layer.");
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
