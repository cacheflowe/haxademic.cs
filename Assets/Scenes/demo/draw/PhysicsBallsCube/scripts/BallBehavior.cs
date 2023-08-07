using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallBehavior : MonoBehaviour
{
    public float upForce = 1000f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // if(Application.isEditor) {
        if (Input.GetKeyDown(KeyCode.Space)) {
            float randX = Random.Range(-1f, 1f) * upForce * 0.1f;
            float randY = Random.Range(0.1f, 1f) * upForce;
            float randZ = Random.Range(-1f, 1f) * upForce * 0.1f;
            GetComponent<Rigidbody>().AddForce(new Vector3(randX, randY, randZ));
        }
        // }
    }

    void OnCollisionEnter(Collision other) {
        // if (other.gameObject.tag == "Ball") {
        //     Destroy(other.gameObject);
        // }
        if(other.relativeVelocity.magnitude > 2f) {
            ParticleSystem ps = GetComponentInChildren<ParticleSystem>();
            ps.Play();
        }
    }


}
