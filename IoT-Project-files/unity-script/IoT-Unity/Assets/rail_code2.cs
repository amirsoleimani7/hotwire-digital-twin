using UnityEngine;

public class FollowY1 : MonoBehaviour
{
    public Transform targetCube; // Assign the cube you want to follow

    void Update()
    {
        if (targetCube != null)
        {
            Vector3 pos = transform.position;
            pos.y = targetCube.position.y; // Match Y position
            transform.position = pos;
        }
    }
}
