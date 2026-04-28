using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class PedestrianAI : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;

    private bool _isGoingHome = false;
    private Vector3 _myHomePos;

    private static Transform[] allOfficePOIs;
    private Transform currentDestination;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

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

    void Update()
    {
        animator.SetFloat("Speed", agent.velocity.magnitude);

        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            if (agent.hasPath && currentDestination != null)
            {
                Debug.DrawLine(transform.position + Vector3.up, currentDestination.position + Vector3.up, Color.green);
            }


            if (!_isGoingHome && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {

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
    }

    // AFTER 18:00p.m.
    public void GoHome()
    {
        _isGoingHome = true;
        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(_myHomePos);
        }
    }
}