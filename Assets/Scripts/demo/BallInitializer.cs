using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallInitializer : MonoBehaviour
{
    public List<GameObject> balls = new List<GameObject>();
    public GameObject ballPrefab;

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
                Random.Range(1f, 5f),
                Random.Range(-5f, 5f)
            );
            GameObject ball = Instantiate(ballPrefab, newPos, Quaternion.identity, transform);  // transform is the parent container
            balls.Add(ball);
        }
    }

    // Update is called once per frame
    void Update()
    {
        Physics.gravity = new Vector3(0, -9.81f, 0);
        // Physics.gravity = new Vector3(0, 0, 0);
    }
}
