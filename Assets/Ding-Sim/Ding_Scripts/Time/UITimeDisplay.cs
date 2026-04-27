using UnityEngine;
using TMPro;

public class UITimeDisplay : MonoBehaviour
{
    public TextMeshProUGUI timeText;

    void Update()
    {
        if (WorldTimeManager.Instance == null) return;

        float totalHours = WorldTimeManager.Instance.currentHour;

        int hours = Mathf.FloorToInt(totalHours);
        int minutes = Mathf.FloorToInt((totalHours - hours) * 60f);

        timeText.text = string.Format("{0:00}:{1:00}", hours, minutes);

        // change color
        if (hours >= 19 || hours <= 6)
            timeText.color = Color.black;
        else
            timeText.color = Color.white;
    }
}