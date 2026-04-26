using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// LaneGeometry contains geometric info for a lane,such as its start and end positions.
/// </summary>

[System.Serializable]
public class LaneGeometry
{
    public Vector2 start;
    public Vector2 end;

    public LaneGeometry(Vector2 s, Vector2 e)
    {
        start = s;
        end = e;
    }
}

/// <summary>
/// World-space lane data exported for agent systems.
/// </summary>
[System.Serializable]
public class AgentLaneData
{
    public int id;
    public Vector3 startPoint;
    public Vector3 endPoint;
    public int oppositeLaneId;

    public Vector2 planarStart;
    public Vector2 planarEnd;

    public AgentLaneData(int id, Vector2 planarStart, Vector2 planarEnd, int oppositeLaneId, float height = 0f)
    {
        this.id = id;
        this.planarStart = planarStart;
        this.planarEnd = planarEnd;
        this.oppositeLaneId = oppositeLaneId;
        startPoint = RoadCoordinateUtility.PlanarToWorld(planarStart, height);
        endPoint = RoadCoordinateUtility.PlanarToWorld(planarEnd, height);
    }
}

public static class AgentLaneExporter
{
    public static List<AgentLaneData> Export(List<LaneGeometry> sourceLanes, float height = 0f)
    {
        List<AgentLaneData> exported = new List<AgentLaneData>();
        if (sourceLanes == null) return exported;

        for (int i = 0; i < sourceLanes.Count; i++)
        {
            LaneGeometry lane = sourceLanes[i];
            int oppositeLaneId = GetOppositeLaneId(i, sourceLanes.Count);
            exported.Add(new AgentLaneData(i, lane.start, lane.end, oppositeLaneId, height));
        }

        return exported;
    }

    private static int GetOppositeLaneId(int laneId, int laneCount)
    {
        int oppositeLaneId = laneId % 2 == 0 ? laneId + 1 : laneId - 1;
        return oppositeLaneId >= 0 && oppositeLaneId < laneCount ? oppositeLaneId : -1;
    }
}
