using Clipper2Lib;
using UnityEngine;

[System.Serializable]
public class LotData
{
    public int id;
    public int sourceBlockId;
    public string sourceBlockStableKey;
    public UrbanBlockType urbanBlockType;
    public Path64 polygon;
    public Path64 buildingFootprint;
    public double area;
    public Vector2 planarCenter;
    public Vector3 center;
    public float buildingHeight;
    public GameObject meshObject;
}
