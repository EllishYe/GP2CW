
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
    public float spawnDelay = 0f;  //每2秒生成一辆车
    public NavMeshSurface navMeshSurface;

    public RoadNetworkGenerator pcgGenerator;
    public TrafficGraph myTrafficGraph;

    void Start()
    {

        if (myTrafficGraph == null)
            myTrafficGraph = GetComponent<TrafficGraph>();

        StartCoroutine(CityGenerationPipeline());
        //pcgGenerator.Generate();

        //IntersectionController[] allIntersections = FindObjectsByType<IntersectionController>(FindObjectsSortMode.None);
        //foreach (var intersection in allIntersections)
        //{
        //    intersection.AutoDetectIncomingLanes(); // 给每个路口通电开机！
        //}


        //TrafficGraph myCityGraph = BuildSplitRoad();

        //// 让交通局去缝合地图
        //myCityGraph.BuildGraphFromScene();

        ////GetComponent<NavMeshSurface>().BuildNavMesh();
        //if (navMeshSurface != null)
        //{
        //    // 这句话会瞬间扫描全城，但只在 Layer 为 Sidewalk 的物体表面生成蓝色的网格！
        //    navMeshSurface.BuildNavMesh();
        //    Debug.Log("🌐 寻路网格动态烘焙完毕！");
        //}

        ////生成车辆
        //StartCoroutine(SpawnCars(myCityGraph));

        ////生成行人
        //FindAnyObjectByType<PedestrianManager>().SpawnCityPedestrians();


    }

    IEnumerator CityGenerationPipeline()
    {
        Debug.Log("🏗️ [阶段 1/5] 开始生成 PCG 骨架...");
        pcgGenerator.Generate();

        Debug.Log("🏗️ [阶段 2/5] 翻译数据并生成物理车道...");
        BridgeDataAndSpawnCars(); // 👈 极其关键！把你上一条消息写的那个方法加在这里！

        // 🚨 极其关键的安全锁：强制 Unity 立刻刷新一次物理引擎的空间划分树！
        // 这样接下来的路口探测才能 100% 摸到刚生成的车道。
        Physics.SyncTransforms();
        // 或者如果你求稳，也可以让代码停顿一帧： yield return new WaitForFixedUpdate();

        Debug.Log("🏗️ [阶段 3/5] 交通局开始打通并接管全城路网...");
        // myCityGraph = BuildSplitRoad(); // (如果你用 PCG 了，这句旧代码可能就不需要了)
        myTrafficGraph.BuildGraphFromScene(); // 让交通局收集所有路段

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

        // 稍微等 0.1 秒，确保场景彻底稳定，再开始投放移动实体（避免卡顿掉地底下）
        yield return new WaitForSeconds(0.1f);

        Debug.Log("🚀 [最终阶段] 投放车辆与行人！");
        StartCoroutine(SpawnCars(myTrafficGraph));
        FindAnyObjectByType<PedestrianManager>().SpawnCityPedestrians();
    }

    // ⚠️ 注意：这里多加了一个参数 startLane
    IEnumerator SpawnCars(TrafficGraph graph)
    {
        if (graph.allLanes == null || graph.allLanes.Count == 0) yield break;

        int actualSpawnedCount = 0;
        int maxAttempts = carCount * 10;
        int currentAttempts = 0;

        // 🚨 终极防重叠武器：建立一个“已停车位”黑名单！
        List<Vector3> spawnedPositions = new List<Vector3>();

        // 两辆车之间的绝对安全距离（假设车长4米，你想要它们隔开一点，就设为 5f 或 6f）
        float safeDistance = 6.0f;

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

    // 4. 核心算法：权重随机挑选
    private GameObject GetRandomCarByWeight()
    {
        // 第一步：算出所有汽车权重的总和
        float totalWeight = 0f;
        foreach (var data in carSpawnPool)
        {
            totalWeight += data.spawnWeight;
        }

        // 第二步：在这个总和范围内，随机扔一个骰子
        float randomPoint = Random.Range(0, totalWeight);

        // 第三步：看看这个骰子落在了哪个“扇形区域”里
        foreach (var data in carSpawnPool)
        {
            // 不断减去当前物品的权重
            randomPoint -= data.spawnWeight;

            // 如果减到小于等于 0，说明骰子就落在这个物品的区间里！
            if (randomPoint <= 0)
            {
                return data.carPrefab;
            }
        }

        // 防呆设计：理论上上面一定会 return，万一浮点数精度误差走到这里，默认返回第一个
        return carSpawnPool[0].carPrefab;
    }


    public void BridgeDataAndSpawnCars()
    {
        // 确保数据已经生成
        if (pcgGenerator.lanes == null || pcgGenerator.lanes.Count == 0)
        {
            Debug.LogError("车道数据为空！");
            return;
        }

        Transform lanesRoot = new GameObject("AI_Lanes_Root").transform;
        List<LaneData> myAILanes = new List<LaneData>();

        // 🎯 核心读取环节
        foreach (var pcgLane in pcgGenerator.lanes)
        {
            // 1. 创建 AI 车道实体
            GameObject laneObj = new GameObject("AutoLane");
            laneObj.transform.SetParent(lanesRoot);
            LaneData myLane = laneObj.AddComponent<LaneData>();

            Transform p1 = new GameObject("P_Start").transform;
            Transform p2 = new GameObject("P_End").transform;
            p1.SetParent(laneObj.transform);
            p2.SetParent(laneObj.transform);

            // ==========================================
            // 🚨 看这里！根据你的截图，直接精准读取 Start 和 End
            Vector2 rawStart = pcgLane.start;
            Vector2 rawEnd = pcgLane.end;

            // 强制平躺转换：把队友二维的 Y 轴数据，塞进 Unity 三维的 Z 轴里
            Vector3 unityStart = new Vector3(rawStart.x, 0f, rawStart.y);
            Vector3 unityEnd = new Vector3(rawEnd.x, 0f, rawEnd.y);
            // ==========================================

            p1.position = unityStart;
            p2.position = unityEnd;

            myLane.pathPoints.Add(p1);
            myLane.pathPoints.Add(p2);

            myAILanes.Add(myLane);
        }

        // 这里接着写你的拓扑连线逻辑 (双重 foreach 检查距离那个)
        ConnectLanes(myAILanes);

        // 把翻译好的、铺在地面上的 514 条路，交给你之前写的交通管理器去刷车！
        // myTrafficGraph.allLanes = myAILanes;

        Debug.Log($"成功读取了 {pcgGenerator.lanes.Count} 条 PCG 车道，并已全部展平到 3D 地面！");
    }


    public TrafficGraph BuildSplitRoad()
    {
        GameObject graphObj = new GameObject("TempCityGraph_TestOnly");
        TrafficGraph graph = graphObj.AddComponent<TrafficGraph>();

        // 1. 驶入路段 (从 Z=-20 开到原点 Z=0)
        //GameObject approachObj = new GameObject("Approach_Lane");
        //LaneData approachLane = approachObj.AddComponent<LaneData>();
        //approachLane.pathPoints = new List<Transform> {
        //    CreateNode(new Vector3(0, 0.5f, -20), approachObj.transform),
        //    CreateNode(new Vector3(0, 0.5f, -10), approachObj.transform),
        //    CreateNode(new Vector3(0, 0.5f, 0), approachObj.transform)
        //};

        //// 2. 直行路段 (从原点开到 Z=20)
        //GameObject straightObj = new GameObject("Straight_Lane");
        //LaneData straightLane = straightObj.AddComponent<LaneData>();
        //straightLane.pathPoints = new List<Transform> {
        //    CreateNode(new Vector3(0, 0.5f, 0), straightObj.transform),
        //    CreateNode(new Vector3(0, 0.5f, 10), straightObj.transform),
        //    CreateNode(new Vector3(0, 0.5f, 20), straightObj.transform)
        //};

        //// 3. 左转路段 (从原点开到 X=-20)
        //GameObject leftObj = new GameObject("Left_Lane");
        //LaneData leftLane = leftObj.AddComponent<LaneData>();
        //leftLane.pathPoints = new List<Transform> {
        //    CreateNode(new Vector3(0, 0.5f, 0), leftObj.transform),
        //    CreateNode(new Vector3(-5, 0.5f, 5), leftObj.transform),
        //    CreateNode(new Vector3(-20, 0.5f, 5), leftObj.transform)
        //};

        //// 4. 右转路段 (从原点开到 X=20)
        //GameObject rightObj = new GameObject("Right_Lane");
        //LaneData rightLane = rightObj.AddComponent<LaneData>();
        //rightLane.pathPoints = new List<Transform> {
        //    CreateNode(new Vector3(0, 0.5f, 0), rightObj.transform),
        //    CreateNode(new Vector3(5, 0.5f, 5), rightObj.transform),
        //    CreateNode(new Vector3(20, 0.5f, 5), rightObj.transform)
        //};

        // ⚠️ 注意看：这里一行 nextLanes 赋值代码都没有！全靠坐标自动缝合！

        return graph;
    }



    ////最简单 弃用
    //public TrafficGraph BuildSimpleCrossroad()
    //{
    //    TrafficGraph graph = new TrafficGraph();  //空的
    //    LaneData lane0 = new LaneData();          //一条路
    //    lane0.laneId = 0;
    //    lane0.pathPoints = new List<Vector3>
    //    {
    //        new Vector3(-20,0.5f,1.5f),
    //        new Vector3(-5,0.5f,1.5f),
    //        new Vector3(0,0.5f,1.5f),
    //        new Vector3(5,0.5f,1.5f),
    //        new Vector3(20,0.5f,1.5f)
    //    };
    //    lane0.nextLaneIds = new List<int>(); // 为了测试不报错，先初始化为空

    //    graph.lanes.Add(lane0.laneId, lane0);
    //    return graph;
    //}

    


    // 拓扑连线核心逻辑：把散落的线段缝合成交通网
    private void ConnectLanes(List<LaneData> lanes)
    {
        float connectionThreshold = 0.5f; // 距离小于 0.5 米就认为在同一个路口

        foreach (LaneData laneA in lanes)
        {
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