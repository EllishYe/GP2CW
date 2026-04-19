using Clipper2Lib;
using UnityEngine;
using System.Collections.Generic;

public class PolylineBuilder
{
    // 数据结构（输出给 RoadFootprint）？
    private GraphModel.Graph graph;
    private HashSet<GraphModel.Edge> visited = new();

    public PolylineBuilder(GraphModel.Graph graph)
    {
        this.graph = graph;
    }

    // 核心算法：从每条未访问的边开始，沿着道路追踪直到遇到岔口或者死胡同
    //主入口（生成所有的路径）
    //public Paths64 Build()
    //{
    //    Paths64 result = new Paths64();

    //    foreach (var edge in graph.MajorEdges)//只处理MajorEdges（为什么？）
    //    {
    //        if (visited.Contains(edge)) continue;

    //        Path64 path = TraceEdge(edge);
    //        if (path.Count > 1)
    //            result.Add(path);
    //    }

    //    return result;
    //}

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

    //从一条边开始追踪，直到遇到分叉或死胡同
    //核心：沿 Edge 走出 polyline
    //private PathD TraceFromEdge(GraphModel.Edge startEdge)
    //{
    //    PathD path = new PathD();

    //    GraphModel.Node current = startEdge.NodeA;
    //    GraphModel.Node prev = null;

    //    GraphModel.Edge currentEdge = startEdge;

    //    while (currentEdge != null && !visited.Contains(currentEdge))
    //    {
    //        visited.Add(currentEdge);

    //        GraphModel.Node next =
    //            (currentEdge.NodeA == current)
    //            ? currentEdge.NodeB
    //            : currentEdge.NodeA;

    //        path.Add(new PointD(current.X, current.Y));

    //        prev = current;
    //        current = next;

    //        currentEdge = GetNextEdge(current, prev);
    //    }

    //    path.Add(new PointD(current.X, current.Y));

    //    return path;
    //}

    //private Path64 TraceEdge(GraphModel.Edge startEdge)
    //{
    //    Path64 path = new Path64();

    //    GraphModel.Node current = startEdge.NodeA;
    //    GraphModel.Node prev = null;

    //    GraphModel.Edge currentEdge = startEdge;

    //    while (currentEdge != null && !visited.Contains(currentEdge))
    //    {
    //        visited.Add(currentEdge);

    //        GraphModel.Node next =
    //            (currentEdge.NodeA == current)
    //            ? currentEdge.NodeB
    //            : currentEdge.NodeA;

    //        path.Add(ToPoint64(current));

    //        prev = current;
    //        current = next;

    //        currentEdge = GetNextEdge(current, prev);
    //    }

    //    path.Add(ToPoint64(current));

    //    return path;
    //}


    //获取下一个边：从当前节点触发，找到未访问的边，且不回头
    //junction 分叉逻辑（关键）
    //private GraphModel.Edge GetNextEdge(GraphModel.Node node, GraphModel.Node from)
    //{
    //    foreach (var edge in node.Edges)
    //    {
    //        if (visited.Contains(edge))
    //            continue;

    //        // ❗避免回头
    //        if (edge.NodeA == from || edge.NodeB == from)
    //            continue;

    //        return edge;
    //    }

    //    return null; // dead-end 或 junction stop
    //}

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

            // 只考虑 MajorEdge（你可以用 HashSet 标记）
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

            //// ❗关键：遇到 junction 就停
            //if (current.Edges.Count >= 3)
            //    break;

            //currentEdge = GetNextMinorEdge(current, prev);
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

        // ❌没有路 → 结束
        if (candidates.Count == 0)
            return null;

        // 🔥只有一条 → 直接走（穿过“假junction”）
        if (candidates.Count == 1)
            return candidates[0];

        // 🔥多条 → 选“最直”的（角度连续性）
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

        // 👉可以加一个“角度阈值”防止乱转（可选）
        if (bestDot < 0.3f) // ≈ >70度转弯就不继续
            return null;

        return bestEdge;
    }

    #endregion


    private bool IsMajor(GraphModel.Edge edge)
    {
        // 假设 graph.MajorEdges 是 HashSet<GraphModel.Edge>
        return graph.MajorEdges.Contains(edge);
    }

    private bool IsMinor(GraphModel.Edge edge)
    {
        // 假设 graph.MinorEdges 是 HashSet<GraphModel.Edge>
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
