using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PedestrianManager : MonoBehaviour
{
    [Header("pedestrian prefabs")]
    public List<GameObject> pedestrianPrefabs;

    [Header("setting")]
    public int PedestrianCount = 100; 
    public float spawnDelay = 1.2f;

    private Transform _pedestrianRoot;
    private List<GameObject> _activePedestrians = new List<GameObject>();
    private List<Transform> _homePoints = new List<Transform>();

    // call this in TestCityBuilder
    //public void SpawnCityPedestrians()
    //{
    //    StartCoroutine(SpawnRoutine());
    //}

    public void SpawnCityPedestrians()
    {
        _pedestrianRoot = new GameObject("Pedestrian_Root").transform;
        // 1. find all POI in city
        PedestrianPOI[] allPOIScripts = Object.FindObjectsByType<PedestrianPOI>(FindObjectsSortMode.None);
        //List<Transform> homePoints = new List<Transform>();

        // 2. find all home POI
        foreach (var poi in allPOIScripts)
        {
            if (poi.type == PedestrianPOI.POIType.Home)
                _homePoints.Add(poi.transform);
        }

        if (_homePoints.Count == 0)
        {
            Debug.LogError("🚨 全城找不到一个 'Home' 类型的 POI！");
            return;
        }


        // 3. 🚨 订阅全局时间系统的报时服务
        if (WorldTimeManager.Instance != null)
        {
            WorldTimeManager.Instance.OnHourChanged += HandleHourChanged;

            // 防呆设计：如果游戏刚启动时已经是白天（7点到18点之间），立刻刷一波人，否则城市是空的！
            float currentHour = WorldTimeManager.Instance.currentHour;
            if (currentHour >= 7 && currentHour < 18)
            {
                StartCoroutine(SpawnRoutine());
            }
        }
    }

    // 接收到时间管理器的信号后执行逻辑
    private void HandleHourChanged(int hour)
    {
        if (hour == 7) // 早上 7 点，开始生成早高峰
        {
            // 防御性编程：确保上一个周期的协程没有在运行
            StopAllCoroutines();
            StartCoroutine(SpawnRoutine());
        }
        else if (hour == 18) // 晚上 18 点，下达回家指令
        {
            SendEveryoneHome();
        }
    }


    // 批量发送回家指令
    private void SendEveryoneHome()
    {
        Debug.Log("🌙 18:00 下班时间到，全城行人开始回家...");
        foreach (GameObject npc in _activePedestrians)
        {
            if (npc != null)
            {
                PedestrianAI ai = npc.GetComponent<PedestrianAI>();
                if (ai != null) ai.GoHome();
            }
        }
    }

    // 🚨 小人到家后，由小人的脚本调用这个方法，把自己从花名册划掉
    public void UnregisterPedestrian(GameObject npc)
    {
        if (_activePedestrians.Contains(npc))
        {
            _activePedestrians.Remove(npc);
        }
    }

    // 养成好习惯：脚本销毁时取消事件订阅，防止内存泄漏
    private void OnDestroy()
    {
        if (WorldTimeManager.Instance != null)
        {
            WorldTimeManager.Instance.OnHourChanged -= HandleHourChanged;
        }
    }


    private IEnumerator SpawnRoutine()
    {
        // 依次循环在家里生成行人
        for (int i = 0; i < PedestrianCount; i++)
        {
            Transform spawnPoint = _homePoints[i % _homePoints.Count];

            Vector3 spawnPos = spawnPoint.position + Random.insideUnitSphere * 1.5f;
            spawnPos.y = spawnPoint.position.y;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(spawnPos, out hit, 2.0f, NavMesh.AllAreas))
            {
                GameObject selectedPrefab = pedestrianPrefabs[Random.Range(0, pedestrianPrefabs.Count)];
                GameObject newPedestrian = Instantiate(selectedPrefab, hit.position, Quaternion.identity);

                // 塞进文件夹
                newPedestrian.transform.SetParent(_pedestrianRoot);

                // 🚨 关键绑定：找到行人身上的 AI 脚本，告诉他“你的家在哪”
                PedestrianAI ai = newPedestrian.GetComponent<PedestrianAI>();
                if (ai != null)
                {
                    ai.InitHome(spawnPoint.position);
                }

                // 记入花名册
                _activePedestrians.Add(newPedestrian);
            }

            yield return new WaitForSeconds(spawnDelay);
        }
    }
}