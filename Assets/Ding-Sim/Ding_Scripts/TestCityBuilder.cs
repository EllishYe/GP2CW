using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;



[System.Serializable]
public struct CarSpawnData
{
    public GameObject carPrefab;
    [Range(0f, 100f)]
    public float spawnWeight;
}

public class TestCityBuilder : MonoBehaviour
{
    public List<CarSpawnData> carSpawnPool;
    public int carCount = 5; 
    public float spawnDelay = 0f; 
    public NavMeshSurface navMeshSurface;

    public RoadNetworkGenerator pcgGenerator;
    public TrafficGraph myTrafficGraph;

    void Start()
    {

        if (myTrafficGraph == null)
            myTrafficGraph = GetComponent<TrafficGraph>();

        //StartCoroutine(CityGenerationPipeline());
    }

    public void UI_TriggerGenerateAll()
    {
        // 1. 停止当前可能正在运行的任何生成协程，防止逻辑冲突
        StopAllCoroutines();

        // 2. (可选) 清理场景中已有的车辆和行人，实现“一键重置并生成”
        // 注意：你需要确保你的脚本里有对这些 Root 物体的引用
        GameObject oldVehicles = GameObject.Find("Vehicles_Root");
        if (oldVehicles != null) Destroy(oldVehicles);

        GameObject oldPeds = GameObject.Find("Pedestrians_Root"); // 假设你的行人管理器生成在这个根下
        if (oldPeds != null) Destroy(oldPeds);

        // 3. 重新启动完整的生成流水线
        StartCoroutine(CityGenerationPipeline());

        Debug.Log("🚀 UI 事件：已触发完整生成流水线（PCG -> 桥接 -> 交通网 -> 代理生成）");
    }

    IEnumerator CityGenerationPipeline()
    {
        Debug.Log("🏗️ [阶段 1/5] 开始生成 PCG 骨架...");
        pcgGenerator.Generate();

        Debug.Log("🏗️ [阶段 2/5] 翻译数据并生成物理车道...");
        BridgeDataAndSpawnCars(); 

        // 🚨 极其关键的安全锁：强制 Unity 立刻刷新一次物理引擎的空间划分树！
        Physics.SyncTransforms();
        // 或者如果你求稳，也可以让代码停顿一帧： yield return new WaitForFixedUpdate();

        Debug.Log("🏗️ [阶段 3/5] 交通局开始打通并接管全城路网...");
        myTrafficGraph.BuildGraphFromScene(); 

        Debug.Log("🏗️ [阶段 4/5] 激活十字路口交警...");
        IntersectionController[] allIntersections = FindObjectsByType<IntersectionController>(FindObjectsSortMode.None);
        foreach (var intersection in allIntersections)
        {
            intersection.AutoDetectIncomingLanes();
        }

        Debug.Log("🏗️ [阶段 5/5] 烘焙行人 NavMesh...");
        if (navMeshSurface != null)
        {
            // ⚠️ 前提：你要确保队友的 PCG 在阶段 1 已经把人行道的 Mesh 生成出来了！
            navMeshSurface.BuildNavMesh();
        }

        // wait for 0.1 second
        yield return new WaitForSeconds(0.1f);

        Debug.Log("put vehicles and pedestrians");
        StartCoroutine(SpawnCars(myTrafficGraph));
        FindAnyObjectByType<PedestrianManager>().SpawnCityPedestrians();
    }

    // ⚠️ 注意：这里多加了一个参数 startLane
    IEnumerator SpawnCars(TrafficGraph graph)
    {
        Transform vehiclesRoot = new GameObject("Vehicles_Root").transform;

        if (graph.allLanes == null || graph.allLanes.Count == 0) yield break;

        int actualSpawnedCount = 0;
        int maxAttempts = carCount * 10;
        int currentAttempts = 0;

        // 🚨 终极防重叠武器：建立一个“已停车位”黑名单！
        List<Vector3> spawnedPositions = new List<Vector3>();

        // 两辆车之间的绝对安全距离（假设车长4米，你想要它们隔开一点，就设为 5f 或 6f）
        float safeDistance = 12.0f;

        for (int i = 0; i < carCount; i++)
        {
            currentAttempts++;
            if (currentAttempts > maxAttempts)
            {
                Debug.LogWarning($"⚠️ 城市已塞满！目标 {carCount} 辆，实际成功 {actualSpawnedCount} 辆。");
                break;
            }

            LaneData chosenLane = graph.allLanes[Random.Range(0, graph.allLanes.Count)];
            Vector3 startPos = chosenLane.pathPoints[0].position;
            Vector3 endPos = chosenLane.pathPoints[chosenLane.pathPoints.Count - 1].position;

            float randomT = Random.Range(0.1f, 0.9f);
            Vector3 spawnPos = Vector3.Lerp(startPos, endPos, randomT) + Vector3.up * 0.5f;

            // 🛡️ 纯数学探测：拿尺子量距离，不依赖物理引擎！
            bool isSpaceClear = true;
            foreach (Vector3 existingPos in spawnedPositions)
            {
                // 如果这个候选点，距离任何一辆已经生成的车小于安全距离
                if (Vector3.Distance(spawnPos, existingPos) < safeDistance)
                {
                    isSpaceClear = false;
                    break; // 太挤了，放弃这个点！
                }
            }

            if (isSpaceClear)
            {
                GameObject selectedPrefab = GetRandomCarByWeight();
                if (selectedPrefab != null)
                {
                    GameObject myCar = Instantiate(selectedPrefab, spawnPos, Quaternion.identity);
                    myCar.transform.SetParent(vehiclesRoot);
                    VehicleAgent agent = myCar.GetComponent<VehicleAgent>();

                    myCar.transform.LookAt(endPos);
                    if (agent != null) agent.InitVehicle(graph, chosenLane);

                    // 🚨 核心步骤：把这个安全坐标加入黑名单！
                    // 这样下一辆车绝对不可能再刷在这个点附近
                    spawnedPositions.Add(spawnPos);
                    actualSpawnedCount++;
                }

                // 因为不用等物理引擎刷新了，你可以把等待时间调得极短，甚至设为 0
                yield return new WaitForSeconds(0.01f);
            }
            else
            {
                // 距离太近，退回抽签
                i--;
            }
        }

        Debug.Log($"🚗 纯数学防重叠部署完毕，成功塞下 {actualSpawnedCount} 辆车！");

    }


    private GameObject GetRandomCarByWeight()
    {
        float totalWeight = 0f;
        foreach (var data in carSpawnPool)
        {
            totalWeight += data.spawnWeight;
        }

        float randomPoint = Random.Range(0, totalWeight);

        foreach (var data in carSpawnPool)
        {
            randomPoint -= data.spawnWeight;
            if (randomPoint <= 0)
            {
                return data.carPrefab;
            }
        }
        return carSpawnPool[0].carPrefab;
    }


    //public void BridgeDataAndSpawnCars()
    //{
    //    // 确保数据已经生成
    //    if (pcgGenerator.lanes == null || pcgGenerator.lanes.Count == 0)
    //    {
    //        Debug.LogError("车道数据为空！");
    //        return;
    //    }

    //    List<AgentLaneData> processedLanes = AgentLaneExporter.Export(pcgGenerator.lanes);
    //    Transform lanesRoot = new GameObject("AI_Lanes_Root").transform;
    //    List<LaneData> myAILanes = new List<LaneData>();

    //    foreach (AgentLaneData agentLane in processedLanes)
    //    {
    //        // 1. 创建 AI 车道实体
    //        GameObject laneObj = new GameObject("AutoLane");
    //        laneObj.transform.SetParent(lanesRoot);
    //        LaneData myLane = laneObj.AddComponent<LaneData>();

    //        Transform p1 = new GameObject("P_Start").transform;
    //        Transform p2 = new GameObject("P_End").transform;
    //        Transform p3 = new GameObject("P_Stop").transform;
    //        p1.SetParent(laneObj.transform);
    //        p2.SetParent(laneObj.transform);
    //        p3.SetParent(laneObj.transform);

    //        Vector3 unityStart = agentLane.startPoint;
    //        Vector3 unityEnd = agentLane.endPoint;
    //        Vector3 unityStop = agentLane.vehicleStopPoint;

    //        p1.position = unityStart;
    //        p2.position = unityEnd;
    //        p3.position = unityStop;

    //        myLane.pathPoints.Add(p1);
    //        myLane.pathPoints.Add(p2);
    //        //myLane.pathPoints.Add(p3);

    //        myLane.stopLinePoint = p3;

    //        myAILanes.Add(myLane);
    //    }

    //    // 这里接着写你的拓扑连线逻辑 (双重 foreach 检查距离那个)
    //    ConnectLanes(myAILanes);

    //    Debug.Log($"成功读取了 {pcgGenerator.lanes.Count} 条 PCG 车道，并已全部展平到 3D 地面！");
    //}


    public void BridgeDataAndSpawnCars()
    {
        if (pcgGenerator.lanes == null || pcgGenerator.lanes.Count == 0) return;

        List<AgentLaneData> processedLanes = pcgGenerator.GetAgentLanes();
        Transform lanesRoot = new GameObject("AI_Lanes_Root").transform;
        List<LaneData> myAILanes = new List<LaneData>();

        foreach (AgentLaneData agentLane in processedLanes)
        {
            GameObject laneObj = new GameObject("AutoLane_" + agentLane.id);
            laneObj.transform.SetParent(lanesRoot);
            LaneData myLane = laneObj.AddComponent<LaneData>();

            Transform p1 = new GameObject("P_Start").transform;
            Transform p2 = new GameObject("P_End").transform;
            Transform p3 = new GameObject("P_Stop").transform;

            p1.SetParent(laneObj.transform);
            p2.SetParent(laneObj.transform);
            p3.SetParent(laneObj.transform);

            // ==========================================
            // 🎯 核心提取：直接各取所需！
            // ==========================================

            // 1. 汽车出生点：使用带偏移的 VehicleStart
            p1.position = agentLane.vehicleStartPoint;

            // 2. 马路几何终点：使用一直延伸到路口中心的 EndPoint
            p2.position = agentLane.endPoint;

            // 3. 红绿灯刹车点：使用队友专门提供的 VehicleStopPoint
            p3.position = agentLane.vehicleStopPoint;

            // ==========================================

            // 加入寻路路点（车子平时沿着 P1 开向 P2）
            myLane.pathPoints.Add(p1);
            myLane.pathPoints.Add(p2);

            // 专门指定红绿灯刹车点
            myLane.stopLinePoint = p3;

            myAILanes.Add(myLane);
        }

        ConnectLanes(myAILanes);
        GenerateTrafficLights(processedLanes);
    }


    public void GenerateTrafficLights(List<AgentLaneData> pcgLanes)
    {
        // 1. 我们不再用精确的字符串字典，而是用两个列表来记录“聚类堆”
        List<Vector3> clusterCenters = new List<Vector3>();
        List<int> clusterCounts = new List<int>();

        // 💡 聚类半径：只要端点距离在 20 米以内，就算同一个路口
        float clusterRadius = 20f;

        foreach (var lane in pcgLanes)
        {
            Vector3 endPos = lane.endPoint;
            bool foundCluster = false;

            // 遍历目前已知的“路口中心堆”
            for (int i = 0; i < clusterCenters.Count; i++)
            {
                if (Vector3.Distance(endPos, clusterCenters[i]) < clusterRadius)
                {
                    clusterCounts[i]++;
                    // 核心魔法：不断更新这个堆的几何平均中心。
                    // 这样即使四个点散落在四周，平均下来也刚好是绝对的十字路口中心！
                    clusterCenters[i] = (clusterCenters[i] * (clusterCounts[i] - 1) + endPos) / clusterCounts[i];
                    foundCluster = true;
                    break;
                }
            }

            // 如果方圆 20 米内没有现成的堆，就自己作为新路口的火种
            if (!foundCluster)
            {
                clusterCenters.Add(endPos);
                clusterCounts.Add(1);
            }
        }

        // 2. 开始生成红绿灯实体
        Transform trafficLightsRoot = new GameObject("Traffic_Lights_Root").transform;
        int lightCount = 0;

        for (int i = 0; i < clusterCenters.Count; i++)
        {
            // 3. 只有汇聚了 3 条及以上车道的地方，才是真路口
            if (clusterCounts[i] >= 3)
            {
                GameObject intersectionObj = new GameObject($"Intersection_{lightCount}");
                // 完美！这个算出来的平均中心，绝对位于四条马路的正中央
                intersectionObj.transform.position = clusterCenters[i];
                intersectionObj.transform.SetParent(trafficLightsRoot);

                IntersectionController controller = intersectionObj.AddComponent<IntersectionController>();

                // 因为此时 controller 的坐标已经在路口中心了，
                // 所以它去探测 12 米内的端点，绝对能把刚才那 4 条车道抓回来！
                controller.AutoDetectIncomingLanes();

                lightCount++;
            }
        }

        Debug.Log($"🚥 聚类雷达扫描完毕！成功自动生成了 {lightCount} 个红绿灯！");
    }

    public TrafficGraph BuildSplitRoad()
    {
        GameObject graphObj = new GameObject("TempCityGraph_TestOnly");
        TrafficGraph graph = graphObj.AddComponent<TrafficGraph>();


        return graph;
    }

 


    // 拓扑连线核心逻辑：把散落的线段缝合成交通网
    private void ConnectLanes(List<LaneData> lanes)
    {
        float connectionThreshold = 0.02f; // 距离小于 0.5 米就认为在同一个路口

        foreach (LaneData laneA in lanes)
        {
            laneA.nextLanes.Clear();
            // 拿到当前路段的最后一个点（出口）
            Vector3 aEnd = laneA.pathPoints[laneA.pathPoints.Count - 1].position;

            foreach (LaneData laneB in lanes)
            {
                if (laneA == laneB) continue; // 不和自己连

                // 拿到另一条路段的第一个点（入口）
                Vector3 bStart = laneB.pathPoints[0].position;

                // 拿卷尺量一下，如果 A 的出口碰到了 B 的入口，连起来！
                if (Vector3.Distance(aEnd, bStart) < connectionThreshold)
                {
                    // 确保列表已初始化
                    if (laneA.nextLanes == null) laneA.nextLanes = new List<LaneData>();

                    laneA.nextLanes.Add(laneB);
                }
            }
        }
    }

    // 这是一个辅助生成路点的工具，它会凭空造出一个空物体，并把它放在指定的坐标上
    private Transform CreateNode(Vector3 pos, Transform parentLane)
    {
        GameObject nodeObj = new GameObject("PathNode"); // 创建空物体
        nodeObj.transform.position = pos;                // 放到指定坐标
        nodeObj.transform.SetParent(parentLane);         // 把它塞到路段的子层级里，保持整洁
        return nodeObj.transform;                        // 返回它的 Transform
    }
}