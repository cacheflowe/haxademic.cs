using UnityEngine;
using UnityEngine.UI;
using Unity.Mathematics;
using Intel.RealSense;
using System.Threading.Tasks;
using TMPro;

// Realsense dev docs for vanilla C# wrapper
// https://dev.intelrealsense.com/docs/csharp-wrapper
// https://github.com/IntelRealSense/librealsense/tree/master/wrappers/csharp
// https://github.com/IntelRealSense/librealsense/blob/master/wrappers/csharp/Documentation/cookbook.md
// https://github.com/IntelRealSense/librealsense/blob/master/wrappers/csharp/Documentation/pinvoke.md

// Nice-to-haves:
// - Multiple depthregions  (for the future) - extract camera init and depth region into different objects/prefabs
// - Example scene w/prefabs

[System.Serializable]
public class DepthRegionConfig
{
    public int left = 0;
    public int right = 1280;
    public int top = 0;
    public int bottom = 720;
    public float near = 0.25f;
    public float far = 1.25f;
    public int pixelSkip = 8;
    public int minPixels = 50;
    public float debugScale = 1f;
}

public class RealsenseDepthRegion : Singleton<RealsenseDepthRegion>
{
    [Header("UI")]
    private Canvas canvas;
    public TextMeshProUGUI textMeshUiTitle;
    public TextMeshProUGUI textMeshCameraName;
    public TextMeshProUGUI textMeshDebugLog;
    public bool hideOnInit = true;

    [Header("Camera Config")]
    private Pipeline pipe;
    private DepthFrame depthData; // unreliable to use outside of thread
    private byte[] dataArray;

    [Space]
    [Header("DepthRegion Settings & UI")]
    public Slider sliderLeft;
    public Slider sliderRight;
    public Slider sliderTop;
    public Slider sliderBottom;
    public Slider sliderNear;
    public Slider sliderFar;
    public Slider sliderPixelSkip;
    public Slider sliderMinPixels;
    public Slider sliderDebugScale;
    public DepthRegionConfig config;
    private const string configKey = "DepthRegionConfig";

    // // temporary depth region settings - move this to a UI

    [Header("Depth Region Calculated State")]
    [SerializeField]
    private int depthW = 0;

    [SerializeField]
    private int depthH = 0;

    [SerializeField]
    private int debugTextureW = 0;

    [SerializeField]
    private int debugTextureH = 0;

    [SerializeField]
    private int numDepthPixels = 0;

    [SerializeField]
    private int dataArraySize = 0;

    [SerializeField]
    private float executionTime = 0;
    private System.Diagnostics.Stopwatch stopwatch;
    private Texture2D debugTexture;
    private Task realsenseThread; // keep for error checking

    [Space]
    [Header("Active User Calculated Position")]
    [Range(-1f, 1f)]
    public float userX = 0;

    [Range(-1f, 1f)]
    public float userY = 0;

    [Range(-1f, 1f)]
    public float userZ = 0;
    public int pixelCount = 0;
    public float userActiveEased = 0;
    public bool userActiveFrame = false;
    public bool userActive = false;

    void Start()
    {
        canvas = GetComponent<Canvas>();
        if(hideOnInit) canvas.enabled = false;
        #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            Init();
        #endif
    }

    void Init()
    {
        AddSliderListeners();
        LoadConfig();
        InitCamera();
        Application.quitting += StopCameraOnExit;
    }

    void AddSliderListeners()
    {
        sliderLeft.onValueChanged.AddListener(delegate { config.left = (int)sliderLeft.value; DepthRegionUpdated(true); });
        sliderRight.onValueChanged.AddListener(delegate { config.right = (int)sliderRight.value; DepthRegionUpdated(true); });
        sliderTop.onValueChanged.AddListener(delegate { config.top = (int)sliderTop.value; DepthRegionUpdated(true); });
        sliderBottom.onValueChanged.AddListener(delegate { config.bottom = (int)sliderBottom.value; DepthRegionUpdated(true); });
        sliderNear.onValueChanged.AddListener(delegate { config.near = sliderNear.value; DepthRegionUpdated(false); });
        sliderFar.onValueChanged.AddListener(delegate { config.far = sliderFar.value; DepthRegionUpdated(false); });
        sliderPixelSkip.onValueChanged.AddListener(delegate { config.pixelSkip = (int)sliderPixelSkip.value; DepthRegionUpdated(true); });
        sliderMinPixels.onValueChanged.AddListener(delegate { config.minPixels = (int)sliderMinPixels.value; DepthRegionUpdated(false); });
        sliderDebugScale.onValueChanged.AddListener(delegate { config.debugScale = sliderDebugScale.value; DepthRegionUpdated(true); });
    }

    void LoadConfig()
    {
        if (PlayerPrefs.HasKey(configKey))
        {
            string json = PlayerPrefs.GetString(configKey);
            config = JsonUtility.FromJson<DepthRegionConfig>(json);
        }
        else
        {
            config = new DepthRegionConfig();
        }
        UpdateSlidersFromConfig();
    }

    void SaveConfig()
    {
        string json = JsonUtility.ToJson(config);
        PlayerPrefs.SetString(configKey, json);
        PlayerPrefs.Save();
    }

    void UpdateSlidersFromConfig()
    {
        sliderLeft.value = config.left;
        sliderRight.value = config.right;
        sliderTop.value = config.top;
        sliderBottom.value = config.bottom;
        sliderNear.value = config.near;
        sliderFar.value = config.far;
        sliderPixelSkip.value = config.pixelSkip;
        sliderMinPixels.value = config.minPixels;
        sliderDebugScale.value = config.debugScale;
    }

    void DepthRegionUpdated(bool rebuildTexture)
    {
        if (rebuildTexture) debugTexture = null;
        SaveConfig();
    }

    void InitCamera()
    {
        CheckCameraConnected();
        StartRealsenseThread();
    }

    void CheckCameraConnected()
    {
        Context ctx = new Context();
        var list = ctx.QueryDevices(); // Get a snapshot of currently connected devices
        if (list.Count == 0)
        {
            textMeshCameraName.text = "No device detected. Is it plugged in?";
            textMeshCameraName.color = Color.red;
        }
        else
        {
            Device device = list[0];
            textMeshCameraName.text = "Camera initialized: <b>" + device.Info[CameraInfo.Name] + "</b>";
        }
    }

    void InitRealsenseRawDepth()
    {
        // build config - can skip this if we're not changing the config, and call Start()
        // var cfg = new Config();
        // cfg.EnableStream(Stream.Infrared, 1);
        // cfg.EnableStream(Stream.Infrared, 2);
        // cfg.EnableStream(Stream.Color);
        // cfg.EnableStream(Stream.Pose);

        // start camera on a thread
        pipe = new Pipeline();
        pipe.Start();

        while (true)
        {
            using (var frames = pipe.WaitForFrames())
            using (var depthData = frames.DepthFrame)
            {
                UpdateDepthDataThreaded(depthData);
            }
        }
    }

    void StartRealsenseThread()
    {
        realsenseThread = Task.Factory.StartNew(InitRealsenseRawDepth, TaskCreationOptions.LongRunning);
    }

    void CheckRealsenseThreadErrors()
    {
        if (realsenseThread != null)
        {
            if (realsenseThread.IsFaulted)
            {
                Debug.LogError("Background thread error: " + realsenseThread.Exception);
            }
        }
    }

    void StopCameraOnExit()
    {
        if (pipe != null && depthData != null)
            pipe.Stop();
    }

    void BuildDebugTexture()
    {
        // if we don't have depth data, bail
        if (depthData == null) return;

        // calculate texture size with pixel skip
        // we might not be ready, so bail if we need to try again
        debugTextureW = (config.right - config.left) / config.pixelSkip;
        debugTextureH = (config.bottom - config.top) / config.pixelSkip;
        // if(debugTextureW == 0 || debugTextureH == 0) return;

        // build texture
        debugTexture = new Texture2D(debugTextureW, debugTextureH, TextureFormat.RGB24, true);
        debugTexture.filterMode = FilterMode.Point;
        debugTexture.wrapMode = TextureWrapMode.Clamp;
        debugTexture.Apply(false);

        // create byte data color array & fill with default green color for visibility
        numDepthPixels = debugTextureW * debugTextureH;
        dataArraySize = numDepthPixels * 3;
        dataArray = new byte[dataArraySize];
        for (int i = 0; i < dataArray.Length; i += 3)
        {
            dataArray[i] = 255;
            dataArray[i + 1] = 0;
            dataArray[i + 2] = 0;
        }
        debugTexture.SetPixelData(dataArray, 0, 0);
        debugTexture.Apply(false);

        // Update the UI RawImage, and resize to match the texture size
        RawImage rawImage = GetComponentInChildren<RawImage>();
        RectTransform rectTransform = rawImage.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(
            debugTextureW * 2 * config.debugScale,
            debugTextureH * 2 * config.debugScale
        );
        rawImage.texture = debugTexture;
    }

    void Update()
    {
        CheckRealsenseThreadErrors();
        UpdateSilhouetteTexture();
        CheckUserActive();
        CheckKeyboardToggle();
        UpdateDebugView();
    }

    void CheckUserActive()
    {
        float easeStep = 2f * Time.deltaTime; // should take a half second to switch from 0 to 1
        userActiveEased += (userActiveFrame) ? easeStep : -easeStep;
        userActiveEased = Mathf.Clamp(userActiveEased, 0f, 1f);
        if (userActiveEased == 0) userActive = false;
        if (userActiveEased == 1) userActive = true;
    }

    void CheckKeyboardToggle()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Canvas canvas = GetComponent<Canvas>();
            canvas.enabled = !canvas.enabled;
        }
    }

    void UpdateSilhouetteTexture()
    {
        // if (!canvas.enabled) return; // we could stop updating the debug texture if the canvas is disabled, but doesn't seem to impact performance
        if (debugTexture == null)
        {
            BuildDebugTexture();
        }
        else
        {
            // update with threaded data
            debugTexture.SetPixelData(dataArray, 0, 0);

            // draw debug lines into texture data
            DrawLine(
                debugTexture,
                new Vector2(0, debugTextureH / 2),
                new Vector2(debugTextureW, debugTextureH / 2),
                Color.white
            );
            DrawLine(
                debugTexture,
                new Vector2(debugTextureW / 2, 0),
                new Vector2(debugTextureW / 2, debugTextureH),
                Color.white
            );

            // draw input position crosshair
            Color posColor = (userActive) ? Color.white : Color.red;
            int xSize = 3;
            int userXPos = (int)math.remap(-1f, 1f, 0f, debugTextureW, userX);
            int userYPos = (int)math.remap(1f, -1f, 0f, debugTextureH, userY);
            DrawLine(
                debugTexture,
                new Vector2(userXPos - xSize, userYPos - xSize),
                new Vector2(userXPos + xSize, userYPos + xSize),
                posColor
            );
            DrawLine(
                debugTexture,
                new Vector2(userXPos - xSize, userYPos + xSize),
                new Vector2(userXPos + xSize, userYPos - xSize),
                posColor
            );

            // commit texture to GPU
            debugTexture.Apply(false);
        }
    }

    void UpdateDepthDataThreaded(DepthFrame depthData)
    {
        // store depth stream attributes
        this.depthData = depthData;
        depthW = depthData.Width;
        depthH = depthData.Height;

        // we rely on the debug texture, which is lazy-initialized
        if (debugTexture == null) return;

        // tally depth data
        userActiveFrame = false;
        pixelCount = 0;
        float controlXTotal = 0;
        float controlYTotal = 0;
        float controlZTotal = 0;

        // check performance
        if (stopwatch == null) stopwatch = System.Diagnostics.Stopwatch.StartNew();
        stopwatch.Restart();

        // iterate over debug texture size, multiplying by pixel skip to access raw depth data grid
        // also takes into account the region specified by left/right/top/bottom
        bool safeToRead = dataArray != null && dataArray.Length > 0; // && textureSizeDirty == false;

        if (safeToRead)
        {
            int pixelIndex = dataArray.Length - 3;
            for (int y = 0; y < debugTextureH; y++)
            {
                for (int x = 0; x < debugTextureW; x++)
                {
                    int sampleY = config.top + y * config.pixelSkip;
                    int sampleX = config.left + x * config.pixelSkip;
                    bool sampleInBounds = sampleX < depthData.Width && sampleY < depthData.Height;  // protect against resized texture/data in thread
                    float pixelDepth = (sampleInBounds) ? depthData.GetDistance(sampleX, sampleY) : 15f;
                    if (pixelDepth > config.near && pixelDepth < config.far)
                    {
                        // set debug pixel with depth awareness
                        byte col = (byte)math.floor(math.remap(config.near, config.far, 255f, 100f, pixelDepth));
                        dataArray[pixelIndex] = 0;
                        dataArray[pixelIndex + 1] = col;
                        dataArray[pixelIndex + 2] = 0;
                        // add up for calculations
                        pixelCount++;
                        controlXTotal += x;
                        controlYTotal += y;
                        controlZTotal += pixelDepth;
                    }
                    else
                    {
                        // set debug pixel
                        byte col = (byte)math.floor(math.remap(config.far, 5, 50, 0f, pixelDepth)); // 5 is max distance for practical use
                        dataArray[pixelIndex] = col;
                        dataArray[pixelIndex + 1] = col;
                        dataArray[pixelIndex + 2] = col;
                    }

                    // advance through depth array
                    // b/c of threading, the texture size and dataArray can be different sizes.
                    // make sure we stay in bounds of the array, or we get silent errors that disable the camera thread
                    if (pixelIndex > 0) pixelIndex -= 3;
                }
            }
        }

        // check if enough of a user is present
        if (pixelCount > config.minPixels)
        {
            userActiveFrame = true;
            if (controlXTotal > 0 && controlZTotal > 0)
            {
                userX = controlXTotal / pixelCount;
                userY = controlYTotal / pixelCount;
                userZ = controlZTotal / pixelCount;
                userX = math.remap(0, debugTextureW, -1f, 1f, userX);
                userY = math.remap(0, debugTextureH, -1f, 1f, userY);
                userZ = math.remap(config.near, config.far, -1f, 1f, userZ);
                userX *= -1f;
                userY *= 1f;
                userZ *= -1f;
            }
        }
        else
        {
            userActiveFrame = false;
        }

        // track performance measurement
        executionTime = stopwatch.ElapsedMilliseconds;
        stopwatch.Stop();
    }

    void DrawLine(Texture2D tex, Vector2 p1, Vector2 p2, Color col)
    {
        // from: https://discussions.unity.com/t/create-line-on-a-texture/41000/3
        Vector2 t = p1;
        float frac = 1 / Mathf.Sqrt(Mathf.Pow(p2.x - p1.x, 2) + Mathf.Pow(p2.y - p1.y, 2));
        float ctr = 0;

        while ((int)t.x != (int)p2.x || (int)t.y != (int)p2.y)
        {
            t = Vector2.Lerp(p1, p2, ctr);
            ctr += frac;
            tex.SetPixel((int)t.x, (int)t.y, col);
        }
    }

    void UpdateDebugView()
    {
        textMeshDebugLog.text =
$@"<b>Depth Region Config:</b>
left: {config.left}
right: {config.right}
top: {config.top}
bottom: {config.bottom}
near: {config.near}
far: {config.far}
pixelSkip: {config.pixelSkip}
minPixels: {config.minPixels}
debugScale: {config.debugScale}

<b>Depth Region Calculated:</b>
depthW: {depthW}
depthH: {depthH}
debugTextureW: {debugTextureW}
debugTextureH: {debugTextureH}
numDepthPixels: {numDepthPixels}
executionTime: {executionTime}ms

<b>User Position:</b>
userActive: {userActive}
pixelCount: {pixelCount}
userX: {userX}
userY: {userY}
userZ: {userZ}
";
    }

    public void OnDestroy()
    {
        // if(pipe != null) pipe.Stop();
    }
}
