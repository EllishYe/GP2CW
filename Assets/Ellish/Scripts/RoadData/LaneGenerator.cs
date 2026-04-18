using System.Collections.Generic;
using UnityEngine;
using GraphModel;

/// <summary>
/// Generate Lane Geometries from the graph's edges'
/// Each edge will generate two lanes(one for each direction)
/// </summary>

public static class LaneGenerator
{
    public static List<LaneGeometry> GenerateLanes(
        Graph graph,
        float cutDistance = 0.6f,// Junction Radius + safety margin（原来是8）
        float laneOffset = 0.3f// Lane width/2（原来是4）
        )
    {
        List<LaneGeometry> lanes = new List<LaneGeometry>();

        // Handle both major and minor egdes together
        List<Edge> allEdges = new List<Edge>();
        allEdges.AddRange(graph.MajorEdges);
        allEdges.AddRange(graph.MinorEdges);

        foreach (var edge in allEdges)
        {
            Vector2 A = new Vector2(edge.NodeA.X, edge.NodeA.Y);
            Vector2 B = new Vector2(edge.NodeB.X, edge.NodeB.Y);

            Vector2 dir = (B - A).normalized;
            Vector2 normal = new Vector2(-dir.y, dir.x);

            float length = Vector2.Distance(A, B);

            // jump short edges, as they may not have enough space for lanes after cutting 暂时去掉
            //if (length < cutDistance * 2f) 
            //    continue;

            // ===== cut =====
            Vector2 start = A + dir * cutDistance;
            Vector2 end = B - dir * cutDistance;

            // ===== Left Lane（A → B）=====
            Vector2 leftStart = start + normal * laneOffset;
            Vector2 leftEnd = end + normal * laneOffset;

            lanes.Add(new LaneGeometry(leftStart, leftEnd));

            // ===== Right Lane（B → A）=====
            Vector2 rightStart = end - normal * laneOffset;
            Vector2 rightEnd = start - normal * laneOffset;

            lanes.Add(new LaneGeometry(rightStart, rightEnd));
        }

        return lanes;
    }

}
