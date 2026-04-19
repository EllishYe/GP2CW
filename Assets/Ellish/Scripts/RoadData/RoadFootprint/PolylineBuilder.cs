using Clipper2Lib;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Polyline Builder：从Graph生成Polyline（道路骨架路径）
/// </summary>

public class PolylineBuilder
{
    // 数据结构（输出给 RoadFootprint）？
    private GraphModel.Graph graph;
    private HashSet<GraphModel.Edge> visited = new();

    public PolylineBuilder(GraphModel.Graph graph)
    {
        this.graph = graph;
    }


    public Paths64 Build()
    {
        Paths64 result = new Paths64();

        // 先 Major（骨架）
        foreach (var edge in graph.MajorEdges)
        {
            if (visited.Contains(edge)) continue;

            var path = TraceMajor(edge);
            if (path.Count > 1)
                result.Add(path);
        }

        // 再 Minor（填充）
        foreach (var edge in graph.MinorEdges)
        {
            if (visited.Contains(edge)) continue;

            var path = TraceMinor(edge);
            if (path.Count > 1)
                result.Add(path);
        }

        return result;
    }

    

    #region Major Road
    private Path64 TraceMajor(GraphModel.Edge startEdge)
    {
        Path64 path = new Path64();

        GraphModel.Node current = startEdge.NodeA;
        GraphModel.Node prev = null;
        GraphModel.Edge currentEdge = startEdge;

        Vector2 prevDir = Vector2.zero;

        while (currentEdge != null && !visited.Contains(currentEdge))
        {
            visited.Add(currentEdge);

            GraphModel.Node next =
                (currentEdge.NodeA == current)
                ? currentEdge.NodeB
                : currentEdge.NodeA;

            path.Add(ToPoint64(current));

            // 更新方向
            prevDir = new Vector2(
                next.X - current.X,
                next.Y - current.Y
            ).normalized;

            prev = current;
            current = next;

            currentEdge = GetNextMajorEdge(current, prev, prevDir);
        }

        path.Add(ToPoint64(current));
        return path;
    }

    private GraphModel.Edge GetNextMajorEdge(
    GraphModel.Node node,
    GraphModel.Node from,
    Vector2 prevDir)
    {
        GraphModel.Edge bestEdge = null;
        float bestDot = -1f;

        foreach (var edge in node.Edges)
        {
            if (visited.Contains(edge)) continue;

            if (edge.NodeA == from || edge.NodeB == from)
                continue;

            if (!IsMajor(edge)) continue;

            GraphModel.Node next =
                (edge.NodeA == node) ? edge.NodeB : edge.NodeA;

            Vector2 dir = new Vector2(
                next.X - node.X,
                next.Y - node.Y
            ).normalized;

            float dot = Vector2.Dot(prevDir, dir);

            if (dot > bestDot)
            {
                bestDot = dot;
                bestEdge = edge;
            }
        }

        return bestEdge;
    }

    #endregion


    #region Minor Road

    private Path64 TraceMinor(GraphModel.Edge startEdge)
    {
        Path64 path = new Path64();

        GraphModel.Node current = startEdge.NodeA;
        GraphModel.Node prev = null;
        GraphModel.Edge currentEdge = startEdge;

        
        Vector2 prevDir = Vector2.zero;

        while (currentEdge != null && !visited.Contains(currentEdge))
        {
            visited.Add(currentEdge);

            GraphModel.Node next =
                (currentEdge.NodeA == current)
                ? currentEdge.NodeB
                : currentEdge.NodeA;

            path.Add(ToPoint64(current));

            
            Vector2 currDir = new Vector2(
            next.X - current.X,
            next.Y - current.Y
            ).normalized;
            

            prev = current;
            current = next;
            prevDir = currDir;

            currentEdge = GetNextMinorEdgeSmart(current, prev, prevDir);
        }

        path.Add(ToPoint64(current));
        return path;
    }

    private GraphModel.Edge GetNextMinorEdge(
    GraphModel.Node node,
    GraphModel.Node from)
    {
        foreach (var edge in node.Edges)
        {
            if (visited.Contains(edge)) continue;

            if (edge.NodeA == from || edge.NodeB == from)
                continue;

            if (!IsMinor(edge)) continue;

            return edge;
        }

        return null;
    }

    private GraphModel.Edge GetNextMinorEdgeSmart(
    GraphModel.Node node,
    GraphModel.Node from,
    Vector2 prevDir)
    {
        List<GraphModel.Edge> candidates = new List<GraphModel.Edge>();

        foreach (var edge in node.Edges)
        {
            if (visited.Contains(edge)) continue;

            if (edge.NodeA == from || edge.NodeB == from)
                continue;

            if (!IsMinor(edge)) continue;

            candidates.Add(edge);
        }

        // 没有路 → 结束
        if (candidates.Count == 0)
            return null;

        // 只有一条 → 直接走（穿过“假junction”）
        if (candidates.Count == 1)
            return candidates[0];

        // 多条 → 选“最直”的（基于角度连续性）
        GraphModel.Edge bestEdge = null;
        float bestDot = -1f;

        foreach (var edge in candidates)
        {
            GraphModel.Node next =
                (edge.NodeA == node) ? edge.NodeB : edge.NodeA;

            Vector2 dir = new Vector2(
                next.X - node.X,
                next.Y - node.Y
            ).normalized;

            float dot = Vector2.Dot(prevDir, dir);

            if (dot > bestDot)
            {
                bestDot = dot;
                bestEdge = edge;
            }
        }

        // “角度阈值”防止乱转（optional）
        if (bestDot < 0.3f) // 暂设≈ >70度转弯就不继续
            return null;

        return bestEdge;
    }

    #endregion


    private bool IsMajor(GraphModel.Edge edge)
    {
        return graph.MajorEdges.Contains(edge);
    }

    private bool IsMinor(GraphModel.Edge edge)
    {
        return graph.MinorEdges.Contains(edge);
    }


    private Point64 ToPoint64(GraphModel.Node n)
    {
        return new Point64(
            (long)(n.X * 1000),
            (long)(n.Y * 1000)
        );
    }



}
