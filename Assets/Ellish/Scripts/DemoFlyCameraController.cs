using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public class DemoFlyCameraController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 15f;
    [SerializeField] private float fastMoveMultiplier = 3f;
    [SerializeField] private bool requireRightMouseForMovement = true;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Look")]
    [SerializeField] private float lookSensitivity = 0.15f;
    [SerializeField] private float minPitch = -89f;
    [SerializeField] private float maxPitch = 89f;
    [SerializeField] private bool lockCursorWhileLooking = true;

    private float yaw;
    private float pitch;

    private void Awake()
    {
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = NormalizeAngle(angles.x);
    }

    private void Update()
    {
        bool isLooking = IsRightMouseHeld();

        if (isLooking)
        {
            UpdateLook();
        }

        if (!requireRightMouseForMovement || isLooking)
        {
            UpdateMovement();
        }

        UpdateCursorState(isLooking);
    }

    private void OnDisable()
    {
        if (lockCursorWhileLooking)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void UpdateLook()
    {
        Vector2 mouseDelta = GetMouseDelta();
        yaw += mouseDelta.x * lookSensitivity;
        pitch -= mouseDelta.y * lookSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void UpdateMovement()
    {
        Vector3 input = Vector3.zero;

        if (IsKeyHeld(KeyBinding.Forward)) input.z += 1f;
        if (IsKeyHeld(KeyBinding.Backward)) input.z -= 1f;
        if (IsKeyHeld(KeyBinding.Right)) input.x += 1f;
        if (IsKeyHeld(KeyBinding.Left)) input.x -= 1f;
        if (IsKeyHeld(KeyBinding.Up)) input.y += 1f;
        if (IsKeyHeld(KeyBinding.Down)) input.y -= 1f;

        if (input.sqrMagnitude <= 0f)
        {
            return;
        }

        input.Normalize();

        float currentSpeed = moveSpeed;
        if (IsKeyHeld(KeyBinding.Fast))
        {
            currentSpeed *= fastMoveMultiplier;
        }

        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        Vector3 worldDirection =
            transform.right * input.x +
            transform.up * input.y +
            transform.forward * input.z;

        transform.position += worldDirection * currentSpeed * deltaTime;
    }

    private void UpdateCursorState(bool isLooking)
    {
        if (!lockCursorWhileLooking)
        {
            return;
        }

        Cursor.lockState = isLooking ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !isLooking;
    }

    private static float NormalizeAngle(float angle)
    {
        return angle > 180f ? angle - 360f : angle;
    }

    private static bool IsRightMouseHeld()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.rightButton.isPressed;
#else
        return Input.GetMouseButton(1);
#endif
    }

    private static Vector2 GetMouseDelta()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
#else
        return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
#endif
    }

    private static bool IsKeyHeld(KeyBinding binding)
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        return binding switch
        {
            KeyBinding.Forward => keyboard.wKey.isPressed,
            KeyBinding.Backward => keyboard.sKey.isPressed,
            KeyBinding.Right => keyboard.dKey.isPressed,
            KeyBinding.Left => keyboard.aKey.isPressed,
            KeyBinding.Up => keyboard.eKey.isPressed,
            KeyBinding.Down => keyboard.qKey.isPressed,
            KeyBinding.Fast => keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed,
            _ => false
        };
#else
        return binding switch
        {
            KeyBinding.Forward => Input.GetKey(KeyCode.W),
            KeyBinding.Backward => Input.GetKey(KeyCode.S),
            KeyBinding.Right => Input.GetKey(KeyCode.D),
            KeyBinding.Left => Input.GetKey(KeyCode.A),
            KeyBinding.Up => Input.GetKey(KeyCode.E),
            KeyBinding.Down => Input.GetKey(KeyCode.Q),
            KeyBinding.Fast => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift),
            _ => false
        };
#endif
    }

    private enum KeyBinding
    {
        Forward,
        Backward,
        Right,
        Left,
        Up,
        Down,
        Fast
    }
}
