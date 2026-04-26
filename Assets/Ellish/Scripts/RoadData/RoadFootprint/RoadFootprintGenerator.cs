using Clipper2Lib;
using GraphModel;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


/// <summary>
/// Footprint Generator:从Polyline（道路骨架路径）生成Footprint（道路边界）
/// </summary>

public static class RoadFootprintGenerator
{

    public static Paths64 Generate(Paths64 input, float roadWidth)
    {
        ClipperOffset offset = new ClipperOffset();

        offset.AddPaths(
            input,
            JoinType.Round,   // 
            EndType.Butt      // 
        );

        Paths64 solution = new Paths64();

        //offset.Execute(roadWidth * 0.5, solution);
        offset.Execute(roadWidth * 500.0, solution);


        return solution;
    }
}

public static class RoadMeshBuilder
{
    private const float ClipperScale = 1000f;

    public static bool TryBuildRoadMesh(Paths64 footprint, float height, out Mesh mesh, out TriangulateResult result)
    {
        mesh = null;
        result = TriangulateResult.noPolygons;

        if (footprint == null || footprint.Count == 0)
            return false;

        result = Clipper.Triangulate(footprint, out Paths64 triangles);
        if (result != TriangulateResult.success || triangles == null || triangles.Count == 0)
            return false;

        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> indices = new List<int>();
        Dictionary<Point64, int> pointToIndex = new Dictionary<Point64, int>();

        foreach (Path64 triangle in triangles)
        {
            if (triangle == null || triangle.Count < 3)
                continue;

            int a = GetOrAddVertex(triangle[0], height, vertices, uvs, pointToIndex);
            int b = GetOrAddVertex(triangle[1], height, vertices, uvs, pointToIndex);
            int c = GetOrAddVertex(triangle[2], height, vertices, uvs, pointToIndex);

            Vector3 normal = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
            if (normal.y < 0f)
            {
                indices.Add(a);
                indices.Add(c);
                indices.Add(b);
            }
            else
            {
                indices.Add(a);
                indices.Add(b);
                indices.Add(c);
            }
        }

        if (vertices.Count == 0 || indices.Count == 0)
            return false;

        mesh = new Mesh();
        mesh.name = "Road Surface Mesh";
        if (vertices.Count > 65535)
            mesh.indexFormat = IndexFormat.UInt32;

        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(indices, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return true;
    }

    private static int GetOrAddVertex(
        Point64 point,
        float height,
        List<Vector3> vertices,
        List<Vector2> uvs,
        Dictionary<Point64, int> pointToIndex)
    {
        if (pointToIndex.TryGetValue(point, out int index))
            return index;

        float x = point.X / ClipperScale;
        float y = point.Y / ClipperScale;
        Vector3 world = RoadCoordinateUtility.PlanarToWorld(x, y, height);

        index = vertices.Count;
        pointToIndex.Add(point, index);
        vertices.Add(world);
        uvs.Add(new Vector2(x, y));
        return index;
    }
}
