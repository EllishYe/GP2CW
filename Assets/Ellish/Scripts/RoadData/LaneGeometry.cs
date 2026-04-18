using UnityEngine;

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