using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class PedestrianAI : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;

    [Header("漫游设置")]
    public float wanderRadius = 10f; // 漫游半径（10米内随机溜达）
    public float minWaitTime = 2f;   // 到了目的地发呆的最短时间
    public float maxWaitTime = 5f;   // 发呆的最长时间

    private float waitTimer;
    private bool isWaiting = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // 游戏开始时，先原地发呆一小会儿再开始走
        waitTimer = Random.Range(0f, maxWaitTime);
        isWaiting = true;
    }

    void Update()
    {
        // 🚨 核心魔法：把双腿真实的移动速度，实时发送给动画神经！
        animator.SetFloat("Speed", agent.velocity.magnitude);

        // 如果在发呆等待
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0)
            {
                isWaiting = false;
                SetNewRandomDestination(); // 时间到了，找个新地方去
            }
        }
        else
        {
            // 如果还没到目标点，画一条红线方便你观察它的目的地！
            if (agent.hasPath)
            {
                Debug.DrawLine(transform.position + Vector3.up, agent.destination + Vector3.up, Color.red);
            }

            // 如果快走到目的地了（剩余距离 < 停止距离）
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                isWaiting = true; // 停下来
                waitTimer = Random.Range(minWaitTime, maxWaitTime); // 重新随机发呆时间
            }
        }
    }

    // 找随机目标点的方法
    private void SetNewRandomDestination()
    {
        // 在自己周围的一个球体范围内随机抓一个点
        Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * wanderRadius;
        randomDirection += transform.position;

        NavMeshHit hit;
        // 把空中随机点“拍”在蓝色的 NavMesh 网格上
        if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, 1))
        {
            agent.SetDestination(hit.position); // 下达走路命令！
        }
    }
}