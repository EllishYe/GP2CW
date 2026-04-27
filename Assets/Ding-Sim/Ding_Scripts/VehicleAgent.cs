using System.Collections.Generic;
using UnityEngine;

public class VehicleAgent : MonoBehaviour
{
    public enum CarState
    {
        Driving,        
        Braking,        
        WaitingAtLight, 
        Yielding        
    }

    [Header("状态")]
    public CarState currentState = CarState.Driving; 


    [Header("Driving parameters")]
    public float maxSpeed;       
    public float turnSpeed = 10f;
    private float currentSpeed = 0f;  

    [Header("sensor")]
    public float sensorLength = 4f;   
    public Vector3 sensorOffset = new Vector3(1, 0.5f, 1.8f); 

    private TrafficGraph cityGraph;
    private LaneData currentLane;
    private int currentPointIndex = 0;

    private Rigidbody rb;

    void Start()
    {
        maxSpeed = Random.Range(8f, 16f);
        rb = GetComponent<Rigidbody>();
        //rb.centerOfMass = new Vector3(1f, -0.5f, 1f);
    }

    public void InitVehicle(TrafficGraph graph, LaneData startLane)
    {
        cityGraph = graph;
        currentLane = startLane;
        if (currentLane != null && currentLane.pathPoints.Count > 0)
        {
            Vector3 startPos = currentLane.pathPoints[0].position;

            transform.position = new Vector3(startPos.x, transform.position.y, startPos.z);
            //transform.position = currentLane.pathPoints[0].position;
            currentPointIndex = 1;
        }
    }

    void FixedUpdate()
    {
        if (currentLane == null || rb == null) return;

        Sensor();
        CheckTrafficLight();

        switch (currentState)
        {
            case CarState.Driving:
                currentSpeed = Mathf.Lerp(currentSpeed, maxSpeed, Time.fixedDeltaTime * 2f);
                DriveAlongLane();
                break;

            case CarState.Braking:
            case CarState.WaitingAtLight:
            case CarState.Yielding:
                currentSpeed = Mathf.Lerp(currentSpeed, 0, Time.fixedDeltaTime * 10f); // quikly braking

                if (currentSpeed < 2f)
                {
                    currentSpeed = 0f;
                }
                DriveAlongLane(); 
                break;
        }
    }

    private void Sensor()
    {
        bool isPathClear = true;
        Vector3 sensorStartPos = transform.TransformPoint(sensorOffset);

        float castRadius = 1.5f;
        RaycastHit hit;

        if (Physics.SphereCast(sensorStartPos, castRadius, transform.forward, out hit, sensorLength))
        {
            if (hit.collider.CompareTag("Vehicle"))
            {
                isPathClear = false;
                currentState = CarState.Braking;
                Debug.DrawRay(sensorStartPos, transform.forward * hit.distance, Color.red);
            }
            
            else if (hit.collider.CompareTag("Pedestrian"))
            {
                isPathClear = false;
                currentState = CarState.Yielding;
                Debug.DrawRay(sensorStartPos, transform.forward * hit.distance, Color.yellow); 
            }
        }

        if (isPathClear)
        {

            Debug.DrawRay(sensorStartPos, transform.forward * sensorLength, Color.green);

            if (currentState == CarState.Braking || currentState == CarState.Yielding)
            {
                currentState = CarState.Driving; 
            }
        }
    }

    private void CheckTrafficLight()
    {

        if (currentPointIndex == currentLane.pathPoints.Count - 1)
        {
            if (currentLane.isRedLight)
            {
                Vector3 stopPos = currentLane.pathPoints[currentPointIndex].position;
                float distanceToStopLine = Vector3.Distance(transform.position, stopPos);

                //breaking distance in front of intersection
                if (distanceToStopLine < 8f)
                {
                    currentState = CarState.WaitingAtLight;
                    return; 
                }
            }
        }
        if (currentState == CarState.WaitingAtLight && !currentLane.isRedLight)
        {
            currentState = CarState.Driving;
        }
    }



    private void DriveAlongLane()
    {
        Vector3 targetPoint = currentLane.pathPoints[currentPointIndex].position;
        Vector3 flatTarget = new Vector3(targetPoint.x, rb.position.y, targetPoint.z);

        Vector3 nextPosition = Vector3.MoveTowards(rb.position, flatTarget, currentSpeed * Time.fixedDeltaTime);
        rb.MovePosition(nextPosition);

        Vector3 direction = (flatTarget - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, lookRotation, Time.fixedDeltaTime * turnSpeed));
        }

        if (Vector3.Distance(transform.position, flatTarget) < 1.0f)
        {
            currentPointIndex++;
            if (currentPointIndex >= currentLane.pathPoints.Count)
            {
                SwitchToNextLane();
            }
        }
    }

    private void SwitchToNextLane()
    {
        if (currentLane.nextLanes != null && currentLane.nextLanes.Count > 0)
        {
            int randomChoice = Random.Range(0, currentLane.nextLanes.Count);
            currentLane = currentLane.nextLanes[randomChoice];
            currentPointIndex = 0;
        }
        else
        {
            currentLane = null;
        }
    }


    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Vector3 sensorStartPos = transform.TransformPoint(sensorOffset);
        float castRadius = 1f;

        if (currentState == CarState.Braking)
            Gizmos.color = Color.red;
        else if (currentState == CarState.Yielding)
            Gizmos.color = Color.yellow;
        else
            Gizmos.color = Color.green;

        Gizmos.DrawWireSphere(sensorStartPos, castRadius);

        Vector3 sensorEndPos = sensorStartPos + transform.forward * sensorLength;
        Gizmos.DrawWireSphere(sensorEndPos, castRadius);

        Gizmos.DrawLine(sensorStartPos + transform.right * castRadius, sensorEndPos + transform.right * castRadius);
        Gizmos.DrawLine(sensorStartPos - transform.right * castRadius, sensorEndPos - transform.right * castRadius);
    }
}