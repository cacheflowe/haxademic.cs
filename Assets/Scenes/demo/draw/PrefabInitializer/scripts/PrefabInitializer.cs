using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabInitializer : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject prefab;

    [Space]
    [Header("Grid")]
    [Range(1, 200)]
    public int cols = 4;

    [Range(1, 200)]
    public int rows = 4;
    private int numInstances;

    [Range(0, 20)]
    public float spacingX = 1;
    private float oldSpacingX = 1;

    [Range(0, 20)]
    public float spacingY = 1;
    private float oldSpacingY = 1;

    [Range(0.01f, 3f)]
    public float scale = 1;
    private float oldScale = 1;

    [Range(-5, 5)]
    public float zAmp = 0;
    private float oldZAmp = 0;

    private readonly List<GameObject> instances = new();

    void Start()
    {
        // OnValidate() will call Build() on start when editing, but we need to explicitly call Build() in game mode
        if (!Application.isEditor)
            Build();
    }

    ///////////////////////////////////////////
    // Check for UI changes
    ///////////////////////////////////////////

    void OnValidate()
    {
        // check for changed values
        bool scaleChanged = scale != oldScale;
        bool spacingXChanged = spacingX != oldSpacingX;
        bool spacingYChanged = spacingY != oldSpacingY;
        bool zAngleChanged = zAmp != oldZAmp;
        bool configChanged =
            NumInstancesChanged()
            || scaleChanged
            || spacingXChanged
            || spacingYChanged
            || zAngleChanged;
        // if changed, rebuild!
        if (configChanged)
            Build();
        // store old values
        oldScale = scale;
        oldSpacingX = spacingX;
        oldSpacingY = spacingY;
        oldZAmp = zAmp;
    }

    bool NumInstancesChanged()
    {
        int oldVal = numInstances;
        numInstances = cols * rows;
        return oldVal != numInstances;
    }

    ///////////////////////////////////////////
    // Rebuild prefab instances
    ///////////////////////////////////////////

    void Build()
    {
        // Ignore in editor mode
        if (!Application.isPlaying)
            return;
        Debug.Log("Build");

        // first, clean any old game objects
        CleanAllInstances();

        // build spheres
        float totalW = (cols - 1) * spacingX;
        float startX = -totalW / 2f;
        float totalH = (rows - 1) * spacingY;
        float startY = -totalH / 2f;
        for (int i = 0; i < numInstances; i++)
        {
            int grixXindex = i % cols;
            int gridYindex = (int)Mathf.Floor(i / cols);
            float x = startX + grixXindex * spacingX;
            float y = startY + gridYindex * spacingY;
            float z = zAmp * y;
            Vector3 newPos = new(x, y, z);
            GameObject ball = Instantiate(prefab, newPos, Quaternion.identity, transform); // transform is the parent container
            ball.transform.localScale = Vector3.one * scale;
            instances.Add(ball);
        }
    }

    ///////////////////////////////////////////
    // Clean up prefab instances
    ///////////////////////////////////////////

    void CleanAllInstances()
    {
        instances.Clear();
        RemoveAllChildren();
    }

    void RemoveAllChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            Destroy(child);
        }
    }

    ///////////////////////////////////////////
    // Update prefab instances
    ///////////////////////////////////////////

    void Update()
    {
        // animate children, or do that in prefab
        // for larger crowd movements, we can update the collection here
    }
}
