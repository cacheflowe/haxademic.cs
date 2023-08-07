using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabObjectOldOsc : MonoBehaviour
{
    public Vector3 basePosition;
    float distanceFromCenter;

    // Start is called before the first frame update
    void Start()
    {
        basePosition = transform.position;
        distanceFromCenter = Vector3.Distance(basePosition, Vector3.zero);
    }

    // Update is called once per frame
    void Update()
    {
        // augment original position and set on GameObject
        Vector3 curPos = basePosition;
        float freq = distanceFromCenter * 1.2f;
        float yAmp = 1;
        curPos.y += Mathf.Sin(Time.time + freq) * yAmp;
        transform.position = curPos;
    }
}
