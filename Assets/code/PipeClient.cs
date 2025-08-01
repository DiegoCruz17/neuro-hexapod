using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using UnityEngine;
using System.Collections.Generic;

public class PipeClient : MonoBehaviour
{
    private NamedPipeClientStream pipeClient;
    private StreamWriter writer;
    private bool isConnected = false;

    [Header("Settings")]
    public bool debugMode = false;
    public int connectionTimeout = 3000;

    void Start()
    {
        ConnectToPipe();
    }

    void ConnectToPipe()
    {
        try
        {
            pipeClient = new NamedPipeClientStream(".", "UnityToPython", PipeDirection.Out);
            if (debugMode) Debug.Log("Conectando a Python...");
            
            pipeClient.Connect(connectionTimeout);
            
            if (debugMode) Debug.Log("Conectado al pipe.");

            writer = new StreamWriter(pipeClient, Encoding.UTF8) { AutoFlush = true };
            isConnected = true;
        }
        catch (Exception e)
        {
            Debug.LogError("No se pudo conectar al pipe: " + e.Message);
            isConnected = false;
        }
    }

    public bool SendJointAngles(List<float> jointAngles)
    {
        if (!isConnected || jointAngles == null) 
            return false;

        try
        {
            var data = new JointAnglesWrapper { angles = jointAngles.ToArray() };
            string json = JsonUtility.ToJson(data);
            writer.WriteLine(json);

            if (debugMode)
                Debug.Log($"Enviado: {jointAngles.Count} ángulos");

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("Error enviando datos: " + e.Message);
            isConnected = false;
            return false;
        }
    }

    public bool IsConnected => isConnected && pipeClient != null && pipeClient.IsConnected;

    void OnApplicationQuit()
    {
        if (isConnected && writer != null)
        {
            try
            {
                writer.WriteLine("exit");
                writer.Close();
                pipeClient.Close();
            }
            catch (Exception e)
            {
                Debug.LogError("Error cerrando pipe: " + e.Message);
            }
        }
    }
}

[Serializable]
public class JointAnglesWrapper
{
    public float[] angles;
}