using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class PedestrianAI : MonoBehaviour
{
    public float homeArrivalRadius = 10f;

    [Header("Movement Range")]
    public float wanderRadius = 5f; 

    private Vector3 _currentCenterPos; 
    private bool _isWorking = false;

    private NavMeshAgent agent;
    private Animator animator;

    private bool _isGoingHome = false;
    private Vector3 _myHomePos;

    private static Transform[] allOfficePOIs;
    private Transform currentDestination;

    void Start()
    {
        //agent = GetComponent<NavMeshAgent>();
        //animator = GetComponent<Animator>();

        if (allOfficePOIs == null || allOfficePOIs.Length == 0)
        {
            // get all items with PedestrianPOI 
            PedestrianPOI[] poiScripts = Object.FindObjectsByType<PedestrianPOI>(FindObjectsSortMode.None);
            List<Transform> officeList = new List<Transform>();

            // get office POI
            foreach (var poi in poiScripts)
            {
                if (poi.type == PedestrianPOI.POIType.Office) 
                {
                    officeList.Add(poi.transform);
                }
            }

            allOfficePOIs = officeList.ToArray();

            if (allOfficePOIs.Length == 0)
            {
                Debug.LogError("no office POI");
            }
        }

        SetLogicalDestination();
    }

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }


    void Update()
    {
        animator.SetFloat("Speed", agent.velocity.magnitude);

        if (agent != null && agent.isOnNavMesh)
        {         
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f)
            {
               
                Invoke("WanderToNewSpot", Random.Range(2f, 5f));
            }
        }
    }

    private void SetLogicalDestination()
    {
        if (allOfficePOIs == null || allOfficePOIs.Length == 0) return;

        Transform bestTarget = null;
        int maxAttempts = 15;

        for (int i = 0; i < maxAttempts; i++)
        {

            Transform randomPOI = allOfficePOIs[Random.Range(0, allOfficePOIs.Length)];
            float distance = Vector3.Distance(transform.position, randomPOI.position);

            if (distance > 5f)
            {
                bestTarget = randomPOI;
                break;
            }
        }

        if (bestTarget == null) bestTarget = allOfficePOIs[Random.Range(0, allOfficePOIs.Length)];

        currentDestination = bestTarget;
        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(currentDestination.position);
        }
    }

    public void InitHome(Vector3 homePosition)
    {
        _myHomePos = homePosition;
        _currentCenterPos = _myHomePos; 
        _isGoingHome = true;

        WanderToNewSpot();
    }

    public void GoToWork()
    {
        _isGoingHome = false;
        _isWorking = true;
        SetLogicalDestination(); 
        _currentCenterPos = currentDestination.position;
    }

    public void GoHome()
    {
        _isWorking = false;
        _isGoingHome = true;
        _currentCenterPos = _myHomePos;
        agent.SetDestination(_myHomePos);
    }

    private void WanderToNewSpot()
    {
        if (!agent.isOnNavMesh) return;

        Vector3 randomDest = _currentCenterPos + Random.insideUnitSphere * wanderRadius;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDest, out hit, wanderRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }
}