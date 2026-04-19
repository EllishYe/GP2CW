using System.Collections.Generic;
using UnityEngine;

public class RoadFootprintVisualizer : MonoBehaviour
{
    public RoadNetworkGenerator generator;
    public List<RoadPolygon> polygons;

    void Update()
    {
        // 在运行时保证实时同步（generator 可能在 Start 之后填充 lanes）此段代码稍有问题
        if (generator != null && polygons != generator.roadPolygons)
        {
            polygons = generator.roadPolygons;
            Debug.Log($"LaneVisualizer: synchronized lanes from generator. lanes.Count = {polygons?.Count ?? 0}");
        }
    }

    void OnDrawGizmos()
    {
        if (polygons == null) return;

        Gizmos.color = Color.red;

        foreach (var poly in polygons)
        {
            for (int i = 0; i < poly.points.Count; i++)
            {
                Vector3 a = poly.points[i];
                Vector3 b = poly.points[(i + 1) % poly.points.Count];

                Gizmos.DrawLine(a, b);
            }
        }
    }
}
