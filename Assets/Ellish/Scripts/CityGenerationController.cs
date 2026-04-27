using UnityEngine;

public class CityGenerationController : MonoBehaviour
{
    [Header("Stage Generators")]
    public RoadNetworkGenerator roadNetworkGenerator;
    public WalkableAreaGenerator walkableAreaGenerator;
    public CrosswalkGenerator crosswalkGenerator;
    public BlockAreaGenerator blockAreaGenerator;
    public LotTestGenerator lotTestGenerator;

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
        if (lotTestGenerator == null)
            lotTestGenerator = GetComponentInChildren<LotTestGenerator>();
        if (lotTestGenerator == null)
            lotTestGenerator = FindExistingLotTestGeneratorInScene();
    }

    public void RefreshLotTestGeneratorReference()
    {
        LotTestGenerator configuredLotTestGenerator = FindConfiguredLotTestGeneratorInScene();
        if (configuredLotTestGenerator != null)
        {
            lotTestGenerator = configuredLotTestGenerator;
            return;
        }

        lotTestGenerator = GetComponentInChildren<LotTestGenerator>();
        if (lotTestGenerator == null)
            lotTestGenerator = FindExistingLotTestGeneratorInScene();
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

    public void GenerateLots()
    {
        EnsureGeneratedHierarchy();
        FindStageGenerators();

        if (lotTestGenerator == null)
        {
            lotTestGenerator = gameObject.AddComponent<LotTestGenerator>();
            Debug.LogWarning("CityGenerationController: Auto-created a LotTestGenerator because no existing one was assigned/found. Assign materials on this generated component before expecting custom materials.");
        }

        if (blockAreaGenerator == null || blockAreaGenerator.blocks == null || blockAreaGenerator.blocks.Count == 0)
        {
            Debug.LogWarning("CityGenerationController: Generate blocks before generating lot test volumes.");
            return;
        }

        lotTestGenerator.Generate(blockAreaGenerator, lotRoot);
    }

    public void ClearLots()
    {
        EnsureGeneratedHierarchy();
        if (lotTestGenerator != null)
            lotTestGenerator.Clear(lotRoot);
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

    private static LotTestGenerator FindExistingLotTestGeneratorInScene()
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<LotTestGenerator>(FindObjectsInactive.Include);
#else
        return Object.FindObjectOfType<LotTestGenerator>(true);
#endif
    }

    private static LotTestGenerator FindConfiguredLotTestGeneratorInScene()
    {
#if UNITY_2023_1_OR_NEWER
        LotTestGenerator[] generators = Object.FindObjectsByType<LotTestGenerator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        LotTestGenerator[] generators = Object.FindObjectsOfType<LotTestGenerator>(true);
#endif
        for (int i = 0; i < generators.Length; i++)
        {
            LotTestGenerator generator = generators[i];
            if (generator != null && (generator.buildingMaterial != null || generator.parkMaterial != null))
                return generator;
        }

        return null;
    }

}
