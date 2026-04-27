using System.Collections.Generic;
using UnityEngine;

public class LaneData : MonoBehaviour
{
    public List<Transform> pathPoints = new List<Transform>();  
    public List<LaneData> nextLanes = new List<LaneData>();

    public bool isRedLight = false;
    public Transform stopLinePoint;
}
