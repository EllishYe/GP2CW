using UnityEngine;
using System.Collections.Generic;

public enum RoadKind
{
    Major,
    Minor
}

/// <summary>
/// LaneGeometry contains geometric info for a lane,such as its start and end positions.
/// </summary>

[System.Serializable]
public class LaneGeometry
{
    public Vector2 start;
    public Vector2 end;
    public RoadKind roadKind;
    public int laneIndexFromCenter;

    public LaneGeometry(Vector2 s, Vector2 e) : this(s, e, RoadKind.Minor, 0)
    {
    }

    public LaneGeometry(Vector2 s, Vector2 e, RoadKind kind, int laneIndex)
    {
        start = s;
        end = e;
        roadKind = kind;
        laneIndexFromCenter = laneIndex;
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
    public Vector3 vehicleStartPoint;
    public Vector3 vehicleStopPoint;
    public int oppositeLaneId;

    public RoadKind roadKind;
    public int laneIndexFromCenter;
    public Vector2 planarStart;
    public Vector2 planarEnd;
    public Vector2 planarVehicleStart;
    public Vector2 planarVehicleStop;

    public AgentLaneData(int id, LaneGeometry lane, int oppositeLaneId, float height = 0f)
    {
        this.id = id;
        planarStart = lane.start;
        planarEnd = lane.end;
        this.oppositeLaneId = oppositeLaneId;
        roadKind = lane.roadKind;
        laneIndexFromCenter = lane.laneIndexFromCenter;
        startPoint = RoadCoordinateUtility.PlanarToWorld(planarStart, height);
        endPoint = RoadCoordinateUtility.PlanarToWorld(planarEnd, height);
        SetVehicleControlPoints(planarStart, planarEnd, height);
    }

    public void SetVehicleControlPoints(Vector2 vehicleStart, Vector2 vehicleStop, float height = 0f)
    {
        planarVehicleStart = vehicleStart;
        planarVehicleStop = vehicleStop;
        vehicleStartPoint = RoadCoordinateUtility.PlanarToWorld(planarVehicleStart, height);
        vehicleStopPoint = RoadCoordinateUtility.PlanarToWorld(planarVehicleStop, height);
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
            exported.Add(new AgentLaneData(i, lane, oppositeLaneId, height));
        }

        return exported;
    }

    private static int GetOppositeLaneId(int laneId, int laneCount)
    {
        int oppositeLaneId = laneId % 2 == 0 ? laneId + 1 : laneId - 1;
        return oppositeLaneId >= 0 && oppositeLaneId < laneCount ? oppositeLaneId : -1;
    }
}
