using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SensorInfo : MonoBehaviour
{
    public enum Sensor { RTouch, LTouch, RHand, LHand }
    public Sensor TargetSensor = Sensor.RTouch;
    // For Debug
    [Header("Debug")]
    public bool showDebug;
    public Material my_mat;
    public GameObject[] dirArrows;
    public GameObject[] rotateAxis;
    enum CoordsType { World, Petroller }
    [SerializeField] private CoordsType logCoords = CoordsType.World;
    public Transform PetrollerTransform;

    // Controller Motion
    // Velocity
    public Vector3 Velocity { get; private set; }
    public Vector3 PetrollerRelativeVelocity { get; private set; }
    [SerializeField] private float speedThreshold = 0.075f; //0.075f
    [SerializeField] private float moveDirThreshold = 0.3f; //0.3f
    public float Speed { get; private set; }
    public bool IsMoving { get; private set; } = false;
    public Vector3 WorldMoveDirection { get; private set; } = Vector3.zero;
    public Vector3 PetrollerRelativeMoveDirection { get; private set; } = Vector3.zero;
    // AngularVelocity
    public Vector3 AngularVelocity { get; private set; }
    public Vector3 PetrollerRelativeAngularVelocity { get; private set; } = Vector3.zero;
    [SerializeField] private float rotateSpeedThreshold = 1f; //1f
    [SerializeField] private float rotateDirThreshold = 0.5f; //0.5f
    public float AngularSpeed { get; private set; }
    public bool IsRotating { get; private set; }
    public Vector3 WorldRotateDirection { get; private set; } = Vector3.zero;
    public Vector3 PetrollerRelativeRotateDirection { get; private set; } = Vector3.zero;
    public Vector3 Acceleration { get; private set; }
    public Vector3 AngularAcceleration { get; private set; }
    private Vector3 oldAngularVelocity;
    private Vector3 oldVelocity;

    // Tracking State
    OVRInput.Controller controller = OVRInput.Controller.RTouch;
    public enum TrackingStatus
    {
        Tracked,
        PresumptiveLostTracked,
        LostTracked
    }
    public TrackingStatus CurrentTrackingState { get; private set; } = TrackingStatus.Tracked;
    bool presumptiveTracked = true;

    void Awake()
    {
        SetTrackingTarget();
        if (!PetrollerTransform) PetrollerTransform = transform;
    }
    void Start()
    {
        oldAngularVelocity = OVRInput.GetLocalControllerAngularVelocity(controller);
        oldVelocity = OVRInput.GetLocalControllerVelocity(controller);
    }
    void Update()
    {
        CurrentTrackingState = HandleTrackingStatus();

        GetPos(OVRInput.GetLocalControllerPosition(controller));
        GetRot(OVRInput.GetLocalControllerRotation(controller));
        GetVelocity(OVRInput.GetLocalControllerVelocity(controller));
        GetAngularVelocity(OVRInput.GetLocalControllerAngularVelocity(controller));
    }
    void FixedUpdate()
    {
        if (Time.fixedDeltaTime <= 0) return;

        Vector3 currentVelocity = OVRInput.GetLocalControllerVelocity(controller);
        Acceleration = AccelerationCalculator(currentVelocity, oldVelocity, Time.fixedDeltaTime);

        Vector3 currentAngularVelocity = OVRInput.GetLocalControllerAngularVelocity(controller);
        AngularAcceleration = AccelerationCalculator(currentAngularVelocity, oldAngularVelocity, Time.fixedDeltaTime);

        oldAngularVelocity = currentAngularVelocity;
        oldVelocity = currentVelocity;
    }
    TrackingStatus HandleTrackingStatus()
    {
        bool isTracked = (OVRInput.GetControllerPositionTracked(controller) & OVRInput.GetControllerOrientationTracked(controller)) ? true : false;
        return isTracked ? (presumptiveTracked ? TrackingStatus.Tracked : TrackingStatus.PresumptiveLostTracked) : TrackingStatus.LostTracked;
    }
    void SetTrackingTarget()
    {
        switch (TargetSensor)
        {
            case Sensor.RHand:
                controller = OVRInput.Controller.RHand;
                break;
            case Sensor.LHand:
                controller = OVRInput.Controller.LHand;
                break;
            case Sensor.RTouch:
                controller = OVRInput.Controller.RTouch;
                break;
            case Sensor.LTouch:
                controller = OVRInput.Controller.LTouch;
                break;
        }
    }
    void GetPos(Vector3 value)
    {
        transform.position = value;
    }
    void GetRot(Quaternion value)
    {
        transform.rotation = value;
    }
    void GetVelocity(Vector3 value)
    {
        Velocity = value;
        PetrollerRelativeVelocity = PetrollerTransform.InverseTransformDirection(Velocity);

        Speed = Velocity.magnitude;

        if (Speed > speedThreshold)
        {
            IsMoving = true;
            WorldMoveDirection = ConvertRelativeDirection(Velocity, moveDirThreshold);
            PetrollerRelativeMoveDirection = ConvertRelativeDirection(PetrollerRelativeVelocity, moveDirThreshold);
        }
        else
        {
            IsMoving = false;
            WorldMoveDirection = Vector3.zero;
            PetrollerRelativeMoveDirection = Vector3.zero;
        }

        if (showDebug)
        {
            foreach (var arrow in dirArrows)
            {
                arrow.SetActive(false);
            }

            Vector3 moveDir = Vector3.zero;
            if (logCoords == CoordsType.World) moveDir = WorldMoveDirection;
            if (logCoords == CoordsType.Petroller) moveDir = PetrollerRelativeMoveDirection;

            if (moveDir.x > 0) dirArrows[0].SetActive(true);
            if (moveDir.x < 0) dirArrows[1].SetActive(true);
            if (moveDir.y > 0) dirArrows[2].SetActive(true);
            if (moveDir.y < 0) dirArrows[3].SetActive(true);
            if (moveDir.z > 0) dirArrows[4].SetActive(true);
            if (moveDir.z < 0) dirArrows[5].SetActive(true);
        }
    }
    void GetAngularVelocity(Vector3 value)
    {
        AngularVelocity = value;
        PetrollerRelativeAngularVelocity = PetrollerTransform.InverseTransformDirection(AngularVelocity);

        AngularSpeed = AngularVelocity.magnitude;

        if (AngularSpeed > rotateSpeedThreshold)
        {
            IsRotating = true;
            WorldRotateDirection = ConvertRelativeDirection(AngularVelocity, rotateDirThreshold);
            PetrollerRelativeRotateDirection = ConvertRelativeDirection(PetrollerRelativeAngularVelocity, rotateDirThreshold);
        }
        else
        {
            IsRotating = false;
            WorldRotateDirection = Vector3.zero;
            PetrollerRelativeRotateDirection = Vector3.zero;
        }

        if (showDebug)
        {
            foreach (var axis in rotateAxis)
            {
                axis.SetActive(false);
            }

            Vector3 rotateDir = Vector3.zero;
            if (logCoords == CoordsType.World) rotateDir = WorldRotateDirection;
            if (logCoords == CoordsType.Petroller) rotateDir = PetrollerRelativeRotateDirection;

            if (Mathf.Abs(rotateDir.x) > 0) rotateAxis[0].SetActive(true);
            if (Mathf.Abs(rotateDir.y) > 0) rotateAxis[1].SetActive(true);
            if (Mathf.Abs(rotateDir.z) > 0) rotateAxis[2].SetActive(true);
        }
    }
    Vector3 ConvertRelativeDirection(Vector3 direction, float threshold)
    {
        Vector3 nm = direction.normalized;

        float dirX = Mathf.Abs(nm.x) > threshold ? (nm.x > 0 ? 1 : -1) : 0;
        float dirY = Mathf.Abs(nm.y) > threshold ? (nm.y > 0 ? 1 : -1) : 0;
        float dirZ = Mathf.Abs(nm.z) > threshold ? (nm.z > 0 ? 1 : -1) : 0;

        return new Vector3(dirX, dirY, dirZ);
    }
    Vector3 AccelerationCalculator(Vector3 currentValue, Vector3 oldValue, float time)
    {
        return (currentValue - oldValue) / time;
    }
    public void ChangePresumptiveTrackingState(bool isTracked)
    {
        presumptiveTracked = isTracked;
    }
}
