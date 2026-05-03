using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrontFloatyUI : MonoBehaviour
{
    [Header("Camera Following")]
    public Transform vrCamera;
    public Transform followObject;
    public float distance = 1.5f;
    public float smoothSpeed = 3.0f;
    public float updateInterval = 1.5f;
    public Vector3 offset;
    public bool isFollow; //should this follow player or not
    Vector3 targetPosition;

    Coroutine updateCoroutine;

    void Awake()
    {
        if (vrCamera == null)
            vrCamera = Camera.main.transform;
    }
    void OnEnable()
    {
        if (!isFollow)
            return;

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
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(transform.position - vrCamera.position), Time.deltaTime * smoothSpeed);

        if (!isFollow)
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
        else if(followObject != null)
            transform.position = Vector3.Lerp(transform.position, followObject.position, Time.deltaTime * smoothSpeed);

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
        Vector3 basePt = vrCamera.position + (vrCamera.forward * distance);
        Vector3 relativeOffset = vrCamera.TransformDirection(offset);

        targetPosition = basePt + relativeOffset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);

    }
}
