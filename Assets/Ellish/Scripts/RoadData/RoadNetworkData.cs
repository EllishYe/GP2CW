using System.Collections.Generic;

[System.Serializable]
public class RoadNetworkData
{
    public List<JunctionData> nodes = new List<JunctionData>();
    public List<RoadEdgeData> edges = new List<RoadEdgeData>();
}