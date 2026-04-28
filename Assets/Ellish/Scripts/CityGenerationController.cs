using UnityEngine;

public class CityGenerationController : MonoBehaviour
{
    [Header("Stage Generators")]
    public RoadNetworkGenerator roadNetworkGenerator;
    public WalkableAreaGenerator walkableAreaGenerator;
    public CrosswalkGenerator crosswalkGenerator;
    public BlockAreaGenerator blockAreaGenerator;
    public LotAreaGenerator lotAreaGenerator;
    public LotBuildingGenerator lotBuildingGenerator;

    [Header("Runtime")]
    public bool generateRoadsOnStart = false;

    [Header("Generated Hierarchy")]
    public string generatedRootName = "GeneratedCity";
    public string roadRootName = "Road";
    public string walkableRootName = "Walkable";
    public string blockRootName = "Blocks";
    public string lotRootName = "Lots";

    [HideInInspector] public Transform generatedRoot;
    [HideInInspector] public Transform roadRoot;
    [HideInInspector] public Transform walkableRoot;
    [HideInInspector] public Transform blockRoot;
    [HideInInspector] public Transform lotRoot;

    void Reset()
    {
        FindStageGenerators();
    }

    void OnValidate()
    {
        if (roadNetworkGenerator == null)
            FindStageGenerators();
    }

    void Start()
    {
        if (generateRoadsOnStart)
            GenerateRoads();
    }

    public void FindStageGenerators()
    {
        if (roadNetworkGenerator == null)
            roadNetworkGenerator = GetComponentInChildren<RoadNetworkGenerator>();
        if (walkableAreaGenerator == null)
            walkableAreaGenerator = GetComponentInChildren<WalkableAreaGenerator>();
        if (crosswalkGenerator == null)
            crosswalkGenerator = GetComponentInChildren<CrosswalkGenerator>();
        if (blockAreaGenerator == null)
            blockAreaGenerator = GetComponentInChildren<BlockAreaGenerator>();
        if (lotAreaGenerator == null)
            lotAreaGenerator = GetComponentInChildren<LotAreaGenerator>();
        if (lotBuildingGenerator == null)
            lotBuildingGenerator = GetComponentInChildren<LotBuildingGenerator>();
    }

    public void EnsureGeneratedHierarchy()
    {
        generatedRoot = FindOrCreateChild(transform, generatedRootName);
        roadRoot = FindOrCreateChild(generatedRoot, roadRootName);
        walkableRoot = FindOrCreateChild(generatedRoot, walkableRootName);
        blockRoot = FindOrCreateChild(generatedRoot, blockRootName);
        lotRoot = FindOrCreateChild(generatedRoot, lotRootName);
    }

    public void GenerateRoads()
    {
        EnsureGeneratedHierarchy();
        FindStageGenerators();

        if (roadNetworkGenerator == null)
        {
            Debug.LogWarning("CityGenerationController: RoadNetworkGenerator is not assigned.");
            return;
        }

        roadNetworkGenerator.Generate();
    }

    public void ClearRoads()
    {
        if (roadNetworkGenerator != null)
            roadNetworkGenerator.ClearGeneratedObjects();
    }

    public void GenerateWalkable()
    {
        EnsureGeneratedHierarchy();
        FindStageGenerators();

        if (walkableAreaGenerator == null)
            walkableAreaGenerator = gameObject.AddComponent<WalkableAreaGenerator>();
        if (crosswalkGenerator == null)
            crosswalkGenerator = gameObject.AddComponent<CrosswalkGenerator>();

        walkableAreaGenerator.Generate(roadNetworkGenerator, walkableRoot);
        crosswalkGenerator.Generate(roadNetworkGenerator, walkableRoot);
    }

    public void ClearWalkable()
    {
        EnsureGeneratedHierarchy();
        if (walkableAreaGenerator != null)
            walkableAreaGenerator.Clear(walkableRoot);
        if (crosswalkGenerator != null)
            crosswalkGenerator.Clear(walkableRoot);
    }

    public void GenerateBlocks()
    {
        EnsureGeneratedHierarchy();
        FindStageGenerators();

        if (blockAreaGenerator == null)
            blockAreaGenerator = gameObject.AddComponent<BlockAreaGenerator>();

        blockAreaGenerator.Generate(roadNetworkGenerator, blockRoot);
    }

    public void ClearBlocks()
    {
        EnsureGeneratedHierarchy();
        if (blockAreaGenerator != null)
            blockAreaGenerator.Clear(blockRoot);
        ClearChildren(blockRoot);
    }

    public void ApplyBlockDebugOverrides()
    {
        EnsureGeneratedHierarchy();
        if (blockAreaGenerator != null)
        {
            blockAreaGenerator.generatedBlockRoot = blockRoot;
            blockAreaGenerator.ApplyDebugOverridesFromScene();
        }
    }

    public void SaveBlockDebugOverrides()
    {
        EnsureGeneratedHierarchy();
        if (blockAreaGenerator != null)
        {
            blockAreaGenerator.generatedBlockRoot = blockRoot;
            blockAreaGenerator.SaveDebugOverridesToProfile();
        }
    }

    public void ClearSavedBlockOverrides()
    {
        if (blockAreaGenerator != null)
            blockAreaGenerator.ClearSavedOverrides();
    }

    public void AssignBlockTypes()
    {
        EnsureGeneratedHierarchy();
        FindStageGenerators();

        if (lotAreaGenerator == null)
            lotAreaGenerator = gameObject.AddComponent<LotAreaGenerator>();

        if (blockAreaGenerator == null || blockAreaGenerator.blocks == null || blockAreaGenerator.blocks.Count == 0)
        {
            Debug.LogWarning("CityGenerationController: Generate blocks before assigning block types.");
            return;
        }

        lotAreaGenerator.AssignBlockTypes(blockAreaGenerator, roadNetworkGenerator);
    }

    public void ApplyBlockTypeOverrides()
    {
        EnsureGeneratedHierarchy();
        FindStageGenerators();

        if (lotAreaGenerator != null && blockAreaGenerator != null)
            lotAreaGenerator.ApplyBlockTypeOverridesFromScene(blockAreaGenerator);
    }

    public void SaveBlockTypeOverrides()
    {
        EnsureGeneratedHierarchy();
        FindStageGenerators();

        if (lotAreaGenerator != null && blockAreaGenerator != null)
            lotAreaGenerator.SaveBlockTypeOverridesToProfile(blockAreaGenerator);
    }

    public void ClearSavedBlockTypeOverrides()
    {
        FindStageGenerators();

        if (lotAreaGenerator != null && blockAreaGenerator != null)
            lotAreaGenerator.ClearSavedBlockTypeOverrides(blockAreaGenerator);
    }

    public void GenerateLots()
    {
        EnsureGeneratedHierarchy();
        FindStageGenerators();

        if (lotAreaGenerator == null)
            lotAreaGenerator = gameObject.AddComponent<LotAreaGenerator>();

        if (blockAreaGenerator == null || blockAreaGenerator.blocks == null || blockAreaGenerator.blocks.Count == 0)
        {
            Debug.LogWarning("CityGenerationController: Generate blocks before generating lots.");
            return;
        }

        if (!HasAssignedBlockTypes())
            lotAreaGenerator.AssignBlockTypes(blockAreaGenerator, roadNetworkGenerator);

        lotAreaGenerator.ApplyBlockTypeOverridesFromScene(blockAreaGenerator);
        lotAreaGenerator.BuildLotsFromAssignedBlockTypes(blockAreaGenerator, roadNetworkGenerator);

        if (lotBuildingGenerator == null)
            lotBuildingGenerator = gameObject.AddComponent<LotBuildingGenerator>();

        lotBuildingGenerator.Generate(lotAreaGenerator, lotRoot);
    }

    public void ClearLots()
    {
        EnsureGeneratedHierarchy();
        if (lotAreaGenerator != null)
            lotAreaGenerator.Clear();
        if (lotBuildingGenerator != null)
            lotBuildingGenerator.Clear(lotRoot);
        ClearChildren(lotRoot);
    }

    public void GenerateAll()
    {
        GenerateRoads();
        GenerateWalkable();
        GenerateBlocks();
        GenerateLots();
    }

    public void ClearAll()
    {
        ClearLots();
        ClearBlocks();
        ClearWalkable();
        ClearRoads();
    }

    private static Transform FindOrCreateChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
            return child;

        GameObject childObject = new GameObject(childName);
        childObject.transform.SetParent(parent, false);
        return childObject.transform;
    }

    private static void ClearChildren(Transform root)
    {
        if (root == null) return;

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Transform child = root.GetChild(i);
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }

    private bool HasAssignedBlockTypes()
    {
        if (blockAreaGenerator == null || blockAreaGenerator.blocks == null)
            return false;

        for (int i = 0; i < blockAreaGenerator.blocks.Count; i++)
        {
            BlockData block = blockAreaGenerator.blocks[i];
            if (block != null && block.IsBuilding && block.urbanBlockType != UrbanBlockType.Default)
                return true;
        }

        return false;
    }

}
