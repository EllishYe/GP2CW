using Clipper2Lib;
using UnityEngine;

public enum BlockLandUse
{
    Building,
    Park
}

public enum BlockLandUseOverride
{
    Auto,
    Building,
    Park
}

[System.Serializable]
public class BlockData
{
    public int id;
    public string stableKey;
    public BlockLandUse landUse;
    public Path64 polygon;
    public double area;
    public Vector2 planarCenter;
    public Vector3 center;
    public GameObject meshObject;

    public bool IsPark => landUse == BlockLandUse.Park;
    public bool IsBuilding => landUse == BlockLandUse.Building;
}
