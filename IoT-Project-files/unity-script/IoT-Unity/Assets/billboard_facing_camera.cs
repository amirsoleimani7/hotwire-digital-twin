using UnityEngine;

public class Billboard : MonoBehaviour
{
    void Update()
    {
        if (Camera.main != null)
        {
            transform.LookAt(Camera.main.transform);
            transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
        }
    }
}
