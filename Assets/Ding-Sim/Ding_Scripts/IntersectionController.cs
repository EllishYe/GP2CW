using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntersectionController : MonoBehaviour
{
    [Header("自动分配的相位车道 (无需手动拖拽)")]
    public List<LaneData> phase1Lanes = new List<LaneData>(); // 例如：南北向
    public List<LaneData> phase2Lanes = new List<LaneData>(); // 例如：东西向

    [Header("红绿灯时长设置 (秒)")]
    public float greenLightTime = 8f; 
    public float clearanceTime = 2f;  

    [Header("自动探测设置")]
    [Tooltip("探测距离12以内的endpoint，找出驶入道路")]
    public float detectionRadius = 12f; 

    // 🚨 核心魔法：让路口自己去抓取周围的车道
    public void AutoDetectIncomingLanes()
    {
        phase1Lanes.Clear();
        phase2Lanes.Clear();

        // 1. 获取全城所有的车道
        LaneData[] allLanes = Object.FindObjectsByType<LaneData>(FindObjectsSortMode.None);

        foreach (LaneData lane in allLanes)
        {
            if (lane.pathPoints == null || lane.pathPoints.Count < 2) continue;

            // 2. 防呆设计：忽略十字路口内部自带的拐弯线
            // 只要这条线的父级是这个路口本身，就说明它是内部线，直接跳过
            if (lane.transform.IsChildOf(this.transform)) continue;

            // 3. 筛选“驶入”本路口的车道
            // 驶入车道的特征：它的【最后一个路点】，一定在路口的中心雷达范围内！
            Vector3 lastPoint = lane.pathPoints[lane.pathPoints.Count - 1].position;
            float distanceToCenter = Vector3.Distance(transform.position, lastPoint);

            if (distanceToCenter <= detectionRadius)
            {
                // 4. 向量数学魔法：判断它是南北向，还是东西向？
                // 算出这条路在末端的朝向 (倒数第二个点 指向 最后一个点)
                Vector3 prevPoint = lane.pathPoints[lane.pathPoints.Count - 2].position;
                Vector3 laneDirection = (lastPoint - prevPoint).normalized;

                // 计算车道朝向 与 路口自身 Z轴(前后) 和 X轴(左右) 的重合度 (点积)
                float dotForward = Mathf.Abs(Vector3.Dot(laneDirection, transform.forward));
                float dotRight = Mathf.Abs(Vector3.Dot(laneDirection, transform.right));

                // 谁的重合度大，就归入哪个相位！
                if (dotForward > dotRight)
                {
                    phase1Lanes.Add(lane); // 贴合 Z 轴方向（南北）
                }
                else
                {
                    phase2Lanes.Add(lane); // 贴合 X 轴方向（东西）
                }
            }
        }
        
        Debug.Log($"{gameObject.name} 自动绑定完成: 相位1({phase1Lanes.Count}条), 相位2({phase2Lanes.Count}条)");
    }

    void Start()
    {
        // 我们不在这里调用 AutoDetect，交由外部统一指挥！
        StartCoroutine(TrafficLightRoutine());
    }

    IEnumerator TrafficLightRoutine()
    {
        while (true) 
        {
            SetLanesRed(phase1Lanes, false); 
            SetLanesRed(phase2Lanes, true);  
            yield return new WaitForSeconds(greenLightTime);

            SetLanesRed(phase1Lanes, true);
            SetLanesRed(phase2Lanes, true);
            yield return new WaitForSeconds(clearanceTime);

            SetLanesRed(phase1Lanes, true);  
            SetLanesRed(phase2Lanes, false); 
            yield return new WaitForSeconds(greenLightTime);

            SetLanesRed(phase1Lanes, true);
            SetLanesRed(phase2Lanes, true);
            yield return new WaitForSeconds(clearanceTime);
        }
    }

    private void SetLanesRed(List<LaneData> lanes, bool isRed)
    {
        foreach (LaneData lane in lanes)
        {
            //if (lane != null) lane.isRedLight = isRed;
            lane.SetLightStatus(isRed);
        }
    }
}