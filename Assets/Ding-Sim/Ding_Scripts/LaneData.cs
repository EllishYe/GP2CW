using System.Collections.Generic;
using UnityEngine;

public class LaneData : MonoBehaviour
{
    //public int laneId;      // 车道 ID
    //public List<int> nextLaneIds = new();   // 这条路的尽头可以接到哪些路（用车道 ID 表示）


    public List<Transform> pathPoints = new List<Transform>();   // 这条路的路径点列表，车辆会沿着这些点行驶
    public List<LaneData> nextLanes = new List<LaneData>();
    
    
    public bool hasStopLine;
    public Vector3 stopLinePos;
    public bool hasTrafficLight;
    public int trafficLightGroupId = -1;

    public int parentPieceId = -1;
    public bool isIntersectionLane;

    public bool isRedLight = false;
}
