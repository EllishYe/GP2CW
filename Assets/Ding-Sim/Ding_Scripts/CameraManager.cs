using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [Header("cameras")]
    public Camera godCamera; 
    public Camera carCamera; 

    void Start()
    {

        if (godCamera != null) godCamera.gameObject.SetActive(true);
        if (carCamera != null) carCamera.gameObject.SetActive(false);
    }

    public void SwitchCamera()
    {
        if (godCamera == null || carCamera == null) return;
        bool isGodCamActive = godCamera.gameObject.activeSelf;

        godCamera.gameObject.SetActive(!isGodCamActive);
        carCamera.gameObject.SetActive(isGodCamActive);

    }
}