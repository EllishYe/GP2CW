using UnityEngine;

public class EditorCameraController : MonoBehaviour
{
    [Header("移动设置 (Movement)")]
    public float baseMoveSpeed = 20f;     // 基础移动速度
    public float shiftMultiplier = 3f;    // 按住 Shift 时的加速倍率
    public float panSpeed = 20f;          // 鼠标中键平移速度
    public float zoomSpeed = 50f;         // 滚轮缩放速度

    [Header("旋转设置 (Rotation)")]
    public float lookSensitivity = 2f;    // 鼠标观察灵敏度

    // 内部变量：记录当前的俯仰角(Pitch)和偏航角(Yaw)
    private float yaw = 0f;
    private float pitch = 0f;

    void Start()
    {
        // 初始化时，获取相机当前的真实旋转角度，防止刚点击时相机视角突然跳变
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
    }

    void Update()
    {
        HandleRotationAndMovement();
        HandlePanning();
        HandleZooming();
    }

    /// <summary>
    /// 处理右键旋转视角 和 WASD 移动
    /// </summary>
    private void HandleRotationAndMovement()
    {
        // 只有按住鼠标右键时，才能旋转视角和移动 (符合 Editor 习惯)
        if (Input.GetMouseButton(1))
        {
            // 1. 处理视角旋转
            yaw += Input.GetAxis("Mouse X") * lookSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * lookSensitivity;

            // 限制俯仰角，防止相机上下翻转导致眩晕 (-90度到90度)
            pitch = Mathf.Clamp(pitch, -90f, 90f);

            transform.eulerAngles = new Vector3(pitch, yaw, 0f);

            // 2. 处理 WASD 移动
            float currentSpeed = baseMoveSpeed;
            if (Input.GetKey(KeyCode.LeftShift))
            {
                currentSpeed *= shiftMultiplier; // 按住 Shift 提速
            }

            Vector3 moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));

            // 将局部的输入方向转换为世界空间方向，并叠加速度和时间
            transform.Translate(moveDirection * currentSpeed * Time.deltaTime, Space.Self);

            // 如果按 Q 或 E，可以垂直升降
            if (Input.GetKey(KeyCode.E)) { transform.Translate(Vector3.up * currentSpeed * Time.deltaTime, Space.World); }
            if (Input.GetKey(KeyCode.Q)) { transform.Translate(Vector3.down * currentSpeed * Time.deltaTime, Space.World); }
        }
    }

    /// <summary>
    /// 处理鼠标中键平移 (Pan)
    /// </summary>
    private void HandlePanning()
    {
        if (Input.GetMouseButton(2)) // 鼠标中键
        {
            float panX = -Input.GetAxis("Mouse X") * panSpeed * Time.deltaTime;
            float panY = -Input.GetAxis("Mouse Y") * panSpeed * Time.deltaTime;

            // 沿着相机的局部 X 和 Y 轴移动，模拟“抓取”画面的感觉
            transform.Translate(new Vector3(panX, panY, 0f), Space.Self);
        }
    }

    /// <summary>
    /// 处理鼠标滚轮缩放 (Zoom)
    /// </summary>
    private void HandleZooming()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            // 沿着相机当前的正前方移动
            transform.Translate(Vector3.forward * scroll * zoomSpeed * Time.deltaTime, Space.Self);
        }
    }
}