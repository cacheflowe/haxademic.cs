using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleBurst : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        ParticleSystem ps = GetComponent<ParticleSystem>();
        if (Input.GetKeyDown(KeyCode.Space)) {
            ps.Play();
        }
    }
}
