using System.Collections.Generic;
using Clipper2Lib;
using UnityEngine;
using UnityEngine.Rendering;

public static class BuildingMeshBuilder
{
    private const float ClipperScale = 1000f;

    public static bool TryBuildExtrudedMesh(Path64 footprint, float baseHeight, float topHeight, out Mesh mesh, out TriangulateResult result)
    {
        mesh = null;
        result = TriangulateResult.noPolygons;

        if (footprint == null || footprint.Count < 3)
            return false;

        Paths64 paths = new Paths64 { footprint };
        result = Clipper.Triangulate(paths, out Paths64 triangles);
        if (result != TriangulateResult.success || triangles == null || triangles.Count == 0)
            return false;

        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<Vector3> normals = new List<Vector3>();
        List<int> indices = new List<int>();
        Dictionary<Point64, int> topPointToIndex = new Dictionary<Point64, int>();
        Dictionary<Point64, int> bottomPointToIndex = new Dictionary<Point64, int>();

        for (int i = 0; i < triangles.Count; i++)
        {
            Path64 triangle = triangles[i];
            if (triangle == null || triangle.Count < 3)
                continue;

            int aTop = GetOrAddVertex(triangle[0], topHeight, Vector3.up, vertices, uvs, normals, topPointToIndex);
            int bTop = GetOrAddVertex(triangle[1], topHeight, Vector3.up, vertices, uvs, normals, topPointToIndex);
            int cTop = GetOrAddVertex(triangle[2], topHeight, Vector3.up, vertices, uvs, normals, topPointToIndex);
            AddTriangleFacingUp(vertices, indices, aTop, bTop, cTop);

            int aBottom = GetOrAddVertex(triangle[0], baseHeight, Vector3.down, vertices, uvs, normals, bottomPointToIndex);
            int bBottom = GetOrAddVertex(triangle[1], baseHeight, Vector3.down, vertices, uvs, normals, bottomPointToIndex);
            int cBottom = GetOrAddVertex(triangle[2], baseHeight, Vector3.down, vertices, uvs, normals, bottomPointToIndex);
            AddTriangleFacingDown(vertices, indices, aBottom, bBottom, cBottom);
        }

        for (int i = 0; i < footprint.Count; i++)
        {
            Point64 current = footprint[i];
            Point64 next = footprint[(i + 1) % footprint.Count];

            AddSideQuad(current, next, baseHeight, topHeight, vertices, uvs, normals, indices);
        }

        if (vertices.Count == 0 || indices.Count == 0)
            return false;

        mesh = new Mesh();
        mesh.name = "Building Mesh";
        if (vertices.Count > 65535)
            mesh.indexFormat = IndexFormat.UInt32;

        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetNormals(normals);
        mesh.SetTriangles(indices, 0);
        mesh.RecalculateBounds();
        return true;
    }

    private static int GetOrAddVertex(
        Point64 point,
        float height,
        Vector3 normal,
        List<Vector3> vertices,
        List<Vector2> uvs,
        List<Vector3> normals,
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
        normals.Add(normal);
        return index;
    }

    private static int AddVertex(Point64 point, float height, Vector3 normal, List<Vector3> vertices, List<Vector2> uvs, List<Vector3> normals)
    {
        float x = point.X / ClipperScale;
        float y = point.Y / ClipperScale;
        int index = vertices.Count;
        vertices.Add(RoadCoordinateUtility.PlanarToWorld(x, y, height));
        uvs.Add(new Vector2(x, y));
        normals.Add(normal);
        return index;
    }

    private static void AddSideQuad(
        Point64 current,
        Point64 next,
        float baseHeight,
        float topHeight,
        List<Vector3> vertices,
        List<Vector2> uvs,
        List<Vector3> normals,
        List<int> indices)
    {
        Vector3 currentPlanar = RoadCoordinateUtility.PlanarToWorld(current.X / ClipperScale, current.Y / ClipperScale, 0f);
        Vector3 nextPlanar = RoadCoordinateUtility.PlanarToWorld(next.X / ClipperScale, next.Y / ClipperScale, 0f);
        Vector3 edgeDirection = (nextPlanar - currentPlanar).normalized;
        Vector3 sideNormal = Vector3.Cross(edgeDirection, Vector3.up).normalized;

        int currentBottom = AddVertex(current, baseHeight, sideNormal, vertices, uvs, normals);
        int currentTop = AddVertex(current, topHeight, sideNormal, vertices, uvs, normals);
        int nextTop = AddVertex(next, topHeight, sideNormal, vertices, uvs, normals);
        int nextBottom = AddVertex(next, baseHeight, sideNormal, vertices, uvs, normals);

        indices.Add(currentBottom);
        indices.Add(currentTop);
        indices.Add(nextTop);

        indices.Add(currentBottom);
        indices.Add(nextTop);
        indices.Add(nextBottom);
    }

    private static void AddTriangleFacingUp(List<Vector3> vertices, List<int> indices, int a, int b, int c)
    {
        Vector3 normal = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
        if (normal.y < 0f)
        {
            indices.Add(a);
            indices.Add(c);
            indices.Add(b);
            return;
        }

        indices.Add(a);
        indices.Add(b);
        indices.Add(c);
    }

    private static void AddTriangleFacingDown(List<Vector3> vertices, List<int> indices, int a, int b, int c)
    {
        Vector3 normal = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
        if (normal.y > 0f)
        {
            indices.Add(a);
            indices.Add(c);
            indices.Add(b);
            return;
        }

        indices.Add(a);
        indices.Add(b);
        indices.Add(c);
    }
}
