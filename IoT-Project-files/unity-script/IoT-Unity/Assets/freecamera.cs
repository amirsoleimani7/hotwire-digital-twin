using UnityEngine;
using UnityEngine.InputSystem;

[AddComponentMenu("Camera/ModelViewerCamera")]
public class ModelViewerCamera : MonoBehaviour
{
    [Tooltip("The point to keep in the center of view")]
    public Transform target;
    [Tooltip("Min/max distance to target")]
    public float minDistance = 0.5f, maxDistance = 50f;
    public float zoomSpeed = 5f;
    public float rotationSpeed = 200f;
    public float panSpeed = 1f;

    float distance;
    Vector2 rotation;  // X = pitch, Y = yaw

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("ModelViewerCamera: assign a target!");
            enabled = false;
            return;
        }

        // Compute initial distance & rotation from current transform
        Vector3 offset = transform.position - target.position;
        distance = offset.magnitude;
        Quaternion initialRot = Quaternion.LookRotation(-offset.normalized, Vector3.up);
        Vector3 e = initialRot.eulerAngles;
        rotation = new Vector2(e.x, e.y);
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        // --- ZOOM ---
        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > Mathf.Epsilon)
        {
            distance = Mathf.Clamp(distance - scroll * zoomSpeed * Time.deltaTime, minDistance, maxDistance);
        }

        // --- ORBIT (left button) ---
        if (mouse.leftButton.isPressed)
        {
            Vector2 d = mouse.delta.ReadValue();
            rotation.y += d.x * rotationSpeed * Time.deltaTime;
            rotation.x -= d.y * rotationSpeed * Time.deltaTime;
            rotation.x = Mathf.Clamp(rotation.x, -89f, 89f);
        }

        // --- PAN (right button) ---
        //if (mouse.rightButton.isPressed)
        //{
        //    Vector2 d = mouse.delta.ReadValue() * panSpeed * Time.deltaTime;
        //    Vector3 right = transform.right * -d.x;
        //    Vector3 up = transform.up * -d.y;
        //    target.position += right + up;
        //}

        // --- APPLY TRANSFORM ---
        Quaternion rot = Quaternion.Euler(rotation.x, rotation.y, 0f);
        Vector3 dir = rot * Vector3.back;
        transform.position = target.position + dir * distance;
        transform.rotation = rot;

        // --- Clamp camera Y position ---
        Vector3 camPos = transform.position;
        camPos.y = Mathf.Clamp(camPos.y, 5f, 28f);
        transform.position = camPos;
    }
}
