using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RoadNetworkData
{
    public List<JunctionData> nodes = new List<JunctionData>();
    public List<RoadEdgeData> edges = new List<RoadEdgeData>();
}

public static class RoadCoordinateUtility
{
    public static Vector3 PlanarToWorld(Vector2 planarPosition, float height = 0f)
    {
        return new Vector3(planarPosition.x, height, planarPosition.y);
    }

    public static Vector3 PlanarToWorld(float x, float y, float height = 0f)
    {
        return new Vector3(x, height, y);
    }

    public static Vector2 WorldToPlanar(Vector3 worldPosition)
    {
        return new Vector2(worldPosition.x, worldPosition.z);
    }
}
