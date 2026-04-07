using System.Collections.Generic;
using UnityEngine;

public class LaneData : MonoBehaviour
{
    //public int laneId;      // 车道 ID
    //public List<int> nextLaneIds = new();   // 这条路的尽头可以接到哪些路（用车道 ID 表示）


    public List<Transform> pathPoints = new List<Transform>();   // 这条路的路径点列表，车辆会沿着这些点行驶
    public List<LaneData> nextLanes = new List<LaneData>();

    public bool isRedLight = false;
    public Transform stopLinePoint;

    [Header("红绿灯模型绑定 (视觉层)")]
    public MeshRenderer trafficLightRenderer; // 拖入你那个 Sphere 的 MeshRenderer
    public Material redMaterial;              // 拖入你调好的红色发光材质
    public Material greenMaterial;            // 拖入你调好的绿色发光材质

    // 交警专用的“变色开关”
    public void SetLightStatus(bool isRed)
    {
        isRedLight = isRed;

        // 防呆设计：确保队友在 Prefab 里拖拽了这些槽位
        if (trafficLightRenderer != null && redMaterial != null && greenMaterial != null)
        {
            if (isRedLight)
            {
                // 变红灯：给球换上红色材质
                trafficLightRenderer.material = redMaterial;
            }
            else
            {
                // 变绿灯：给球换上绿色材质
                trafficLightRenderer.material = greenMaterial;
            }
        }
    }
}
