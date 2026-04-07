using System.Collections.Generic;
using UnityEngine;

public class VehicleAgent : MonoBehaviour
{

    public enum CarState
    {
        Driving,        
        Braking,        // 刹车
        WaitingAtLight, // 等红绿灯 (预留)
        Yielding        // 避让行人 (预留)
    }

    [Header("状态")]
    public CarState currentState = CarState.Driving; // 当前状态


    [Header("Driving parameters")]
    public float maxSpeed;       
    public float turnSpeed = 10f;
    private float currentSpeed = 0f;  // 当前实际车速

    [Header("sensor")]
    public float sensorLength = 4f;   // 探测距离
    public Vector3 sensorOffset = new Vector3(1, 0.5f, 1.8f); // 传感器位置（避免射到自己内部）

    private TrafficGraph cityGraph;
    private LaneData currentLane;
    private int currentPointIndex = 0;

    private Rigidbody rb;

    void Start()
    {
        maxSpeed = Random.Range(4f, 8f);
        rb = GetComponent<Rigidbody>();
        //rb.centerOfMass = new Vector3(1f, -0.5f, 1f);
    }

    public void InitVehicle(TrafficGraph graph, LaneData startLane)
    {
        cityGraph = graph;
        currentLane = startLane;
        if (currentLane != null && currentLane.pathPoints.Count > 0)
        {
            Vector3 startPos = currentLane.pathPoints[0].position;

            // 把汽车瞬间传送到起点，但保持汽车现有的高度 (Y轴)
            transform.position = new Vector3(startPos.x, transform.position.y, startPos.z);
            //transform.position = currentLane.pathPoints[0].position;
            currentPointIndex = 1;
        }
    }

    void FixedUpdate()
    {
        if (currentLane == null || rb == null) return;

        Sensor();
        CheckTrafficLight();

        switch (currentState)
        {
            case CarState.Driving:
                currentSpeed = Mathf.Lerp(currentSpeed, maxSpeed, Time.fixedDeltaTime * 2f);
                DriveAlongLane();
                break;

            case CarState.Braking:
            case CarState.WaitingAtLight:
            case CarState.Yielding:
                currentSpeed = Mathf.Lerp(currentSpeed, 0, Time.fixedDeltaTime * 10f); // 强力刹车

                if (currentSpeed < 2f)
                {
                    currentSpeed = 0f;
                }
                DriveAlongLane(); // 即使刹车，也需要沿着轨迹慢慢停下
                break;
        }
    }

    private void Sensor()
    {
        bool isPathClear = true;

        // 计算雷达发射点，将其转换到世界坐标
        Vector3 sensorStartPos = transform.TransformPoint(sensorOffset);

        // 向前发射隐形的射线
        RaycastHit hit;
        if (Physics.Raycast(sensorStartPos, transform.forward, out hit, sensorLength))
        {
            // 如果碰到了 Tag 为 "Vehicle" 的物体
            if (hit.collider.CompareTag("Vehicle"))
            {
                isPathClear = false;
                // 在 Scene 窗口画一条红线，方便你 Debug
                Debug.DrawRay(sensorStartPos, transform.forward * hit.distance, Color.red);
            }
        }

        if (isPathClear)
        {
            // 前方畅通，平滑加速到最高限速
            Debug.DrawRay(sensorStartPos, transform.forward * sensorLength, Color.green);
            //currentSpeed = Mathf.Lerp(currentSpeed, maxSpeed, Time.deltaTime * 2f);
            currentState = CarState.Driving;
        }
        else
        {
            // 发现前车！紧急刹车跟车
            currentState = CarState.Braking;
        }
    }


    // 🚨 新增：司机的红绿灯视觉系统
    private void CheckTrafficLight()
    {
        // 1. 只有当车子正开向这条路的【最后一个路点】时（说明马上要进十字路口了），才需要看灯
        if (currentPointIndex == currentLane.pathPoints.Count - 1)
        {
            // 2. 抬头看一眼，这根车道现在是红灯吗？
            if (currentLane.isRedLight)
            {
                // 3. 算出车头离停止线（最后一个路点）还有多远
                Vector3 stopPos = currentLane.pathPoints[currentPointIndex].position;
                float distanceToStopLine = Vector3.Distance(transform.position, stopPos);

                // 4. 如果距离停止线不到 3 米了，立刻踩刹车进入等灯状态！
                if (distanceToStopLine < 3f)
                {
                    currentState = CarState.WaitingAtLight;
                    return; // 结束判断
                }
            }
        }

        // 5. 状态恢复：如果车子正在等红灯，但路口的交警把灯切回绿灯了 (isRedLight == false)
        if (currentState == CarState.WaitingAtLight && !currentLane.isRedLight)
        {
            // 挂挡，重新起步！
            currentState = CarState.Driving;
        }
    }



    private void DriveAlongLane()
    {
        Vector3 targetPoint = currentLane.pathPoints[currentPointIndex].position;
        Vector3 flatTarget = new Vector3(targetPoint.x, rb.position.y, targetPoint.z);

        // 注意：这里使用的是 currentSpeed 而不是固定的 speed 了
        Vector3 nextPosition = Vector3.MoveTowards(rb.position, flatTarget, currentSpeed * Time.fixedDeltaTime);
        rb.MovePosition(nextPosition);

        Vector3 direction = (flatTarget - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, lookRotation, Time.fixedDeltaTime * turnSpeed));
        }

        if (Vector3.Distance(transform.position, flatTarget) < 0.1f)
        {
            currentPointIndex++;
            if (currentPointIndex >= currentLane.pathPoints.Count)
            {
                SwitchToNextLane();
            }
        }
    }

    private void SwitchToNextLane()
    {
        if (currentLane.nextLanes != null && currentLane.nextLanes.Count > 0)
        {
            //int randomNextId = currentLane.nextLaneIds[Random.Range(0, currentLane.nextLaneIds.Count)];         
            //currentLane = cityGraph.lanes[randomNextId];

            int randomChoice = Random.Range(0, currentLane.nextLanes.Count);
            currentLane = currentLane.nextLanes[randomChoice];
            currentPointIndex = 0;
        }
        else
        {
            currentLane = null;
        }
    }
}