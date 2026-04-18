using UnityEngine;
using System.Collections.Generic;

public class LaneVisualizer : MonoBehaviour
{
    public RoadNetworkGenerator generator;

    public List<LaneGeometry> lanes;

    public Color colorForward = Color.green;
    public Color colorBackward = Color.blue;


    void Update()
    {
        // 在运行时保证实时同步（generator 可能在 Start 之后填充 lanes）此段代码稍有问题
        if (generator != null && lanes != generator.lanes)
        {
            lanes = generator.lanes;
            Debug.Log($"LaneVisualizer: synchronized lanes from generator. lanes.Count = {lanes?.Count ?? 0}");
        }
    }

    void OnDrawGizmos()
    {
        if (lanes == null) return;

        foreach (var lane in lanes)
        {
            Vector3 start = new Vector3(lane.start.x, lane.start.y, 0);
            Vector3 end = new Vector3(lane.end.x, lane.end.y, 0);

            // 画线
            Gizmos.color = Color.white;
            Gizmos.DrawLine(start, end);

            // 画方向箭头
            Vector3 dir = (end - start).normalized;
            Vector3 mid = (start + end) * 0.5f;

            float arrowSize = 1.0f;

            Vector3 right = Quaternion.Euler(0, 0, 30) * -dir;
            Vector3 left = Quaternion.Euler(0, 0, -30) * -dir;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(mid, mid + right * arrowSize);
            Gizmos.DrawLine(mid, mid + left * arrowSize);
        }
        //if (lanes == null) return;

        //// 假定 LaneGenerator 为每个 edge 生成一对 lane（A->B，B->A），因此按索引交替着色
        //for (int i = 0; i < lanes.Count; i++)
        //{
        //    var lane = lanes[i];
        //    Vector3 start = new Vector3(lane.start.x, lane.start.y, 0);
        //    Vector3 end = new Vector3(lane.end.x, lane.end.y, 0);

        //    // 画线（固定白色）
        //    Gizmos.color = Color.white;
        //    Gizmos.DrawLine(start, end);

        //    // 箭头方向：沿着 lane 的 start -> end
        //    Vector3 dir = (end - start).normalized;
        //    Vector3 mid = (start + end) * 0.5f;
        //    float arrowSize = 1.0f;

        //    // 交替使用 forward/backward 颜色（因为每条边生成两条车道）
        //    Gizmos.color = (i % 2 == 0) ? colorForward : colorBackward;

        //    Vector3 right = Quaternion.Euler(0, 0, 30) * -dir;
        //    Vector3 left = Quaternion.Euler(0, 0, -30) * -dir;

        //    Gizmos.DrawLine(mid, mid + right * arrowSize);
        //    Gizmos.DrawLine(mid, mid + left * arrowSize);
        //}
    }
}
