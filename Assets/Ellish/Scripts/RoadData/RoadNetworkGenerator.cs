using Clipper2Lib;
using GraphModel;
using RoadGeneration;
using System;
using System.Collections.Generic;
using UnityEngine;



public class RoadNetworkGenerator : MonoBehaviour
{
    [Header("Profile")]
    public RoadNetworkProfile profile;

    [Header("Runtime")]
    public bool generateOnStart = true;

    [Header("Map Settings")]
    public int mapSize = 1000;
    public int randomSeed = 12345;
    public float roadSegmentLength = 100f;

    [Header("Major Roads")]
    public int majorRoadCount = 200;
    public int maxLeanAngle = 20;
    public float branchProbability = 0.1f;

    [Header("Minor Roads")]
    public int minorRoadCount = 400;
    public float deletionProbability = 0.2f;
    
    [Header("Road Dimensions")]
    public float laneWidth = 3.5f;
    public int majorLanesPerDirection = 2;
    public int minorLanesPerDirection = 1;
    public float junctionCutDistance = 8f;
    public bool deriveRoadWidthFromLaneWidth = true;
    public float majorRoadWidth = 14f;
    public float minorRoadWidth = 7f;
    public bool skipLanesTooShortForJunctionCut = true;
    [HideInInspector] public float roadWidth = 14f;

    [Header("Agent Interface")]
    public float agentLaneHeight = 0f;

    [Header("Generated Road Mesh")]
    public bool generateRoadMesh = true;
    public float roadMeshHeight = 0.01f;
    public Material roadMaterial;
    public string roadMeshObjectName = "Road_Surface_Mesh";
    public bool addRoadMeshCollider = true;
    public string roadLayerName = "Road";
    [HideInInspector] public GameObject roadMeshObject;

    private Graph graph;

    public List<LaneGeometry> lanes;
    public List<AgentLaneData> agentLanes;
    private Paths64 polylines;
    private Paths64 majorPolylines;
    private Paths64 minorPolylines;
    public List<RoadPolygon> roadPolygons;
    private Paths64 footprint;


    void Start()
    {
        if (generateOnStart)
            Generate();
    }

    void OnValidate()
    {
        SanitizeSettings();
    }

    public void ApplyProfile()
    {
        if (profile == null)
        {
            Debug.LogWarning("RoadNetworkGenerator: No RoadNetworkProfile is assigned.");
            return;
        }

        profile.ApplyTo(this);
    }

    public void CaptureCurrentSettingsToProfile()
    {
        if (profile == null)
        {
            Debug.LogWarning("RoadNetworkGenerator: No RoadNetworkProfile is assigned.");
            return;
        }

        profile.CaptureFrom(this);
    }

    public void SanitizeSettings()
    {
        mapSize = Mathf.Max(1, mapSize);
        roadSegmentLength = Mathf.Max(1f, roadSegmentLength);
        laneWidth = Mathf.Max(0.1f, laneWidth);
        majorLanesPerDirection = Mathf.Max(1, majorLanesPerDirection);
        minorLanesPerDirection = Mathf.Max(1, minorLanesPerDirection);
        junctionCutDistance = Mathf.Max(0f, junctionCutDistance);
        if (deriveRoadWidthFromLaneWidth)
            ApplyDerivedRoadWidths();
        else
        {
            majorRoadWidth = Mathf.Max(0.1f, majorRoadWidth);
            minorRoadWidth = Mathf.Max(0.1f, minorRoadWidth);
            roadWidth = Mathf.Max(majorRoadWidth, minorRoadWidth);
        }
        agentLaneHeight = Mathf.Max(0f, agentLaneHeight);
        roadMeshHeight = Mathf.Max(0f, roadMeshHeight);
    }

    public void Generate()
    {
        ClearGeneratedObjects();
        graph = new Graph();

        System.Random rand = new System.Random(randomSeed); // Fixed seed for repeatable generation.

        // Generate major roads 
        MajorGenerator majorGen = new MajorGenerator(
            rand,
            mapSize,
            roadSegmentLength,
            majorRoadCount,
            maxLeanAngle,
            branchProbability,
            graph
        );

        majorGen.Run();
        var majorSegments = majorGen.GetRoadSegments();

        // Generate minor roads
        MinorGenerator minorGen = new MinorGenerator(
            rand,
            mapSize,
            roadSegmentLength,
            minorRoadCount,
            deletionProbability,
            graph,
            majorSegments
        );

        minorGen.Run();

        RoadNetworkData data = GraphConverter.Convert(graph);// Convert the graph to RoadData

        Debug.Log("Road Network Generated!");

        // Generate Lanes from the graph's edges
        if (deriveRoadWidthFromLaneWidth)
            ApplyDerivedRoadWidths();

        lanes = LaneGenerator.GenerateLanes(graph, junctionCutDistance, laneWidth, majorLanesPerDirection, minorLanesPerDirection, skipLanesTooShortForJunctionCut);
        agentLanes = AgentLaneExporter.Export(lanes, agentLaneHeight);
        Debug.Log("Lane count: " + lanes.Count);
        Debug.Log("Agent lane count: " + agentLanes.Count);


        // Footprint
        var builder = new PolylineBuilder(graph);
        majorPolylines = builder.BuildMajor();
        minorPolylines = builder.BuildMinor();
        polylines = CombinePolylines(majorPolylines, minorPolylines);

        //Generate road footprints
        //roadPolygons = RoadFootprintGenerator.Generate(graph, 2f);
        footprint = RoadFootprintGenerator.GenerateByRoadType(majorPolylines, majorRoadWidth, minorPolylines, minorRoadWidth);
        if (generateRoadMesh)
            BuildRoadMeshObject();

    }

    public void ClearGeneratedObjects()
    {
        graph = null;
        lanes = null;
        agentLanes = null;
        polylines = null;
        majorPolylines = null;
        minorPolylines = null;
        roadPolygons = null;
        footprint = null;

        if (roadMeshObject == null)
        {
            Transform existing = transform.Find(roadMeshObjectName);
            if (existing != null)
                roadMeshObject = existing.gameObject;
        }

        if (roadMeshObject == null) return;

        if (Application.isPlaying)
            Destroy(roadMeshObject);
        else
            DestroyImmediate(roadMeshObject);

        roadMeshObject = null;
    }

    private void BuildRoadMeshObject()
    {
        if (!RoadMeshBuilder.TryBuildRoadMesh(footprint, roadMeshHeight, out Mesh mesh, out TriangulateResult result))
        {
            Debug.LogWarning($"Road mesh triangulation failed: {result}");
            return;
        }

        roadMeshObject = new GameObject(roadMeshObjectName);
        roadMeshObject.transform.SetParent(transform, false);
        roadMeshObject.transform.localPosition = Vector3.zero;
        roadMeshObject.transform.localRotation = Quaternion.identity;
        roadMeshObject.transform.localScale = Vector3.one;

        MeshFilter meshFilter = roadMeshObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = roadMeshObject.AddComponent<MeshRenderer>();
        meshFilter.sharedMesh = mesh;
        if (roadMaterial != null)
            meshRenderer.sharedMaterial = roadMaterial;

        if (addRoadMeshCollider)
        {
            MeshCollider meshCollider = roadMeshObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
        }

        AssignLayerIfExists(roadMeshObject, roadLayerName);
    }

    private void ApplyDerivedRoadWidths()
    {
        majorRoadWidth = laneWidth * majorLanesPerDirection * 2f;
        minorRoadWidth = laneWidth * minorLanesPerDirection * 2f;
        roadWidth = Mathf.Max(majorRoadWidth, minorRoadWidth);
    }

    private Paths64 CombinePolylines(Paths64 first, Paths64 second)
    {
        Paths64 combined = new Paths64();
        AddPaths(combined, first);
        AddPaths(combined, second);
        return combined;
    }

    private void AddPaths(Paths64 target, Paths64 source)
    {
        if (source == null) return;
        foreach (Path64 path in source)
            target.Add(path);
    }
    internal Graph GetGraph()
    {
        return graph;
    }

    public List<AgentLaneData> GetAgentLanes()
    {
        return agentLanes;
    }

    private static void AssignLayerIfExists(GameObject target, string layerName)
    {
        if (target == null || string.IsNullOrWhiteSpace(layerName)) return;

        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0)
        {
            Debug.LogWarning($"RoadNetworkGenerator: Layer '{layerName}' does not exist. Road object stays on its current layer.");
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
    // Exposed for debugging and visualization.
    public Paths64 Polylines => polylines;
    public Paths64 MajorPolylines => majorPolylines;
    public Paths64 MinorPolylines => minorPolylines;
    public Paths64 FootprintPaths => footprint;
    public Graph Graph => graph;
}
