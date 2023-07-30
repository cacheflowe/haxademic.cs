using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using NativeWebSocket; // https://github.com/endel/NativeWebSocket

//////////////////////////////////////
// Incoming Socket Message deserialization
//////////////////////////////////////

[System.Serializable]
public class SocketCmd
{
    public string cmd;
    public float valueX;
    public float valueY;
    public float valueZ;
    public bool active;
    public int count;

    public static SocketCmd CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<SocketCmd>(jsonString);
    }
}

//////////////////////////////////////
// WebSocket connection manager
//////////////////////////////////////

public class WebSocketManager : MonoBehaviour
{
    [Header("Socket Server config")]
    public string wsURL = "ws://localhost:3001";
    protected WebSocket websocket;

    [Space]
    [Header("Socket Reconnection status")]
    public bool connectionActive = false;
    public Image connectionStatusImage;
    public float secondsSinceReconnect = 0.0f;

    // incoming ws:// values
    [Space]
    [Header("Incoming socket vals")]
    public Vector3 position;
    public bool playerActive;


    void Start()
    {
        StartWebSocket();
        InvokeRepeating("PollConnection", 0.0f, 5f);
    }

    // Update is called once per frame
    void Update()
    {
        #if !UNITY_WEBGL || UNITY_EDITOR
            if(websocket != null) websocket.DispatchMessageQueue();
        #endif
        CheckReconnectTimer();
    }

    //////////////////////////////////////
    // WebSocket object & callbacks
    //////////////////////////////////////

    async void StartWebSocket() 
    {
        websocket = new WebSocket(wsURL);
        websocket.OnOpen += OnOpen;
        websocket.OnError += OnError;
        websocket.OnClose += OnClose;
        websocket.OnMessage += OnMessage;
        await websocket.Connect();
    }

    void OnOpen() 
    {
        Debug.Log("WebSocketManager: Connected!");
        connectionActive = true;
        connectionStatusImage.color = Color.green;
    }

    void OnError(string e)
    {
        Debug.LogError("WebSocketManager: Error! " + e);
        connectionActive = false;
        connectionStatusImage.color = Color.red;
    }

    void OnClose(WebSocketCloseCode e)
    {
        Debug.LogError("WebSocketManager: Connection closed!");
        connectionActive = false;
        connectionStatusImage.color = Color.red;
    }

    void OnMessage(byte[] bytes)
    {
        var jsonStr = System.Text.Encoding.UTF8.GetString(bytes);
        HandleMessage(jsonStr);
    }

    void HandleMessage(string jsonStr)
    {
        if (jsonStr.Contains("cmd")) 
        {
            SocketCmd jsonObj = JsonUtility.FromJson<SocketCmd>(jsonStr);
            string cmd = jsonObj.cmd;
            switch (cmd)
            {
                case "position":
                    position.Set(jsonObj.valueX, jsonObj.valueY, jsonObj.valueZ);
                    break;
                case "active":
                    playerActive = jsonObj.active;
                    break;
                case "heartbeat":
                    // Debug.Log("heartbeat ( " + jsonObj.count + " )");
                    break;
                default:
                    break;
            }
        }
    }

    //////////////////////////////////////
    // Reconnect timer
    //////////////////////////////////////

    void CheckReconnectTimer()
    {
        secondsSinceReconnect += Time.deltaTime;
        // float minutes = Mathf.FloorToInt(secondsSinceReconnect / 60);
        // float seconds = Mathf.FloorToInt(secondsSinceReconnect % 60);
        // Debug.Log(string.Format("WebSocketManager - Reconnect timer: {0:00}:{1:00}", minutes, seconds));
    }

    async void PollConnection()
    {
        if (websocket == null) return;
        if (websocket.State == WebSocketState.Open)
        {
            await websocket.SendText("{\"cmd\": \"reverseHeartbeat\"}"); // send heartbeat back to server
        } 
        else if (websocket.State == WebSocketState.Closed)
        {
            if (secondsSinceReconnect > 30)
            {
                Debug.Log("WebSocket is not open! Attempting to reconnect..."); 
                secondsSinceReconnect = 0.0f;
                await websocket.Connect();
            }
        }
    }

    //////////////////////////////////////
    // Cleanup
    //////////////////////////////////////

    private async void OnApplicationQuit()
    {
        if (websocket == null) return;
        await websocket.Close();
    }

}
