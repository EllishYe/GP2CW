using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

[System.Serializable]
public struct PedestrianSpawnData
{
    public GameObject PedestrianPrefab;
    [Range(0f, 100f)]
    public float spawnWeight;
}

public class PedestrianManager : MonoBehaviour
{
    [Header("pedestrian prefabs")]
    public List<PedestrianSpawnData> PedestrianSpawnPool;

    [Header("setting")]
    public int PedestrianCount = 100;
    public TextMeshProUGUI pedestrianCountText;
    public float spawnDelay = 1.2f;

    private Transform _pedestrianRoot;
    private List<GameObject> _activePedestrians = new List<GameObject>();
    private List<Transform> _homePoints = new List<Transform>();

    private void Start()
    {
        pedestrianCountText.text = $"Pedestrian Count: {PedestrianCount}";
    }

    private GameObject GetRandomPesByWeight()
    {
        float totalWeight = 0f;
        foreach (var data in PedestrianSpawnPool)
        {
            totalWeight += data.spawnWeight;
        }

        float randomPoint = Random.Range(0, totalWeight);

        foreach (var data in PedestrianSpawnPool)
        {
            randomPoint -= data.spawnWeight;
            if (randomPoint <= 0)
            {
                return data.PedestrianPrefab;
            }
        }
        return PedestrianSpawnPool[0].PedestrianPrefab;
    }

    public void OnPedestrianSliderChanged(float newValue)
    {
        PedestrianCount = Mathf.RoundToInt(newValue); 
        if (pedestrianCountText != null)
        {
            pedestrianCountText.text = $"Pedestrian Count: {PedestrianCount}";
        }
    }



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
            Debug.LogError("no home POI");
            return;
        }



        StartCoroutine(SpawnAllAtHomeRoutine());
        WorldTimeManager.Instance.OnHourChanged += HandleHourChanged;
    }


    private void HandleHourChanged(int hour)
    {
        if (hour == 7)
        {
            //Debug.Log("大家起床去上班了！");
            foreach (GameObject npc in _activePedestrians)
            {
                npc.GetComponent<PedestrianAI>().GoToWork();
            }
        }
        else if (hour == 18)
        {
            //Debug.Log("下班回家！");
            foreach (GameObject npc in _activePedestrians)
            {
                npc.GetComponent<PedestrianAI>().GoHome();
            }
        }
    }

    private IEnumerator SpawnAllAtHomeRoutine()
    {
        for (int i = 0; i < PedestrianCount; i++)
        {
            Transform homePoint = _homePoints[i % _homePoints.Count];
            GameObject newPedestrian = Instantiate(GetRandomPesByWeight(), homePoint.position, Quaternion.identity);
            newPedestrian.transform.SetParent(_pedestrianRoot);
            newPedestrian.GetComponent<PedestrianAI>().InitHome(homePoint.position);
            _activePedestrians.Add(newPedestrian);

            yield return null; 
        }
    }

    private void SendEveryoneHome()
    {
        Debug.Log("time to go home");
        foreach (GameObject npc in _activePedestrians)
        {
            if (npc != null)
            {
                PedestrianAI ai = npc.GetComponent<PedestrianAI>();
                if (ai != null) ai.GoHome();
            }
        }
    }

    public void UnregisterPedestrian(GameObject npc)
    {
        if (_activePedestrians.Contains(npc))
        {
            _activePedestrians.Remove(npc);
        }
    }

    private void OnDestroy()
    {
        if (WorldTimeManager.Instance != null)
        {
            WorldTimeManager.Instance.OnHourChanged -= HandleHourChanged;
        }
    }


    private IEnumerator SpawnRoutine()
    {
        for (int i = 0; i < PedestrianCount; i++)
        {
            Transform spawnPoint = _homePoints[i % _homePoints.Count];

            Vector3 spawnPos = spawnPoint.position + Random.insideUnitSphere * 1.5f;
            spawnPos.y = spawnPoint.position.y;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(spawnPos, out hit, 2.0f, NavMesh.AllAreas))
            {
                GameObject selectedPrefab = GetRandomPesByWeight();
                GameObject newPedestrian = Instantiate(selectedPrefab, hit.position, Quaternion.identity);

                newPedestrian.transform.SetParent(_pedestrianRoot);

                PedestrianAI ai = newPedestrian.GetComponent<PedestrianAI>();
                if (ai != null)
                {
                    ai.InitHome(spawnPoint.position);
                }

                _activePedestrians.Add(newPedestrian);
            }

            yield return new WaitForSeconds(spawnDelay);
        }
    }
}