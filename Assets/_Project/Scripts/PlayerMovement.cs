using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 7f;
    [SerializeField] private float sprintSpeed = 11f;
    [SerializeField] private float jumpHeight = 2.2f;
    [SerializeField] private float gravity = -24f;

    [Header("Camera & Look Settings")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float mouseSensitivity = 0.15f;
    [SerializeField] private float minPitch = -85f;
    [SerializeField] private float maxPitch = 85f;

    private CharacterController controller;
    private Vector3 velocity;
    private float pitch = 0f;
    private bool isGrounded;

    private void Awake()
    {
        // Smooth frame pacing and prevent thermal throttling / high battery drain on laptops
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 1;

        controller = GetComponent<CharacterController>();
        LockCursor();
    }

    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResetVelocity()
    {
        velocity = Vector3.zero;
    }

    private void Update()
    {
        // Keep cursor unlocked and stop movement when victory screen is shown
        if (GameUI.Instance != null && !GameUI.Instance.IsRunning)
        {
            if (Cursor.lockState != CursorLockMode.None)
            {
                UnlockCursor();
            }
            return;
        }

        // Toggle cursor lock with Escape
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (Cursor.lockState == CursorLockMode.Locked) UnlockCursor();
            else LockCursor();
        }
#else
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Cursor.lockState == CursorLockMode.Locked) UnlockCursor();
            else LockCursor();
        }
#endif

        if (Cursor.lockState != CursorLockMode.Locked) return;

        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        HandleLook();
        HandleMovement();
    }

    private void HandleLook()
    {
        float mouseX = 0f;
        float mouseY = 0f;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            mouseX = delta.x * mouseSensitivity;
            mouseY = delta.y * mouseSensitivity;
        }
#else
        mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * 10f;
        mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * 10f;
#endif

        transform.Rotate(Vector3.up * mouseX);

        if (cameraTransform != null)
        {
            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }

    private void HandleMovement()
    {
        float x = 0f;
        float z = 0f;
        bool jump = false;
        bool sprint = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) z += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) z -= 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
            jump = Keyboard.current.spaceKey.wasPressedThisFrame;
            sprint = Keyboard.current.leftShiftKey.isPressed;
        }
#else
        x = Input.GetAxisRaw("Horizontal");
        z = Input.GetAxisRaw("Vertical");
        jump = Input.GetButtonDown("Jump");
        sprint = Input.GetKey(KeyCode.LeftShift);
#endif

        Vector3 move = (transform.right * x + transform.forward * z).normalized;
        float speed = sprint ? sprintSpeed : walkSpeed;
        controller.Move(move * speed * Time.deltaTime);

        if (jump && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
