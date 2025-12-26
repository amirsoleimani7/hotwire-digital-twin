using System;
using System.Net;
using System.Net.Sockets;
using TMPro;
using System.Threading;
using UnityEngine;
using System.Text;
using UnityEngine.UI;

public class TcpTempReceiver : MonoBehaviour
{
    public TMP_Text tempText;
    public TMP_Text pressureText;
    public TMP_Text altitudeText;

    private TcpListener listener;
    private Thread listenerThread;

    private string latestTemp = "";
    private string latestPres = "";
    private string latestAlt = "";

    private readonly object lockObj = new object();

    void Start()
    {
        listenerThread = new Thread(ListenForData);
        listenerThread.IsBackground = true;
        listenerThread.Start();
    }

    private void ListenForData()
    {
        try
        {
            listener = new TcpListener(IPAddress.Any, 5007);
            listener.Start();
            Debug.Log("TCP Temp Receiver started on port 5007");

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
                        Debug.Log("Received Temp Data: " + received);

                        ParseTempData(received);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.Log("TCP Temp Error: " + e.Message);
        }
    }

    private void ParseTempData(string message)
    {
        try
        {
            if (message.StartsWith("temp_data;"))
                message = message.Substring("temp_data;".Length);

            string[] parts = message.Split(';');
            if (parts.Length >= 3)
            {
                lock (lockObj)
                {
                    latestTemp = parts[0];
                    latestPres = parts[1];
                    latestAlt = parts[2];
                }
            }
        }
        catch (Exception e)
        {
            Debug.Log("Temp Parse Error: " + e.Message);
        }
    }

    void Update()
    {
        lock (lockObj)
        {
            if (tempText != null) tempText.text = "Temp: " + latestTemp + "°C";
            if (pressureText != null) pressureText.text = "Pressure: " + latestPres + " hPa";
            if (altitudeText != null) altitudeText.text = "Altitude: " + latestAlt + " m";
        }
    }

    void OnApplicationQuit()
    {
        listener?.Stop();
        if (listenerThread != null && listenerThread.IsAlive)
            listenerThread.Abort();
    }
}
