using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class TcpServerUnity2 : MonoBehaviour
{
    [Header("Drag your single cube (or object) here")]
    public Transform objectToMove;

    [Header("Threshold Settings")]
    public float xThreshold = 100f;  // in cm
    public float yThreshold = 100f;
    public float xMax = 10f;         // in Unity units
    public float yMax = 10f;
    public float xMin = 0f;          // new minimum X
    public float yMin = 0f;          // new minimum Y

    private float lastDistance1 = 0f;  // controls Y
    private float lastDistance2 = 0f;  // controls X

    private readonly object lockObj = new object();
    private Vector3 initPos;

    private TcpListener listener;
    private Thread listenerThread;

    void Start()
    {
        if (objectToMove != null)
            initPos = objectToMove.position;

        listenerThread = new Thread(ListenForData);
        listenerThread.IsBackground = true;
        listenerThread.Start();
    }

    private void ListenForData()
    {
        try
        {
            listener = new TcpListener(IPAddress.Any, 5006);
            listener.Start();
            Debug.Log("TCP Server started on port 5005");

            while (true)
            {
                using (TcpClient client = listener.AcceptTcpClient())
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] buffer = new byte[1024];
                    int length;

                    while ((length = stream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        string received = Encoding.UTF8.GetString(buffer, 0, length);
                        Debug.Log("Received: " + received);
                        ParseDistances(received);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.Log("TCP Error: " + e.Message);
        }
    }

    private void ParseDistances(string message)
    {
        try
        {
            if (message.StartsWith("CUBE2:"))
                message = message.Substring("CUBE2:".Length);

            string[] parts = message.Split(';');
            foreach (string part in parts)
            {
                if (part.StartsWith("DIST1:"))
                {
                    string val = part.Replace("DIST1:", "").Trim();
                    if (float.TryParse(val, out float cm1))
                    {
                        lock (lockObj) { lastDistance1 = cm1; }
                    }
                }
                else if (part.StartsWith("DIST2:"))
                {
                    string val = part.Replace("DIST2:", "").Trim();
                    if (float.TryParse(val, out float cm2))
                    {
                        lock (lockObj) { lastDistance2 = cm2; }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.Log("Parse error: " + e.Message + "\nRaw message: " + message);
        }
    }

    void Update()
    {
        lock (lockObj)
        {
            if (objectToMove != null)
            {
                // Convert distance (cm) to 0–1 based on threshold
                float normalizedX = Mathf.Clamp01(lastDistance2 / xThreshold);
                float normalizedY = Mathf.Clamp01(lastDistance1 / yThreshold);

                // Map 0–1 to your desired Unity range, even with negative values
                float targetX = Mathf.Lerp(xMin, xMax, normalizedX);
                float targetY = Mathf.Lerp(yMin, yMax, normalizedY);

                Vector3 targetPos = new Vector3(targetX, targetY, initPos.z);
                float smoothing = 0.6f;
                objectToMove.position = Vector3.Lerp(objectToMove.position, targetPos, smoothing * Time.deltaTime);
            }
        }
    }   

    void OnApplicationQuit()
    {
        listener?.Stop();
        if (listenerThread != null && listenerThread.IsAlive)
            listenerThread.Abort();
    }
}
