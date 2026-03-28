using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrontFloatyUI : MonoBehaviour
{
    [Header("Camera Following")]
    public Transform vrCamera;
    public float distance = 1.5f;
    public float smoothSpeed = 3.0f;
    public float updateInterval = 1.5f;
    public Vector3 offset;
    Vector3 targetPosition;

    Coroutine updateCoroutine;

    void Awake()
    {
        if (vrCamera == null)
            vrCamera = Camera.main.transform;
    }
    void OnEnable()
    {
        UpdateTargetTransform();
        transform.position = targetPosition + offset;
        if (updateCoroutine != null) StopCoroutine(updateCoroutine);
        updateCoroutine = StartCoroutine(UpdatePositionTimer());
    }
    void OnDisable()
    {
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
            updateCoroutine = null;
        }
    }
    void Update()
    {
        transform.position = Vector3.Lerp(transform.position, targetPosition + offset, Time.deltaTime * smoothSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(transform.position - vrCamera.position), Time.deltaTime * smoothSpeed);
    }
    IEnumerator UpdatePositionTimer()
    {
        while (true)
        {
            yield return new WaitForSeconds(updateInterval);
            UpdateTargetTransform();
        }
    }
    void UpdateTargetTransform()
    {
        targetPosition = vrCamera.position + (vrCamera.forward * distance);
    }
}
