using Clipper2Lib;
using GraphModel;
using System.Collections.Generic;
using UnityEngine;

public static class RoadFootprintGenerator
{
    //public static List<RoadPolygon> Generate(GraphModel.Graph graph, float roadWidth = 0.6f)
    //    //此处的roadWith需要和LaneGenerator中的laneOffser保持一致（roadWith=laneOffset*2f）
    //{

    //    //List<Paths64> input = new List<Paths64>();
    //    Paths64 input = new Paths64();

    //    // ===== Collect Edges =====
    //    List<GraphModel.Edge> edges = new List<GraphModel.Edge>();
    //    edges.AddRange(graph.MajorEdges);
    //    edges.AddRange(graph.MinorEdges);

    //    foreach (var edge in edges)
    //    {
    //        Vector2 A = new Vector2(edge.NodeA.X, edge.NodeA.Y);
    //        Vector2 B = new Vector2(edge.NodeB.X, edge.NodeB.Y);

    //        if (Vector2.Distance(A, B) < 0.01f) continue;

    //        Path64 path = new Path64
    //        {
    //        new Point64((long)(A.x * 1000), (long)(A.y * 1000)),
    //        new Point64((long)(B.x * 1000), (long)(B.y * 1000))
    //        };

    //        input.Add(path);
    //    }



    //    //=====Clipper2 Offset=====
    //    ClipperOffset offset = new ClipperOffset();

    //    offset.AddPaths(
    //        input,
    //        JoinType.Round,   // JointPoint (for Polylines)
    //        EndType.Butt  // EndPoint
    //    );

    //    Paths64 expanded = new Paths64();
    //    offset.Execute(roadWidth * 500, expanded);

    //    // ===== Clipper2 Union =====
    //    Paths64 solution = Clipper.Union(expanded, FillRule.NonZero);

    //    // ===== Result Conversion =====
    //    List<RoadPolygon> result = new List<RoadPolygon>();

    //    foreach (var path in solution)
    //    {
    //        RoadPolygon poly = new RoadPolygon();

    //        foreach (var p in path)
    //        {
    //            poly.points.Add(new Vector2((float)p.X / 1000f, (float)p.Y / 1000f));
    //        }

    //        result.Add(poly);
    //    }

    //    return result;
    //}
    public static Paths64 Generate(Paths64 input, float roadWidth)
    {
        ClipperOffset offset = new ClipperOffset();

        offset.AddPaths(
            input,
            JoinType.Round,   // 推荐
            EndType.Butt      // 开放线段
        );

        Paths64 solution = new Paths64();

        //offset.Execute(roadWidth * 0.5, solution);
        offset.Execute(roadWidth * 500.0, solution);


        return solution;
    }
}
