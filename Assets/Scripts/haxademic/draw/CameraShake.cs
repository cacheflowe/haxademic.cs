using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShake : Singleton<CameraShake>
{
    // Transform of the camera to shake. Grabs the gameObject's transform
    // if null.
    public Transform camTransform;
    public Vector3 originalPos;

    // Shake params
    public float shakeDuration = 0f;
    public float shakeAmount = 0.7f;
    public float decreaseFactor = 1.0f;

    void Start()
    {
        camTransform = Camera.main.transform;
        originalPos = camTransform.localPosition;
    }

    void Update()
    {
        if (Camera.main != null && Camera.main.isActiveAndEnabled)
        {
            if (shakeDuration > 0 && shakeAmount > 0)
            {
                camTransform.localPosition = originalPos + Random.insideUnitSphere * shakeAmount;
                shakeDuration -= Time.deltaTime * decreaseFactor;
                shakeAmount -= Time.deltaTime * decreaseFactor;

            }
            else
            {
                shakeDuration = 0f;
                camTransform.localPosition = originalPos;
            }
        }
    }

    public void Shake(float duration, float amount, float decreaseFactor = 1.0f)
    {
        this.shakeDuration = duration;
        this.shakeAmount = amount;
        this.decreaseFactor = decreaseFactor;
    }
}
