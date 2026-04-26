using System.Collections.Generic;
using UnityEngine;
using GraphModel;

/// <summary>
/// Generate lane geometries from graph edges. Each road can have multiple lanes per direction.
/// </summary>
public static class LaneGenerator
{
    public static List<LaneGeometry> GenerateLanes(
        Graph graph,
        float cutDistance = 8f,
        float laneWidth = 3.5f,
        int majorLanesPerDirection = 2,
        int minorLanesPerDirection = 1,
        bool skipShortEdges = true
        )
    {
        List<LaneGeometry> lanes = new List<LaneGeometry>();
        if (graph == null) return lanes;

        AddLanesForEdges(lanes, graph.MajorEdges, RoadKind.Major, majorLanesPerDirection, laneWidth, cutDistance, skipShortEdges);
        AddLanesForEdges(lanes, graph.MinorEdges, RoadKind.Minor, minorLanesPerDirection, laneWidth, cutDistance, skipShortEdges);
        return lanes;
    }

    private static void AddLanesForEdges(
        List<LaneGeometry> lanes,
        List<Edge> edges,
        RoadKind roadKind,
        int lanesPerDirection,
        float laneWidth,
        float cutDistance,
        bool skipShortEdges)
    {
        if (edges == null) return;

        lanesPerDirection = Mathf.Max(1, lanesPerDirection);
        laneWidth = Mathf.Max(0.1f, laneWidth);
        cutDistance = Mathf.Max(0f, cutDistance);

        foreach (var edge in edges)
        {
            Vector2 a = new Vector2(edge.NodeA.X, edge.NodeA.Y);
            Vector2 b = new Vector2(edge.NodeB.X, edge.NodeB.Y);
            Vector2 dir = (b - a).normalized;
            Vector2 normal = new Vector2(-dir.y, dir.x);
            float length = Vector2.Distance(a, b);

            if (skipShortEdges && length < cutDistance * 2f)
                continue;

            Vector2 start = a + dir * cutDistance;
            Vector2 end = b - dir * cutDistance;

            for (int laneIndex = 0; laneIndex < lanesPerDirection; laneIndex++)
            {
                float offset = laneWidth * (laneIndex + 0.5f);

                Vector2 forwardStart = start + normal * offset;
                Vector2 forwardEnd = end + normal * offset;
                lanes.Add(new LaneGeometry(forwardStart, forwardEnd, roadKind, laneIndex));

                Vector2 backwardStart = end - normal * offset;
                Vector2 backwardEnd = start - normal * offset;
                lanes.Add(new LaneGeometry(backwardStart, backwardEnd, roadKind, laneIndex));
            }
        }
    }
}
