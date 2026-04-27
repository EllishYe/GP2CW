using System.Collections;
using System.Collections.Generic;
using TMPro;
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
    public TextMeshProUGUI vehicleCountText;
    public float spawnDelay = 0f; 
    public NavMeshSurface navMeshSurface;

    public RoadNetworkGenerator pcgGenerator;
    public TrafficGraph myTrafficGraph;

    void Start()
    {

        if (myTrafficGraph == null)
            myTrafficGraph = GetComponent<TrafficGraph>();

        vehicleCountText.text = $"Vehicle Count: {carCount}";

        //StartCoroutine(CityGenerationPipeline());
    }

    public void UI_TriggerGenerateAll()
    {
        StopAllCoroutines();

        GameObject oldVehicles = GameObject.Find("Vehicles_Root");
        if (oldVehicles != null) Destroy(oldVehicles);

        GameObject oldPeds = GameObject.Find("Pedestrians_Root");
        if (oldPeds != null) Destroy(oldPeds);


        StartCoroutine(CityGenerationPipeline());

        Debug.Log("Start Generating Agents");
    }

    //Vehicle Slider 绑定
    public void OnVehicleSliderChanged(float newValue)
    {
        carCount = Mathf.RoundToInt(newValue); 

        if (vehicleCountText != null)
        {
            //vehicleSlider.value = carCount;
            vehicleCountText.text = $"Vehicle Count: {carCount}";
        }
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
            navMeshSurface.BuildNavMesh();
        }

        // wait for 0.1 second
        yield return new WaitForSeconds(0.1f);

        Debug.Log("put vehicles and pedestrians");
        StartCoroutine(SpawnCars(myTrafficGraph));
        FindAnyObjectByType<PedestrianManager>().SpawnCityPedestrians();
    }

    IEnumerator SpawnCars(TrafficGraph graph)
    {
        Transform vehiclesRoot = new GameObject("Vehicles_Root").transform;

        if (graph.allLanes == null || graph.allLanes.Count == 0) yield break;

        int actualSpawnedCount = 0;
        int maxAttempts = carCount * 10;
        int currentAttempts = 0;

        List<Vector3> spawnedPositions = new List<Vector3>();


        float safeDistance = 12.0f;

        for (int i = 0; i < carCount; i++)
        {
            currentAttempts++;
            if (currentAttempts > maxAttempts)
            {
                Debug.LogWarning($"generate {actualSpawnedCount} vehicles");
                break;
            }

            LaneData chosenLane = graph.allLanes[Random.Range(0, graph.allLanes.Count)];
            Vector3 startPos = chosenLane.pathPoints[0].position;
            Vector3 endPos = chosenLane.pathPoints[chosenLane.pathPoints.Count - 1].position;

            float randomT = Random.Range(0.1f, 0.9f);
            Vector3 spawnPos = Vector3.Lerp(startPos, endPos, randomT) + Vector3.up * 0.5f;

            bool isSpaceClear = true;
            foreach (Vector3 existingPos in spawnedPositions)
            {
                if (Vector3.Distance(spawnPos, existingPos) < safeDistance)
                {
                    isSpaceClear = false;
                    break; 
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

                    spawnedPositions.Add(spawnPos);
                    actualSpawnedCount++;
                }

                yield return new WaitForSeconds(0.01f);
            }
            else
            {
                i--;
            }
        }

        Debug.Log($"generate {actualSpawnedCount} vehicles");

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

            p1.position = agentLane.vehicleStartPoint;
            p2.position = agentLane.endPoint;
            p3.position = agentLane.vehicleStopPoint;

            myLane.pathPoints.Add(p1);
            myLane.pathPoints.Add(p2);

            myLane.stopLinePoint = p3;

            myAILanes.Add(myLane);
        }

        ConnectLanes(myAILanes);
        GenerateTrafficLights(processedLanes);
    }


    public void GenerateTrafficLights(List<AgentLaneData> pcgLanes)
    {
        List<Vector3> clusterCenters = new List<Vector3>();
        List<int> clusterCounts = new List<int>();

        // radius
        float clusterRadius = 20f;

        foreach (var lane in pcgLanes)
        {
            Vector3 endPos = lane.endPoint;
            bool foundCluster = false;

            for (int i = 0; i < clusterCenters.Count; i++)
            {
                if (Vector3.Distance(endPos, clusterCenters[i]) < clusterRadius)
                {
                    clusterCounts[i]++;
                    clusterCenters[i] = (clusterCenters[i] * (clusterCounts[i] - 1) + endPos) / clusterCounts[i];
                    foundCluster = true;
                    break;
                }
            }

            if (!foundCluster)
            {
                clusterCenters.Add(endPos);
                clusterCounts.Add(1);
            }
        }

        // 2. generate traffic lights
        Transform trafficLightsRoot = new GameObject("Traffic_Lights_Root").transform;
        int lightCount = 0;

        for (int i = 0; i < clusterCenters.Count; i++)
        {
            if (clusterCounts[i] >= 3)
            {
                GameObject intersectionObj = new GameObject($"Intersection_{lightCount}");
                intersectionObj.transform.position = clusterCenters[i];
                intersectionObj.transform.SetParent(trafficLightsRoot);

                IntersectionController controller = intersectionObj.AddComponent<IntersectionController>();

                controller.AutoDetectIncomingLanes();

                lightCount++;
            }
        }

        Debug.Log($"generate {lightCount} traffic lights");
    }

    public TrafficGraph BuildSplitRoad()
    {
        GameObject graphObj = new GameObject("TempCityGraph_TestOnly");
        TrafficGraph graph = graphObj.AddComponent<TrafficGraph>();


        return graph;
    }

 

    private void ConnectLanes(List<LaneData> lanes)
    {
        float connectionThreshold = 0.02f; 

        foreach (LaneData laneA in lanes)
        {
            laneA.nextLanes.Clear();
            Vector3 aEnd = laneA.pathPoints[laneA.pathPoints.Count - 1].position;

            foreach (LaneData laneB in lanes)
            {
                if (laneA == laneB) continue; 

                Vector3 bStart = laneB.pathPoints[0].position;
                if (Vector3.Distance(aEnd, bStart) < connectionThreshold)
                {
                    if (laneA.nextLanes == null) laneA.nextLanes = new List<LaneData>();

                    laneA.nextLanes.Add(laneB);
                }
            }
        }
    }

    private Transform CreateNode(Vector3 pos, Transform parentLane)
    {
        GameObject nodeObj = new GameObject("PathNode"); // 创建空物体
        nodeObj.transform.position = pos;                // 放到指定坐标
        nodeObj.transform.SetParent(parentLane);         // 把它塞到路段的子层级里，保持整洁
        return nodeObj.transform;                        // 返回它的 Transform
    }
}