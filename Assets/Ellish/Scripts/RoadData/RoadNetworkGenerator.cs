using Clipper2Lib;
using GraphModel;
using RoadGeneration;
using System;
using System.Collections.Generic;
using UnityEngine;



public class RoadNetworkGenerator : MonoBehaviour
{
    [Header("Map Settings")]
    public int mapSize = 100;

    [Header("Major Roads")]
    public int majorRoadCount = 200;
    public int maxLeanAngle = 20;
    public float branchProbability = 0.1f;

    [Header("Minor Roads")]
    public int minorRoadCount = 400;
    public float deletionProbability = 0.2f;
    
    [Header("Road Width")]
    public float roadWidth = 2f;

    [Header("Agent Interface")]
    public float agentLaneHeight = 0f;

    private Graph graph;

    public List<LaneGeometry> lanes;
    public List<AgentLaneData> agentLanes;
    private Paths64 polylines;
    public List<RoadPolygon> roadPolygons;
    private Paths64 footprint;


    void Start()
    {
        Generate();
    }

    public void Generate()
    {
        graph = new Graph();

        System.Random rand = new System.Random(12345); // 固定种子（方便调试）

        // Generate major roads 
        MajorGenerator majorGen = new MajorGenerator(
            rand,
            mapSize,
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
            minorRoadCount,
            deletionProbability,
            graph,
            majorSegments
        );

        minorGen.Run();

        RoadNetworkData data = GraphConverter.Convert(graph);// Convert the graph to RoadData

        Debug.Log("Road Network Generated!");

        // Generate Lanes from the graph's edges
        lanes = LaneGenerator.GenerateLanes(graph);
        agentLanes = AgentLaneExporter.Export(lanes, agentLaneHeight);
        Debug.Log("Lane count: " + lanes.Count);
        Debug.Log("Agent lane count: " + agentLanes.Count);


        // Footprint
        var builder = new PolylineBuilder(graph);
        polylines = builder.Build();

        //Generate road footprints
        //roadPolygons = RoadFootprintGenerator.Generate(graph, 2f);//改参的入口
        footprint = RoadFootprintGenerator.Generate(polylines, roadWidth);

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