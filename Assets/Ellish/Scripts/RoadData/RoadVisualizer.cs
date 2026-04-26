using UnityEngine;
using GraphModel;

public class RoadVisualizer : MonoBehaviour
{
    public RoadNetworkGenerator generator;

    [Header("Major Road Gizmo")]
    public Color majorLineColor = Color.white;
    [Range(0.1f, 10f)]
    public float majorLineThickness = 2f; // pixels. only works in editor mode

    [Header("Minor Road Gizmo")]
    public Color minorLineColor = Color.grey;
    [Range(0.1f, 10f)]
    public float minorLineThickness = 1f;

    [Header("Node Gizmo")]
    public Color nodeColor = Color.red;
    [Range(0.01f, 2f)]
    public float nodeSize = 0.2f;

    void OnValidate()
    {
        majorLineThickness = Mathf.Max(0.01f, majorLineThickness);
        minorLineThickness = Mathf.Max(0.01f, minorLineThickness);
        nodeSize = Mathf.Max(0.001f, nodeSize);
    }

    void DrawNode(Vector2 pos, float size, Color color)
    {
        Vector3 center = RoadCoordinateUtility.PlanarToWorld(pos);
        Debug.DrawLine(center + Vector3.forward * size, center - Vector3.forward * size, color);
        Debug.DrawLine(center + Vector3.left * size, center - Vector3.left * size, color);
    }

    void OnDrawGizmos()
    {
        if (generator == null) return;
        var graph = generator.GetGraph();
        if (graph == null) return;

        // Major edges
#if UNITY_EDITOR
        UnityEditor.Handles.color = majorLineColor;
        foreach (var edge in graph.MajorEdges)
        {
            Vector3 a = RoadCoordinateUtility.PlanarToWorld(edge.NodeA.X, edge.NodeA.Y);
            Vector3 b = RoadCoordinateUtility.PlanarToWorld(edge.NodeB.X, edge.NodeB.Y);
            if (majorLineThickness <= 1f)
                Gizmos.DrawLine(a, b);
            else
                UnityEditor.Handles.DrawAAPolyLine(majorLineThickness, a, b);
        }
#else
        Gizmos.color = majorLineColor;
        foreach (var edge in graph.MajorEdges)
        {
            Vector3 a = RoadCoordinateUtility.PlanarToWorld(edge.NodeA.X, edge.NodeA.Y);
            Vector3 b = RoadCoordinateUtility.PlanarToWorld(edge.NodeB.X, edge.NodeB.Y);
            Gizmos.DrawLine(a, b);
        }
#endif

        // Minor edges
#if UNITY_EDITOR
        UnityEditor.Handles.color = minorLineColor;
        foreach (var edge in graph.MinorEdges)
        {
            Vector3 a = RoadCoordinateUtility.PlanarToWorld(edge.NodeA.X, edge.NodeA.Y);
            Vector3 b = RoadCoordinateUtility.PlanarToWorld(edge.NodeB.X, edge.NodeB.Y);
            if (minorLineThickness <= 1f)
                Gizmos.DrawLine(a, b);
            else
                UnityEditor.Handles.DrawAAPolyLine(minorLineThickness, a, b);
        }
#else
        Gizmos.color = minorLineColor;
        foreach (var edge in graph.MinorEdges)
        {
            Vector3 a = RoadCoordinateUtility.PlanarToWorld(edge.NodeA.X, edge.NodeA.Y);
            Vector3 b = RoadCoordinateUtility.PlanarToWorld(edge.NodeB.X, edge.NodeB.Y);
            Gizmos.DrawLine(a, b);
        }
#endif

        // Nodes£¨use GraphConverter output£©
        Gizmos.color = nodeColor;
        var network = GraphConverter.Convert(graph);
        if (network != null && network.nodes != null)
        {
            foreach (var jd in network.nodes)
            {
                Vector3 pos = RoadCoordinateUtility.PlanarToWorld(jd.position);
                Gizmos.DrawSphere(pos, nodeSize);
            }
        }
    }
}