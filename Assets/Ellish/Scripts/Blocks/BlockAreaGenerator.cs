using System.Collections.Generic;
using Clipper2Lib;
using UnityEngine;

public class BlockAreaGenerator : MonoBehaviour
{
    private const float ClipperScale = 1000f;

    [Header("Block Generation")]
    public bool generateBlockMeshes = true;
    public float blockSetbackDistance = 4f;
    public float blockMeshHeight = 0.015f;
    public float minBlockArea = 25f;
    public bool excludeMapEdgeBlocks = true;

    [Header("Park Classification")]
    [Range(0f, 1f)] public float parkProbability = 0.12f;
    public float parkMinArea = 12000f;
    public int parkMinVertexCount = 10;
    public int randomSeed = 12345;

    [Header("Materials")]
    public Material buildingBlockMaterial;
    public Material parkBlockMaterial;

    [Header("Physics and Layers")]
    public bool addBlockMeshCollider = true;
    public string buildingBlockLayerName = "";
    public string parkBlockLayerName = "Walkable";

    [Header("Debug Save")]
    public BlockGenerationProfile blockGenerationProfile;

    [HideInInspector] public Paths64 blockArea;
    [HideInInspector] public Transform generatedBlockRoot;
    public List<BlockData> blocks = new List<BlockData>();

    void OnValidate()
    {
        blockMeshHeight = Mathf.Max(0f, blockMeshHeight);
        blockSetbackDistance = Mathf.Max(0f, blockSetbackDistance);
        minBlockArea = Mathf.Max(0f, minBlockArea);
        parkMinArea = Mathf.Max(0f, parkMinArea);
        parkMinVertexCount = Mathf.Max(3, parkMinVertexCount);
    }

    public void Generate(RoadNetworkGenerator roadNetworkGenerator, Transform parent)
    {
        Clear(parent);

        if (roadNetworkGenerator == null)
        {
            Debug.LogWarning("BlockAreaGenerator: RoadNetworkGenerator is not assigned.");
            return;
        }

        Paths64 roadFootprint = roadNetworkGenerator.FootprintPaths;
        if (roadFootprint == null || roadFootprint.Count == 0)
        {
            Debug.LogWarning("BlockAreaGenerator: Road footprint is empty. Generate roads before generating blocks.");
            return;
        }

        blockArea = BuildBlockArea(roadNetworkGenerator.mapSize, roadFootprint, blockSetbackDistance);
        BuildBlockData(roadNetworkGenerator.mapSize);

        if (!generateBlockMeshes)
            return;

        Transform root = parent != null ? parent : transform;
        generatedBlockRoot = root;
        for (int i = 0; i < blocks.Count; i++)
            CreateBlockObject(blocks[i], root);
    }

    public void Clear(Transform parent)
    {
        blocks.Clear();
        blockArea = null;
        generatedBlockRoot = parent;

        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child.GetComponent<BlockDebugComponent>() == null)
                continue;

            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }

    public List<BlockData> GetBuildingBlocks()
    {
        List<BlockData> buildingBlocks = new List<BlockData>();
        for (int i = 0; i < blocks.Count; i++)
        {
            if (blocks[i].IsBuilding)
                buildingBlocks.Add(blocks[i]);
        }

        return buildingBlocks;
    }

    public List<BlockData> GetParkBlocks()
    {
        List<BlockData> parkBlocks = new List<BlockData>();
        for (int i = 0; i < blocks.Count; i++)
        {
            if (blocks[i].IsPark)
                parkBlocks.Add(blocks[i]);
        }

        return parkBlocks;
    }

    public void ApplyDebugOverridesFromScene()
    {
        BlockDebugComponent[] debugComponents = CollectBlockDebugComponents();
        if (debugComponents.Length == 0)
        {
            Debug.LogWarning("BlockAreaGenerator: No BlockDebugComponent found. Generate blocks before applying overrides.");
            return;
        }

        for (int i = 0; i < debugComponents.Length; i++)
        {
            BlockDebugComponent debug = debugComponents[i];
            BlockLandUse effectiveLandUse = debug.EffectiveLandUse;
            debug.landUse = effectiveLandUse;

            BlockData block = FindBlockByStableKey(debug.stableKey);
            if (block != null)
                block.landUse = effectiveLandUse;

            ApplyObjectPresentation(debug.gameObject, effectiveLandUse);
        }

        Debug.Log($"BlockAreaGenerator: Applied {debugComponents.Length} block debug override(s).");
    }

    public void SaveDebugOverridesToProfile()
    {
        if (blockGenerationProfile == null)
        {
            Debug.LogWarning("BlockAreaGenerator: BlockGenerationProfile is not assigned.");
            return;
        }

        ApplyDebugOverridesFromScene();

        BlockDebugComponent[] debugComponents = CollectBlockDebugComponents();
        if (debugComponents.Length == 0)
        {
            Debug.LogWarning("BlockAreaGenerator: No BlockDebugComponent found. Generate blocks before saving overrides.");
            return;
        }

        for (int i = 0; i < debugComponents.Length; i++)
        {
            BlockDebugComponent debug = debugComponents[i];
            blockGenerationProfile.SetOverride(debug.stableKey, debug.EffectiveLandUse);
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(blockGenerationProfile);
        UnityEditor.AssetDatabase.SaveAssets();
#endif

        Debug.Log($"BlockAreaGenerator: Saved {debugComponents.Length} block override(s) to {blockGenerationProfile.name}.");
    }

    public void ClearSavedOverrides()
    {
        if (blockGenerationProfile == null)
            return;

        blockGenerationProfile.ClearOverrides();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(blockGenerationProfile);
        UnityEditor.AssetDatabase.SaveAssets();
#endif
    }

    private void BuildBlockData(int mapSize)
    {
        if (blockArea == null)
            return;

        System.Random random = new System.Random(randomSeed);

        for (int i = 0; i < blockArea.Count; i++)
        {
            Path64 polygon = blockArea[i];
            if (polygon == null || polygon.Count < 3)
                continue;

            double signedClipperArea = Clipper.Area(polygon);
            if (signedClipperArea <= 0d)
                continue;

            double area = signedClipperArea / (ClipperScale * ClipperScale);
            if (area < minBlockArea)
                continue;

            if (excludeMapEdgeBlocks && TouchesMapBoundary(polygon, mapSize))
                continue;

            Vector2 center = CalculateCentroid(polygon);
            string stableKey = BuildStableKey(center, area);
            BlockLandUse landUse = ClassifyBlock(polygon, area, stableKey, random);

            blocks.Add(new BlockData
            {
                id = blocks.Count,
                stableKey = stableKey,
                landUse = landUse,
                polygon = polygon,
                area = area,
                planarCenter = center,
                center = RoadCoordinateUtility.PlanarToWorld(center, blockMeshHeight)
            });
        }
    }

    private BlockLandUse ClassifyBlock(Path64 polygon, double area, string stableKey, System.Random random)
    {
        if (blockGenerationProfile != null && blockGenerationProfile.TryGetOverride(stableKey, out BlockLandUse savedLandUse))
            return savedLandUse;

        if (polygon.Count >= parkMinVertexCount)
            return BlockLandUse.Park;

        if (area >= parkMinArea)
            return BlockLandUse.Park;

        if (random.NextDouble() < parkProbability)
            return BlockLandUse.Park;

        return BlockLandUse.Building;
    }

    private void CreateBlockObject(BlockData block, Transform parent)
    {
        if (!BlockMeshBuilder.TryBuildBlockSurfaceMesh(block.polygon, blockMeshHeight, out Mesh mesh, out TriangulateResult result))
        {
            Debug.LogWarning($"BlockAreaGenerator: Block mesh triangulation failed for block {block.id}: {result}");
            return;
        }

        mesh.name = $"Block_{block.id:000}_{block.landUse}";

        GameObject blockObject = new GameObject(mesh.name);
        blockObject.transform.SetParent(parent, false);

        MeshFilter meshFilter = blockObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = blockObject.AddComponent<MeshRenderer>();
        meshFilter.sharedMesh = mesh;

        Material material = block.landUse == BlockLandUse.Park ? parkBlockMaterial : buildingBlockMaterial;
        if (material != null)
            meshRenderer.sharedMaterial = material;

        if (addBlockMeshCollider)
        {
            MeshCollider meshCollider = blockObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
        }

        BlockDebugComponent debug = blockObject.AddComponent<BlockDebugComponent>();
        debug.owner = this;
        debug.blockId = block.id;
        debug.stableKey = block.stableKey;
        debug.landUse = block.landUse;
        debug.area = block.area;
        debug.center = block.center;

        ApplyObjectPresentation(blockObject, block.landUse);
        block.meshObject = blockObject;
    }

    private BlockDebugComponent[] CollectBlockDebugComponents()
    {
        if (generatedBlockRoot != null)
            return generatedBlockRoot.GetComponentsInChildren<BlockDebugComponent>(true);

        List<BlockDebugComponent> collected = new List<BlockDebugComponent>();
        for (int i = 0; i < blocks.Count; i++)
        {
            GameObject meshObject = blocks[i].meshObject;
            if (meshObject == null)
                continue;

            BlockDebugComponent debug = meshObject.GetComponent<BlockDebugComponent>();
            if (debug != null)
                collected.Add(debug);
        }

        if (collected.Count > 0)
            return collected.ToArray();

        return GetComponentsInChildren<BlockDebugComponent>(true);
    }

    private void ApplyObjectPresentation(GameObject blockObject, BlockLandUse landUse)
    {
        MeshRenderer meshRenderer = blockObject.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            Material material = landUse == BlockLandUse.Park ? parkBlockMaterial : buildingBlockMaterial;
            if (material != null)
                meshRenderer.sharedMaterial = material;
        }

        string layerName = landUse == BlockLandUse.Park ? parkBlockLayerName : buildingBlockLayerName;
        AssignLayerIfExists(blockObject, layerName);
    }

    private BlockData FindBlockByStableKey(string stableKey)
    {
        for (int i = 0; i < blocks.Count; i++)
        {
            if (blocks[i].stableKey == stableKey)
                return blocks[i];
        }

        return null;
    }

    private static Paths64 BuildBlockArea(int mapSize, Paths64 roadFootprint, float blockSetbackDistance)
    {
        Paths64 mapBounds = new Paths64 { BuildMapBoundsPath(mapSize) };
        Paths64 blockBoundaryFootprint = ExpandFootprint(roadFootprint, blockSetbackDistance);
        return Clipper.Difference(mapBounds, blockBoundaryFootprint, FillRule.NonZero);
    }

    private static Paths64 ExpandFootprint(Paths64 footprint, float setbackDistance)
    {
        if (footprint == null || footprint.Count == 0)
            return new Paths64();

        if (setbackDistance <= 0f)
            return footprint;

        ClipperOffset offset = new ClipperOffset();
        offset.AddPaths(footprint, JoinType.Round, EndType.Polygon);

        Paths64 expanded = new Paths64();
        offset.Execute(setbackDistance * ClipperScale, expanded);
        return Clipper.Union(expanded, FillRule.NonZero);
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

    private static bool TouchesMapBoundary(Path64 polygon, int mapSize)
    {
        long extent = Mathf.Max(1, mapSize) * (long)ClipperScale;
        const long tolerance = 2;

        for (int i = 0; i < polygon.Count; i++)
        {
            Point64 point = polygon[i];
            if (Mathf.Abs((float)(point.X - extent)) <= tolerance ||
                Mathf.Abs((float)(point.X + extent)) <= tolerance ||
                Mathf.Abs((float)(point.Y - extent)) <= tolerance ||
                Mathf.Abs((float)(point.Y + extent)) <= tolerance)
                return true;
        }

        return false;
    }

    private static Vector2 CalculateCentroid(Path64 polygon)
    {
        if (polygon == null || polygon.Count == 0)
            return Vector2.zero;

        double signedArea = 0d;
        double centroidX = 0d;
        double centroidY = 0d;

        for (int i = 0; i < polygon.Count; i++)
        {
            Point64 current = polygon[i];
            Point64 next = polygon[(i + 1) % polygon.Count];
            double cross = current.X * (double)next.Y - next.X * (double)current.Y;
            signedArea += cross;
            centroidX += (current.X + next.X) * cross;
            centroidY += (current.Y + next.Y) * cross;
        }

        signedArea *= 0.5d;
        if (System.Math.Abs(signedArea) < double.Epsilon)
        {
            double averageX = 0d;
            double averageY = 0d;
            for (int i = 0; i < polygon.Count; i++)
            {
                averageX += polygon[i].X;
                averageY += polygon[i].Y;
            }

            return new Vector2((float)(averageX / polygon.Count / ClipperScale), (float)(averageY / polygon.Count / ClipperScale));
        }

        centroidX /= 6d * signedArea;
        centroidY /= 6d * signedArea;
        return new Vector2((float)(centroidX / ClipperScale), (float)(centroidY / ClipperScale));
    }

    private static string BuildStableKey(Vector2 center, double area)
    {
        int roundedX = Mathf.RoundToInt(center.x * 10f);
        int roundedY = Mathf.RoundToInt(center.y * 10f);
        int roundedArea = Mathf.RoundToInt((float)area);
        return $"{roundedX}_{roundedY}_{roundedArea}";
    }

    private static void AssignLayerIfExists(GameObject target, string layerName)
    {
        if (target == null || string.IsNullOrWhiteSpace(layerName)) return;

        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0)
        {
            Debug.LogWarning($"BlockAreaGenerator: Layer '{layerName}' does not exist. Block object stays on its current layer.");
            return;
        }

        target.layer = layer;
    }
}
