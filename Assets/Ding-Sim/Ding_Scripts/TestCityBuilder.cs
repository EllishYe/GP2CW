
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

    //void Start()
    //{
    //    //TrafficGraph myCityGraph = BuildSimpleCrossroad();

    //    // 1. 构建一个正方形的环形路网
    //    TrafficGraph myCityGraph = BuildSplitRoad();

    //    // 2. 开启协程，每隔几秒生成一辆车
    //    StartCoroutine(SpawnCars(myCityGraph));

    //}

    //IEnumerator SpawnCars(TrafficGraph graph)
    //{
    //    LaneData startLane = graph.allLanes[0];
    //    for (int i = 0; i < carCount; i++)
    //    {

    //        GameObject selectedPrefab = GetRandomCarByWeight();
    //        if (selectedPrefab != null)
    //        {
    //            GameObject myCar = Instantiate(selectedPrefab);
    //            VehicleAgent agent = myCar.GetComponent<VehicleAgent>();
    //            if (agent != null)
    //            {
    //                agent.InitVehicle(graph, startLane);
    //            }
    //        }

    //        // 等待 2 秒后再生成下一辆
    //        yield return new WaitForSeconds(spawnDelay);
    //    }
    //}

    void Start()
    {

        IntersectionController[] allIntersections = FindObjectsByType<IntersectionController>(FindObjectsSortMode.None);
        foreach (var intersection in allIntersections)
        {
            intersection.AutoDetectIncomingLanes(); // 给每个路口通电开机！
        }


        TrafficGraph myCityGraph = BuildSplitRoad();

        // 让交通局去缝合地图
        myCityGraph.BuildGraphFromScene();

        //GetComponent<NavMeshSurface>().BuildNavMesh();
        if (navMeshSurface != null)
        {
            // 这句话会瞬间扫描全城，但只在 Layer 为 Sidewalk 的物体表面生成蓝色的网格！
            navMeshSurface.BuildNavMesh();
            Debug.Log("🌐 寻路网格动态烘焙完毕！");
        }

        //生成车辆
        StartCoroutine(SpawnCars(myCityGraph));

        //生成行人
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




    //public TrafficGraph BuildSquareLoop()
    //{
    //    TrafficGraph graph = new TrafficGraph();

    //    // --- 第 1 条路：底部边 (向右开) ---
    //    LaneData lane0 = new LaneData();
    //    lane0.laneId = 0;
    //    lane0.pathPoints = new List<Vector3> {
    //        new Vector3(-20, 0.5f, -20),
    //        new Vector3(0, 0.5f, -20),
    //        new Vector3(20, 0.5f, -20)
    //    };
    //    lane0.nextLaneIds = new List<int> { 1 }; // 开完去 1 号路

    //    // --- 第 2 条路：右侧边 (向上开) ---
    //    LaneData lane1 = new LaneData();
    //    lane1.laneId = 1;
    //    lane1.pathPoints = new List<Vector3> {
    //        new Vector3(20, 0.5f, -20),
    //        new Vector3(20, 0.5f, 0),
    //        new Vector3(20, 0.5f, 20)
    //    };
    //    lane1.nextLaneIds = new List<int> { 2 }; // 开完去 2 号路

    //    // --- 第 3 条路：顶部边 (向左开) ---
    //    LaneData lane2 = new LaneData();
    //    lane2.laneId = 2;
    //    lane2.pathPoints = new List<Vector3> {
    //        new Vector3(20, 0.5f, 20),
    //        new Vector3(0, 0.5f, 20),
    //        new Vector3(-20, 0.5f, 20)
    //    };
    //    lane2.nextLaneIds = new List<int> { 3 }; // 开完去 3 号路

    //    // --- 第 4 条路：左侧边 (向下开，回到起点) ---
    //    LaneData lane3 = new LaneData();
    //    lane3.laneId = 3;
    //    lane3.pathPoints = new List<Vector3> {
    //        new Vector3(-20, 0.5f, 20),
    //        new Vector3(-20, 0.5f, 0),
    //        new Vector3(-20, 0.5f, -20)
    //    };
    //    lane3.nextLaneIds = new List<int> { 0 }; // 开完回到 0 号路！形成死循环！

    //    // 把这 4 条路都加入地图
    //    graph.lanes.Add(lane0.laneId, lane0);
    //    graph.lanes.Add(lane1.laneId, lane1);
    //    graph.lanes.Add(lane2.laneId, lane2);
    //    graph.lanes.Add(lane3.laneId, lane3);

    //    return graph;
    //}



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

    // 这是一个辅助生成路点的工具，它会凭空造出一个空物体，并把它放在指定的坐标上
    private Transform CreateNode(Vector3 pos, Transform parentLane)
    {
        GameObject nodeObj = new GameObject("PathNode"); // 创建空物体
        nodeObj.transform.position = pos;                // 放到指定坐标
        nodeObj.transform.SetParent(parentLane);         // 把它塞到路段的子层级里，保持整洁
        return nodeObj.transform;                        // 返回它的 Transform
    }
}