using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class JunctionData
{
    public int id;
    public Vector2 position;
    public List<int> connectedEdges = new List<int>();
}
