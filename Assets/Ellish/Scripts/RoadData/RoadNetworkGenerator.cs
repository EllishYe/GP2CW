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

    private Graph graph;

    public List<LaneGeometry> lanes;
    public List<RoadPolygon> roadPolygons;


    void Start()
    {
        Generate();
    }

    void Generate()
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
        Debug.Log("Lane count: " + lanes.Count);

        //Generate road footprints
        roadPolygons = RoadFootprintGenerator.Generate(graph, 1.2f);//改参的入口
    }

    internal Graph GetGraph()
    {
        return graph;
    }
}