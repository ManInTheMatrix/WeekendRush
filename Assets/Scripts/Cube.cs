using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Cube : MonoBehaviour
{
    //public float scrollSpeed = 2;
   // public float doubleClickDelay = 0.4f;

    public float rotationSpeed = 1f;
    float currentTime = 0;
    Vector2 currentPosition = Vector2.zero;

    // Start is called before the first frame update
    void Start()
    {
        currentPosition = Input.mousePosition;
    }
    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }
    // Update is called once per frame
    void Update()
    {
        var xInput = Input.GetAxis("Mouse X") * rotationSpeed;
        var yInput = Input.GetAxis("Mouse Y") * rotationSpeed;

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
