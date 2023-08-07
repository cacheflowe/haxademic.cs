using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabObject : MonoBehaviour
{
    public Vector3 basePosition;
    public float offset;
    public float xWobbleSpeed;
    public float yAmp = 0.1f;
    public float jumpProgress = 0;
    public float jumpSpeed = 2.5f;
    public float nextJumpTime = -1;
    public Material[] materials;

    // TODO:
    // - Some baseline motion?
    // - Add model from Court

    void Start()
    {
        // store gameobject settings
        basePosition = transform.position;
        // distanceFromCenter = Vector3.Distance(basePosition, Vector3.zero);
        offset = Random.Range(0f, Mathf.PI * 2f);
        xWobbleSpeed = Random.Range(0.5f, 1.5f);

        // pick a random material and apply to MeshRenderer
        int randomIndex = Random.Range(0, materials.Length);
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.material = materials[randomIndex];
    }

    // Update is called once per frame
    void Update()
    {
        // augment original position and set on GameObject
        Vector3 curPos = basePosition;
        // move through jump cycle
        if (jumpProgress < 1) {
            jumpProgress += jumpSpeed * Time.deltaTime;
        } else if (jumpProgress > 1) {
            jumpProgress = 1;
        }
        // start next jump
        if (Time.time > nextJumpTime)
        {
            jumpProgress = 0;
            jumpSpeed = Random.Range(2f, 3.5f);
            nextJumpTime = Time.time + Random.Range(1.5f, 5f);
        }
        // set position
        curPos.x += Mathf.Cos(Time.time * xWobbleSpeed + offset) * 0.03f; // sideways wobble
        curPos.y += Mathf.Sin(jumpProgress * Mathf.PI) * yAmp; // jump
        transform.position = curPos;
    }
}
