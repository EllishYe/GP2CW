using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntersectionController : MonoBehaviour
{
    public List<LaneData> phase1Lanes = new List<LaneData>(); 
    public List<LaneData> phase2Lanes = new List<LaneData>(); 

    [Header("Traffic Time Setting")]
    public float greenLightTime = 8f; 
    public float clearanceTime = 2f;  

    [Tooltip("detect endpoint in 12")]
    public float detectionRadius = 12f; 

    public void AutoDetectIncomingLanes()
    {
        phase1Lanes.Clear();
        phase2Lanes.Clear();

        // get all lanes
        LaneData[] allLanes = Object.FindObjectsByType<LaneData>(FindObjectsSortMode.None);

        foreach (LaneData lane in allLanes)
        {
            if (lane.pathPoints == null || lane.pathPoints.Count < 2) continue;

            if (lane.transform.IsChildOf(this.transform)) continue;

            Vector3 lastPoint = lane.pathPoints[lane.pathPoints.Count - 1].position;
            float distanceToCenter = Vector3.Distance(transform.position, lastPoint);

            if (distanceToCenter <= detectionRadius)
            {
                Vector3 prevPoint = lane.pathPoints[lane.pathPoints.Count - 2].position;
                Vector3 laneDirection = (lastPoint - prevPoint).normalized;

                float dotForward = Mathf.Abs(Vector3.Dot(laneDirection, transform.forward));
                float dotRight = Mathf.Abs(Vector3.Dot(laneDirection, transform.right));

                if (dotForward > dotRight)
                {
                    phase1Lanes.Add(lane);
                }
                else
                {
                    phase2Lanes.Add(lane);
                }
            }
        }

        //Debug.Log($"{gameObject.name} 自动绑定完成: 相位1({phase1Lanes.Count}条), 相位2({phase2Lanes.Count}条)");
    }

    void Start()
    {
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
            if (lane != null) lane.isRedLight = isRed;
        }
    }
}