using UnityEngine;

public class FollowY : MonoBehaviour
{
    public Transform target; // Assign the object to follow

    void Update()
    {
        if (target != null)
        {
            Vector3 pos = transform.position;
            pos.y = target.position.y;
            transform.position = pos;
        }
    }
}
    