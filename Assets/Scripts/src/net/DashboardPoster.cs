using System.Collections;
using UnityEngine.Networking;
using UnityEngine;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System;
using System.IO;

public class DashboardPoster : MonoBehaviour
{
    public string dashboardURL = "http://localhost/haxademic/www/dashboard-new/";
    public bool showDebugLogs = false;
    public float interval = 600; // every 10 minutes (60s * 10)
    public float intervalScreenshot = 1800; // every 30 minutes (60s * 30)
    private string screenshotProperty = "";

    void Start()
    {
        InvokeRepeating("PostHeartbeat", interval, interval);
        StartScrenshotThread();
    }

    private void OnDestroy()
    {
        CancelInvoke("PostHeartbeat");
    }

    void PostHeartbeat()
    {
        // check internet connection before trying to post
        if(Application.internetReachability == NetworkReachability.NotReachable) {
            Log("No internet connection");
            return;
        }

        // build dashboard json data
        string jsonData = $@"
        {{
            ""appId"": ""TEST_PROJECT"",
            ""appTitle"": ""Unity App"",
            ""ipAddress"": ""{GetLocalIPAddress()}"",
            ""uptime"": {Mathf.Round(Time.realtimeSinceStartup)},
            ""frameCount"": {Time.frameCount},
            ""frameRate"": {(int)(1.0f / Time.smoothDeltaTime)},
            ""resolution"": ""{Screen.width}x{Screen.height}""
            {screenshotProperty}
        }}
        ";

        // clear out screenshot after sending
        screenshotProperty = "";

        // Start the web request
        StartCoroutine(PostRequest(dashboardURL, jsonData));
    }

    private IEnumerator PostRequest(string url, string json)
    {
        // Create a UnityWebRequest with the URL and set it to use POST method
        var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        request.SetRequestHeader("Accept", "application/json");
        request.SetRequestHeader("Content-Type", "application/json");
        request.method = UnityWebRequest.kHttpVerbPOST;

        // Attach the JSON data to the request
        byte[] jsonBytes = new System.Text.UTF8Encoding().GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(jsonBytes);
        request.downloadHandler = new DownloadHandlerBuffer();

        // Send the request and wait for the response
        yield return request.SendWebRequest();


        // Check for errors
        if (request.result == UnityWebRequest.Result.ConnectionError)
        {
            Log("ConnectionError sending JSON data: " + request.error);
        }
        else if (request.result != UnityWebRequest.Result.Success)
        {
            Log("Error sending JSON data: " + request.error);
        }
        else
        {
            // Request was successful, handle the response
            Log("JSON data sent successfully! " + request.responseCode);
            Log("Response: " + request.result);
            Log("request.downloadHandler.data: " + Encoding.UTF8.GetString(request.downloadHandler.data));
        }
    }

    void Log(string message)
    {
        if(showDebugLogs) Log("[DashboardPoster] " + message);
    }

    public static string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        throw new Exception("No network adapters with an IPv4 address in the system!");
    }

    void StartScrenshotThread() 
    {
        InvokeRepeating("CollectScreenshot", interval, interval);
    }

    void CollectScreenshot()
    {
        // Capture the screenshot
        string filePath = Path.Combine(Application.persistentDataPath, "screenshot.jpg");
        ScreenCapture.CaptureScreenshot(filePath);
        Log("Dashboard screenshot file saved!");

        // Wait a short moment to ensure the screenshot is saved
        StartCoroutine(SaveScreenshotAsBase64(filePath));
    }

    private System.Collections.IEnumerator SaveScreenshotAsBase64(string filePath)
    {
        // Wait for the screenshot to be saved
        yield return new WaitForSeconds(1f);

        // Read the screenshot image from the file
        byte[] imageBytes = File.ReadAllBytes(filePath);

        // Convert the image bytes to JPG format
        Texture2D texture = new Texture2D(2, 2); // Create a temporary Texture2D to load the image
        texture.LoadImage(imageBytes); // Load the image from the byte array
        byte[] jpgBytes = texture.EncodeToJPG(); // Encode the texture to JPG format

        // Convert the JPG bytes to a base64 string
        string base64String = Convert.ToBase64String(jpgBytes);

        // Destroy the temporary Texture2D
        Destroy(texture);
        // Output the base64 string
        screenshotProperty = $@"
            , ""imageScreenshot"": ""{base64String}""
        ";
        // data:image/jpeg;base64,
        Log("Dashboard screenshot base64 encoded!");
        // Log("Screenshot saved as base64: " + base64String);
    }

    void Update()
    {
        
    }

}
