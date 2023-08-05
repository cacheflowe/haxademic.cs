using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabInitializer : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject prefab;
    [Space]
    [Header("Grid")]
    public int cols = 4;
    public int rows = 4;
    public float spacing = 1f;
    
    private List<GameObject> balls = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        Build();
    }

    void Build() 
    {
        // build spheres
        for (int z = 0; z < 100; z++) {
            Vector3 newPos = new Vector3(
                Random.Range(-5f, 5f),
                Random.Range(-1f, 5f),
                Random.Range(-5f, 5f)
            );
            GameObject ball = Instantiate(prefab, newPos, Quaternion.identity, transform);  // transform is the parent container
            balls.Add(ball);
        }
    }

    void Update()
    {
        
    }
}
