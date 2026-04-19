using System.Collections.Generic;
using UnityEngine;
using Clipper2Lib;

public class RoadFootprintVisualizer : MonoBehaviour
{
    public RoadNetworkGenerator generator;
    public List<RoadPolygon> polygons;

    // 缓存来自 generator 的引用（用于检测变化）
    private Paths64 polylinesRef;
    private Paths64 footprintRef;

    // 简单的线段结构用于缓存转换结果（只转换一次）
    public struct LineSegment { public Vector3 a, b; public LineSegment(Vector3 a, Vector3 b) { this.a = a; this.b = b; } }

    public List<LineSegment> polylineSegments = new List<LineSegment>();
    public List<LineSegment> footprintSegments = new List<LineSegment>();

    void Update()
    {
        // 同步 polygons（只在发生变化时打印一次）
        if (generator != null && polygons != generator.roadPolygons)
        {
            polygons = generator.roadPolygons;
            Debug.Log($"LaneVisualizer: synchronized lanes from generator. lanes.Count = {polygons?.Count ?? 0}");
        }

        // 在运行时（Play）检测并按需构建缓存
        CheckAndBuildSegments();
    }

    void OnDrawGizmos()
    {
        // 在编辑器也要检测（以支持 Edit mode 下可视化）
        CheckAndBuildSegments();

        // 如果既没有多边形也没有缓存线段，就直接返回
        if (polygons == null && polylineSegments.Count == 0 && footprintSegments.Count == 0) return;

        // 绘制原有 roadPolygons（红色）
        Gizmos.color = Color.red;
        if (polygons != null)
        {
            foreach (var poly in polygons)
            {
                if (poly == null || poly.points == null || poly.points.Count < 2) continue;
                for (int i = 0; i < poly.points.Count; i++)
                {
                    Vector3 a = poly.points[i];
                    Vector3 b = poly.points[(i + 1) % poly.points.Count];
                    Gizmos.DrawLine(a, b);
                }
            }
        }

        // 使用缓存绘制 Polyline（黄色）和 Footprint（绿色）
        PolylineDebug.DrawSegments(polylineSegments, Color.yellow);
        FootprintDebug.DrawSegments(footprintSegments, Color.green);
    }

    // 如果 generator 的 paths 变了就重建缓存（转换为 Vector3 线段）
    private void CheckAndBuildSegments()
    {
        if (generator == null) return;

        var gp = generator.Polylines;
        if (gp != polylinesRef)
        {
            polylinesRef = gp;
            RebuildPolylineSegments();
        }

        var gf = generator.FootprintPaths;
        if (gf != footprintRef)
        {
            footprintRef = gf;
            RebuildFootprintSegments();
        }
    }

    private void RebuildPolylineSegments()
    {
        polylineSegments.Clear();
        if (polylinesRef == null) return;

        foreach (var path in polylinesRef)
        {
            if (path == null || path.Count < 2) continue;
            for (int i = 0; i < path.Count - 1; i++)
            {
                var a = Point64ToVector3(path[i]);
                var b = Point64ToVector3(path[i + 1]);
                polylineSegments.Add(new LineSegment(a, b));
            }
        }

        Debug.Log($"PolylineDebug: built segments = {polylineSegments.Count}");
    }

    private void RebuildFootprintSegments()
    {
        footprintSegments.Clear();
        if (footprintRef == null) return;

        foreach (var path in footprintRef)
        {
            if (path == null || path.Count < 2) continue;
            for (int i = 0; i < path.Count; i++)
            {
                var a = Point64ToVector3(path[i]);
                var b = Point64ToVector3(path[(i + 1) % path.Count]);
                footprintSegments.Add(new LineSegment(a, b));
            }
        }

        Debug.Log($"FootprintDebug: built segments = {footprintSegments.Count}");
    }

    private static Vector3 Point64ToVector3(Point64 p)
    {
        // 与 PolylineBuilder 中的 ToPoint64 对应的反向转换
        return new Vector3(p.X / 1000f,  p.Y / 1000f, 0f);
    }

    #region Polyline Debug
    public static class PolylineDebug
    {
        // 仅绘制已经缓存好的 Vector3 线段（不再每帧转换或打印）
        public static void DrawSegments(List<LineSegment> segments, Color color)
        {
            if (segments == null || segments.Count == 0) return;
            Color prev = Gizmos.color;
            Gizmos.color = color;
            foreach (var s in segments) Gizmos.DrawLine(s.a, s.b);
            Gizmos.color = prev;
        }
    }
    #endregion

    #region Footprint Debug
    public static class FootprintDebug
    {
        public static void DrawSegments(List<LineSegment> segments, Color color)
        {
            if (segments == null || segments.Count == 0) return;
            Color prev = Gizmos.color;
            Gizmos.color = color;
            foreach (var s in segments) Gizmos.DrawLine(s.a, s.b);
            Gizmos.color = prev;
        }
    }
    #endregion
}
