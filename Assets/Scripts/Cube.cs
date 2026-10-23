using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cube : MonoBehaviour
{
    public float rotationSpeed = 1f;
    float currentTime = 0;
    Vector2 currentPosition = Vector2.zero;

    void Start()
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Mouse.current != null)
            currentPosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
#elif ENABLE_LEGACY_INPUT_MANAGER
        currentPosition = Input.mousePosition;
#endif
    }

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        Vector2 stick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        if (stick.sqrMagnitude < 0.01f)
            stick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);

        float mouseX = 0f;
        float mouseY = 0f;
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Mouse.current != null)
        {
            Vector2 delta = UnityEngine.InputSystem.Mouse.current.delta.ReadValue();
            mouseX = delta.x * 0.1f;
            mouseY = delta.y * 0.1f;
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        mouseX = Input.GetAxis("Mouse X");
        mouseY = Input.GetAxis("Mouse Y");
#endif

        var xInput = (mouseX + stick.x * 2.5f) * rotationSpeed;
        var yInput = (mouseY + stick.y * 2.5f) * rotationSpeed;

        Vector3 cubeRot = Vector3.zero;
        cubeRot.x = yInput;
        cubeRot.y = -xInput;

        transform.Rotate(cubeRot, Space.World);
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
    }
}
