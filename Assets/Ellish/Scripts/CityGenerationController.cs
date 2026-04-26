using UnityEngine;

public class CityGenerationController : MonoBehaviour
{
    [Header("Stage Generators")]
    public RoadNetworkGenerator roadNetworkGenerator;

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
        Debug.LogWarning("CityGenerationController: Walkable generation is not implemented yet.");
    }

    public void ClearWalkable()
    {
        EnsureGeneratedHierarchy();
        ClearChildren(walkableRoot);
    }

    public void GenerateBlocks()
    {
        EnsureGeneratedHierarchy();
        Debug.LogWarning("CityGenerationController: Block generation is not implemented yet.");
    }

    public void ClearBlocks()
    {
        EnsureGeneratedHierarchy();
        ClearChildren(blockRoot);
    }

    public void GenerateLots()
    {
        EnsureGeneratedHierarchy();
        Debug.LogWarning("CityGenerationController: Lot generation is not implemented yet.");
    }

    public void ClearLots()
    {
        EnsureGeneratedHierarchy();
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
}
