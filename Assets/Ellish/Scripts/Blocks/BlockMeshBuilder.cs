using Clipper2Lib;
using UnityEngine;

public static class BlockMeshBuilder
{
    public static bool TryBuildBlockSurfaceMesh(Path64 polygon, float height, out Mesh mesh, out TriangulateResult result)
    {
        Paths64 paths = new Paths64();
        if (polygon != null && polygon.Count >= 3)
            paths.Add(polygon);

        return RoadMeshBuilder.TryBuildRoadMesh(paths, height, out mesh, out result);
    }
}
