using Clipper2Lib;
using GraphModel;
using RoadGeneration;
using System;
using System.Collections.Generic;
using UnityEngine;



public class RoadNetworkGenerator : MonoBehaviour
{
    [Header("Runtime")]
    public bool generateOnStart = true;

    [Header("Map Settings")]
    public int mapSize = 1000;
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
    public float junctionCutDistance = 8f;
    public bool deriveRoadWidthFromLaneWidth = true;
    public float roadWidth = 7f;
    public bool skipLanesTooShortForJunctionCut = true;

    [Header("Agent Interface")]
    public float agentLaneHeight = 0f;

    [Header("Generated Road Mesh")]
    public bool generateRoadMesh = true;
    public float roadMeshHeight = 0.01f;
    public Material roadMaterial;
    public string roadMeshObjectName = "Road_Surface_Mesh";
    [HideInInspector] public GameObject roadMeshObject;

    private Graph graph;

    public List<LaneGeometry> lanes;
    public List<AgentLaneData> agentLanes;
    private Paths64 polylines;
    public List<RoadPolygon> roadPolygons;
    private Paths64 footprint;


    void Start()
    {
        if (generateOnStart)
            Generate();
    }

    void OnValidate()
    {
        mapSize = Mathf.Max(1, mapSize);
        roadSegmentLength = Mathf.Max(1f, roadSegmentLength);
        laneWidth = Mathf.Max(0.1f, laneWidth);
        junctionCutDistance = Mathf.Max(0f, junctionCutDistance);
        if (deriveRoadWidthFromLaneWidth)
            roadWidth = laneWidth * 2f;
        else
            roadWidth = Mathf.Max(0.1f, roadWidth);
        agentLaneHeight = Mathf.Max(0f, agentLaneHeight);
        roadMeshHeight = Mathf.Max(0f, roadMeshHeight);
    }

    public void Generate()
    {
        ClearGeneratedObjects();
        graph = new Graph();

        System.Random rand = new System.Random(12345); // 固定种子（方便调试）

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
            roadWidth = laneWidth * 2f;

        lanes = LaneGenerator.GenerateLanes(graph, junctionCutDistance, laneWidth * 0.5f, skipLanesTooShortForJunctionCut);
        agentLanes = AgentLaneExporter.Export(lanes, agentLaneHeight);
        Debug.Log("Lane count: " + lanes.Count);
        Debug.Log("Agent lane count: " + agentLanes.Count);


        // Footprint
        var builder = new PolylineBuilder(graph);
        polylines = builder.Build();

        //Generate road footprints
        //roadPolygons = RoadFootprintGenerator.Generate(graph, 2f);//改参的入口
        footprint = RoadFootprintGenerator.Generate(polylines, roadWidth);

        if (generateRoadMesh)
            BuildRoadMeshObject();

    }

    public void ClearGeneratedObjects()
    {
        graph = null;
        lanes = null;
        agentLanes = null;
        polylines = null;
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
    }

    internal Graph GetGraph()
    {
        return graph;
    }

    public List<AgentLaneData> GetAgentLanes()
    {
        return agentLanes;
    }

    // 对外只读暴露用于调试/可视化
    public Paths64 Polylines => polylines;
    public Paths64 FootprintPaths => footprint;
}