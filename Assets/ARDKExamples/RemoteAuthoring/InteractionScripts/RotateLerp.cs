using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateLerp: MonoBehaviour
{
    private Quaternion startRotation;
    private Quaternion targetRotation;
    private float rotationTime = 1.0f;
    private bool isRotating = false;

    // Start is called before the first frame update
    void Start()
    {
        startRotation = transform.rotation;
        targetRotation = transform.rotation * Quaternion.Euler(0, 180, 0);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ChangeBool()
    {
        if (!isRotating)
        {
            StartCoroutine(Rotate());
        }
    }

    IEnumerator Rotate()
    {
        isRotating = true;
        float elapsedTime = 0;
        while (elapsedTime < rotationTime)
        {
            transform.rotation = Quaternion.Lerp(startRotation, targetRotation, elapsedTime / rotationTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        // transform.rotation = targetRotation;
        isRotating = false;
        startRotation = targetRotation;
        targetRotation = targetRotation * Quaternion.Euler(0, 180, 0);
    }
    
}