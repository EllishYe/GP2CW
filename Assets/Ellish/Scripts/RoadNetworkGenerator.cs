using UnityEngine;
using GraphModel;
using RoadGeneration;
using System;

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

        // Genera
        MinorGenerator minorGen = new MinorGenerator(
            rand,
            mapSize,
            minorRoadCount,
            deletionProbability,
            graph,
            majorSegments
        );

        minorGen.Run();

        Debug.Log("Road Network Generated!");
    }

    internal Graph GetGraph()
    {
        return graph;
    }
}