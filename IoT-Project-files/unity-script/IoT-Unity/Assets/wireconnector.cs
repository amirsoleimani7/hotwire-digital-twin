using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class WireConnector : MonoBehaviour
{
    [Header("Assign your two objects here")]
    public Transform startObject;
    public Transform endObject;

    [Header("Appearance")]
    public float wireWidth = 0.02f;
    public Color wireColor = Color.yellow;

    private LineRenderer line;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        SetupLine();
    }

    void SetupLine()
    {
        transform.localScale = Vector3.one;

        line.useWorldSpace = true;
        line.positionCount = 2;
        line.startWidth = wireWidth;
        line.endWidth = wireWidth;

        var mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = wireColor;
        line.material = mat;
    }

    void LateUpdate()
    {
        if (startObject == null || endObject == null) return;

        line.SetPosition(0, startObject.position);
        line.SetPosition(1, endObject.position);
    }
}
