using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrontFloatyUI : MonoBehaviour
{
    [Header("Camera Following")]
    public Transform vrCamera;
    public float distance = 2.0f;
    public float smoothSpeed = 2.0f;
    public float updateInterval = 10f;
    Vector3 targetPosition;
    Quaternion targetRotation;

    // Start is called before the first frame update
    void Start()
    {
        if (vrCamera == null)
            vrCamera = Camera.main.transform;

        UpdateTargetTransform();
        transform.position = targetPosition;
        transform.rotation = targetRotation;
        StartCoroutine(UpdatePositionTimer());
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
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
        targetRotation = Quaternion.LookRotation(transform.position - vrCamera.position);
    }
}
