using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class PedestrianAI : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;

    [Header("真实通勤设置")]
    public float minWaitTime = 1f;   // 等红绿灯/发呆的最短时间
    public float maxWaitTime = 4f;   // 发呆的最长时间
    public float maxTravelDistance = 50f; // 限制单次出行的最远距离，避免跨越全城走断腿

    private float waitTimer;
    private bool isWaiting = false;

    // 全城的兴趣点缓存
    private static Transform[] allCityPOIs;
    private Transform currentDestination;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // 1. 性能优化：全城小人共享一份 POI 名单，不需要每个人都去搜一遍
        if (allCityPOIs == null || allCityPOIs.Length == 0)
        {
            GameObject[] poiObjects = GameObject.FindGameObjectsWithTag("PedestrianPOI");
            allCityPOIs = new Transform[poiObjects.Length];
            for (int i = 0; i < poiObjects.Length; i++)
            {
                allCityPOIs[i] = poiObjects[i].transform;
            }

            if (allCityPOIs.Length == 0)
            {
                Debug.LogError("🚨 城市里没有行人兴趣点！请在人行道上放置空物体并打上 'PedestrianPOI' 标签！");
            }
        }

        waitTimer = Random.Range(0f, maxWaitTime);
        isWaiting = true;
    }

    void Update()
    {
        animator.SetFloat("Speed", agent.velocity.magnitude);

        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0)
            {
                isWaiting = false;
                SetLogicalDestination(); // 时间到了，出发去下一个目标！
            }
        }
        else
        {
            if (agent.hasPath && currentDestination != null)
            {
                // 用绿线画出它的长途通勤目标
                Debug.DrawLine(transform.position + Vector3.up, currentDestination.position + Vector3.up, Color.green);
            }

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                isWaiting = true;
                waitTimer = Random.Range(minWaitTime, maxWaitTime);
            }
        }
    }

    // 🚨 核心逻辑：找一个符合人类逻辑的目的地
    private void SetLogicalDestination()
    {
        if (allCityPOIs == null || allCityPOIs.Length == 0) return;

        Transform bestTarget = null;
        int maxAttempts = 10; // 找 10 次，找不到合适的就随便去一个，防止死循环

        for (int i = 0; i < maxAttempts; i++)
        {
            // 随机抽一个全城兴趣点
            Transform randomPOI = allCityPOIs[Random.Range(0, allCityPOIs.Length)];

            // 1. 距离过滤：不能选自己脚下现在的点，也不能选太远的点
            float distance = Vector3.Distance(transform.position, randomPOI.position);

            if (distance > 2f && distance < maxTravelDistance)
            {
                bestTarget = randomPOI;
                break;
            }
        }

        // 如果上面没找到合适的，就兜底选第一个
        if (bestTarget == null) bestTarget = allCityPOIs[Random.Range(0, allCityPOIs.Length)];

        currentDestination = bestTarget;
        agent.SetDestination(currentDestination.position);
    }
}