using UnityEngine;

public class PedestrianPOI : MonoBehaviour
{
    public enum POIType { Home, Office, Mall }

    [Header("SiteType")]
    public POIType type;

    // 可以在这里加一些额外属性，比如最大容纳人数等
}