using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PedestrianManager : MonoBehaviour
{
    [Header("行人预制体 (可以放多个不同外观的小人)")]
    public List<GameObject> pedestrianPrefabs;

    [Header("生成设置")]
    public int targetPedestrianCount = 100; // 你想生成的全城总人数
    public float spawnDelay = 0.05f;        // 每隔 0.05 秒生成一个，防止瞬间卡死

    // 这个方法留给大总管 TestCityBuilder 在建城完毕后调用
    public void SpawnCityPedestrians()
    {
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        // 1. 获取全城所有的人行道兴趣点
        GameObject[] allPOIs = GameObject.FindGameObjectsWithTag("PedestrianPOI");

        if (allPOIs.Length == 0)
        {
            Debug.LogError("🚨 致命错误：没找到任何 PedestrianPOI！无法生成行人！");
            yield break;
        }

        // 2. 把数组转换成 List，方便我们“像发牌一样”抽取，保证初始时两个人绝对不会挤在同一个点
        List<GameObject> availablePOIs = new List<GameObject>(allPOIs);

        // 防呆设计：如果想生成的人数比地砖（POI）还多，就按 POI 的最大数量来，防止越界崩溃
        int actualSpawnCount = Mathf.Min(targetPedestrianCount, availablePOIs.Count);

        for (int i = 0; i < actualSpawnCount; i++)
        {
            if (pedestrianPrefabs.Count == 0) break;

            // 3. 随机抽一个兴趣点，抽完就从名单里撕掉
            int randomIndex = Random.Range(0, availablePOIs.Count);
            Transform spawnPoint = availablePOIs[randomIndex].transform;
            availablePOIs.RemoveAt(randomIndex); // 核心：抽走！

            // 4. 随机挑一个款式的小人预制体
            GameObject selectedPrefab = pedestrianPrefabs[Random.Range(0, pedestrianPrefabs.Count)];

            // 5. 在该点生成小人
            GameObject newPedestrian = Instantiate(selectedPrefab, spawnPoint.position, Quaternion.identity);

            // ---------------- 🚨 NavMesh 核心防掉落魔法 ----------------
            // 因为 PCG 生成的点可能稍微悬空或者陷入地下 0.1 米
            // NavMeshAgent 如果一出生不在蓝色的网格上，就会直接报错瘫痪！
            // 所以我们用 SamplePosition 把空中/地下的点，像磁铁一样“吸附”到最近的 NavMesh 表面上
            NavMeshHit hit;
            if (NavMesh.SamplePosition(spawnPoint.position, out hit, 2.0f, NavMesh.AllAreas))
            {
                newPedestrian.transform.position = hit.position; // 完美踩在地上
            }
            // -------------------------------------------------------------

            // 稍微等待一小会儿再生成下一个，保证游戏初始帧极其丝滑
            if (spawnDelay > 0)
            {
                yield return new WaitForSeconds(spawnDelay);
            }
        }

        Debug.Log($"生成 {actualSpawnCount} 个行人！");
    }
}