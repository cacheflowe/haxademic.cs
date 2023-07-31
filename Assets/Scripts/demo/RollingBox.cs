using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class BoxRoller : MonoBehaviour
{
    private Vector3 startPos;

    [Range(0.001f, 0.4f)]
    public float speedAmp = 0.025f;
    [Range(0.001f, 0.4f)]
    public float xAddAmp = 0.025f;
    [Range(0, 0.5f)]
    public float xSinRemap = 0.3f;
    public float xLeft = -2.35f;
    public float xRight = 7f;
    // public float speed = 2f;
    public float boxCircumference;
    public float yTravel;
    public float rotationProgress = 0;
    public float rotationProgressLast = 0;

    void Start()
    {
        // boxSize = meshRenderer.bounds.size.x;
        float boxSize = transform.localScale.x * 2f;

        // calculations for box roll
        startPos = transform.position;
        boxCircumference = boxSize * 4f;
        float boxDiag = Mathf.Sqrt(2f) * (boxSize);
        yTravel = boxDiag - boxSize;
    }

    void Update()
    {
        // move virtual x forward
        float x = Time.frameCount * speedAmp;
        // transform.position += new Vector3(Time.deltaTime * speed, 0, 0);
        // transform.position += new Vector3(Mathf.Sin((Time.fixedTime / 40f) / 40f % Mathf.PI), 0, 0);

        // box roll calculation
        float rotations = (x + 100) / boxCircumference;  // make sure we're always positive (+100)
        rotationProgress = (rotations % 0.25f) / 0.25f; // percent of progress through quarter rotation
        if(rotationProgress < rotationProgressLast) {
            AudioManager.instance.Play("Thump");
            CameraShake.Instance.Shake(0.3f, 0.05f);
        }
        rotationProgressLast = rotationProgress;
        float yAdd = Mathf.Sin(rotationProgress * Mathf.PI) * yTravel / 4f;
        float xAdd = math.remap(0, 1, xSinRemap, 1f - xSinRemap, Mathf.Sin(rotationProgress * Mathf.PI)) * xAddAmp;        

        // update mesh
        transform.position = new Vector3(transform.position.x + xAdd, startPos.y + yAdd, startPos.z);
        transform.rotation = Quaternion.Euler(0, 0, -rotationProgress * Mathf.PI/2f * Mathf.Rad2Deg);

        // recycle to left side of screen
        if(transform.position.x > xRight) {
            transform.position = new Vector3(xLeft, transform.position.y, transform.position.z);
        }


        // reset
        if (Input.GetKeyDown(KeyCode.Space))
        {
            transform.position = startPos;
        }

    }

	float easeInOutSine(float normalizedProgress) {
		return easeInOutSine(normalizedProgress, 0, 1, 1);
	}
	
	float easeInOutSine(float t, float b, float c, float d) {
		return -c/2 * ((float)Mathf.Cos(Mathf.PI*t/d) - 1) + b;
	}

}
