using Clipper2Lib;
using GraphModel;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public static class RoadFootprintGenerator
{
    public static List<RoadPolygon> Generate(GraphModel.Graph graph, float roadWidth = 0.6f)//此处的roadWith需要和LaneGenerator中的laneOffser保持一致（roadWith=laneOffset*2f）
    {
        List<PathD> subject = new List<PathD>();

        // ===== 收集所有 Edge =====
        List<GraphModel.Edge> edges = new List<GraphModel.Edge>();
        edges.AddRange(graph.MajorEdges);
        edges.AddRange(graph.MinorEdges);

        foreach (var edge in edges)
        {
            Vector2 A = new Vector2(edge.NodeA.X, edge.NodeA.Y);
            Vector2 B = new Vector2(edge.NodeB.X, edge.NodeB.Y);

            float length = Vector2.Distance(A, B);
            if (length < 0.01f) continue;

            Vector2 dir = (B - A).normalized;
            Vector2 normal = new Vector2(-dir.y, dir.x);

            float halfWidth = roadWidth * 0.5f;

            // ===== 构造矩形（顺时针）=====
            Vector2 p1 = A + normal * halfWidth;
            Vector2 p2 = B + normal * halfWidth;
            Vector2 p3 = B - normal * halfWidth;
            Vector2 p4 = A - normal * halfWidth;

            PathD rect = new PathD
            {
                new PointD(p1.x, p1.y),
                new PointD(p2.x, p2.y),
                new PointD(p3.x, p3.y),
                new PointD(p4.x, p4.y)
            };

            subject.Add(rect);
        }

        // ===== Clipper2 Union =====
        PathsD solution = Clipper.Union(new PathsD(subject), FillRule.NonZero);

        // ===== 转回 Unity 数据 =====
        List<RoadPolygon> result = new List<RoadPolygon>();

        foreach (var path in solution)
        {
            RoadPolygon poly = new RoadPolygon();

            foreach (var p in path)
            {
                poly.points.Add(new Vector2((float)p.x, (float)p.y));
            }

            result.Add(poly);
        }

        return result;
    }
}
