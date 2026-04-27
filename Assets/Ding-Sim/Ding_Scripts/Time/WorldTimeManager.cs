using UnityEngine;

public class WorldTimeManager : MonoBehaviour
{
    public static WorldTimeManager Instance;

    [Header("Time Setting")]
    [Range(0f, 20f)]
    public float timeMultiplier = 1f; // speed
    public float currentHour = 8.0f;    // 0-24

    public Light sunLight;

    public System.Action<int> OnHourChanged; 

    private int lastHour = -1;

    void Awake() => Instance = this;

    void Update()
    {
        Time.timeScale = timeMultiplier;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        currentHour += (Time.deltaTime * timeMultiplier * 1000) / 3600f;
        if (currentHour >= 24) currentHour = 0;

        int hourInt = Mathf.FloorToInt(currentHour);
        if (hourInt != lastHour)
        {
            lastHour = hourInt;
            OnHourChanged?.Invoke(hourInt);
        }

        // relate to lighting (URP/HDRP)
        UpdateEnvironmentLight();
    }

    void UpdateEnvironmentLight()
    {
        if (sunLight == null) return;

        float sunRotation = (currentHour / 24f) * 360f - 90f;
        sunLight.transform.rotation = Quaternion.Euler(sunRotation, 170f, 0f);

        sunLight.intensity = (currentHour > 6 && currentHour < 18) ? 1.0f : 0.1f;
    }

    public void OnTimeSliderChanged(float newValue)
    {
        timeMultiplier = newValue;
    }
}