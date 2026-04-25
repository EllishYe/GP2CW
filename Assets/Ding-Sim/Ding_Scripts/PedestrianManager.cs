using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PedestrianManager : MonoBehaviour
{
    [Header("pedestrian prefabs")]
    public List<GameObject> pedestrianPrefabs;

    [Header("setting")]
    public int PedestrianCount = 100; // 你想生成的全城总人数
    public float spawnDelay = 1.2f;        // 每隔 0.05 秒生成一个，防止瞬间卡死

    // 这个方法留给大总管 TestCityBuilder 在建城完毕后调用
    public void SpawnCityPedestrians()
    {
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        // 1. 找到全城所有的 POI 脚本
        PedestrianPOI[] allPOIScripts = Object.FindObjectsByType<PedestrianPOI>(FindObjectsSortMode.None);
        List<Transform> homePoints = new List<Transform>();

        // 2. 筛选出所有类型为 Home 的点
        foreach (var poi in allPOIScripts)
        {
            if (poi.type == PedestrianPOI.POIType.Home)
                homePoints.Add(poi.transform);
        }

        if (homePoints.Count == 0)
        {
            Debug.LogError("🚨 全城找不到一个 'Home' 类型的 POI！");
            yield break;
        }


        // 3. 开始依次/循环在家里生成行人
        for (int i = 0; i < PedestrianCount; i++)
        {
            // 依次取家（如果人数多于家，就取模循环）
            Transform spawnPoint = homePoints[i % homePoints.Count];

            // 加上我们之前的随机偏移和 NavMesh 吸附逻辑
            Vector3 spawnPos = spawnPoint.position + Random.insideUnitSphere * 1.5f;
            spawnPos.y = spawnPoint.position.y;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(spawnPos, out hit, 2.0f, NavMesh.AllAreas))
            {
                GameObject selectedPrefab = pedestrianPrefabs[Random.Range(0, pedestrianPrefabs.Count)];
                Instantiate(selectedPrefab, hit.position, Quaternion.identity);
            }

            yield return new WaitForSeconds(spawnDelay);
        }
    }
}