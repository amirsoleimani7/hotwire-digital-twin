using UnityEngine;

public class SnapFollowTarget : MonoBehaviour
{
    public Transform target; // Assign the object to follow

    void Update()
    {
        if (target != null)
        {
            // Snap instantly to the target's position (all axes)
            transform.position = target.position;
        }
    }
}