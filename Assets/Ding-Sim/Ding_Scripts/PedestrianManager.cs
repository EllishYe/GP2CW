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

    // call this in TestCityBuilder
    public void SpawnCityPedestrians()
    {
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        Transform PedestrianRoot = new GameObject("Pedestrian_Root").transform;
        // 1. find all POI in city
        PedestrianPOI[] allPOIScripts = Object.FindObjectsByType<PedestrianPOI>(FindObjectsSortMode.None);
        List<Transform> homePoints = new List<Transform>();

        // 2. find all home POI
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
                GameObject newPedestrian = Instantiate(selectedPrefab, hit.position, Quaternion.identity);
                newPedestrian.transform.SetParent(PedestrianRoot);
            }

            yield return new WaitForSeconds(spawnDelay);
        }
    }
}