using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
public class PetrollerObjectInfo : MonoBehaviour
{
    ControlMap controlMap;
    [SerializeField] private bool useOVRInput = true;

    // For Debug //
    public bool showDebug;
    public Material my_mat;
    public GameObject[] dirArrows;
    public GameObject[] rotateAxis;
    enum CoordsType { World, Petroller }
    [SerializeField] private CoordsType logCoords = CoordsType.World;
    public Transform PetrollerTransform;

    // Joystick and Button // 
    public Vector2 JoystickRead { get; private set; }
    public enum JoystickDir { Origin, Ear, Tail, LeftHand, RightHand }
    public JoystickDir CurrentJoystickDir { get; private set; } = JoystickDir.Origin;
    public bool VerticalPress { get; private set; }
    public bool HorizontalPress { get; private set; }

    // Controller Motion //
    // Velocity // 
    public Vector3 Velocity { get; private set; }
    public Vector3 PetrollerRelativeVelocity { get; private set; }
    [SerializeField] private float speedThreshold = 0.75f; //0.75f
    [SerializeField] private float moveDirThreshold = 0.3f; //0.3f
    public float Speed { get; private set; }
    public bool IsMoving { get; private set; } = false;
    public Vector3 WorldMoveDirection { get; private set; } = Vector3.zero;
    public Vector3 PetrollerRelativeMoveDirection { get; private set; } = Vector3.zero;

    // AngularVelocity //
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

    // Impact //
    public int LastImpactFrame { get; private set; } = -1;
    public float LastImpactSpeed { get; private set; } = 0f;
    public string LastImpactZoneID { get; private set; } = "";

    // Tracking State //
    [SerializeField] private OVRInput.Controller controller = OVRInput.Controller.RTouch;
    public enum TrackingStatus
    { Tracked, PresumptiveLostTracked, LostTracked }
    public TrackingStatus CurrentTrackingState = TrackingStatus.Tracked;
    bool presumptiveTracked = true;
    public enum ControllerPairing { Connected, Disconnected }
    public ControllerPairing CurrentControllerConnection { get; private set; } = ControllerPairing.Connected;

    // Tracking Zone //
    private HashSet<string> currentZoneIDs = new HashSet<string>();

    void Awake()
    {
        controlMap = new ControlMap();
        if (!PetrollerTransform) PetrollerTransform = transform;
    }
    void OnEnable()
    {
        controlMap.Petroller.Enable();

        controlMap.Petroller.Pull.started += GetAction_Pull;
        controlMap.Petroller.Pull.performed += GetAction_Pull;
        controlMap.Petroller.Pull.canceled += GetAction_Pull;
        controlMap.Petroller.VerticalPress.started += GetAction_VerticalPress;
        controlMap.Petroller.VerticalPress.canceled += GetAction_VerticalPress;
        controlMap.Petroller.HorizontalPress.started += GetAction_HorizontalPress;
        controlMap.Petroller.HorizontalPress.canceled += GetAction_HorizontalPress;

        if (useOVRInput) return;
        controlMap.Petroller.Pos.performed += GetAction_Pos;
        controlMap.Petroller.Rot.performed += GetAction_Rot;
        controlMap.Petroller.DeviceVelocity.performed += GetAction_Velocity;
        controlMap.Petroller.DeviceAngularVelocity.performed += GetAction_AngularVelocity;
    }
    void OnDisable()
    {
        controlMap.Petroller.Pull.started -= GetAction_Pull;
        controlMap.Petroller.Pull.performed -= GetAction_Pull;
        controlMap.Petroller.Pull.canceled -= GetAction_Pull;
        controlMap.Petroller.VerticalPress.started -= GetAction_VerticalPress;
        controlMap.Petroller.VerticalPress.canceled -= GetAction_VerticalPress;
        controlMap.Petroller.HorizontalPress.started -= GetAction_HorizontalPress;
        controlMap.Petroller.HorizontalPress.canceled -= GetAction_HorizontalPress;

        if (useOVRInput) return;
        controlMap.Petroller.Pos.performed -= GetAction_Pos;
        controlMap.Petroller.Rot.performed -= GetAction_Rot;
        controlMap.Petroller.DeviceVelocity.performed -= GetAction_Velocity;
        controlMap.Petroller.DeviceAngularVelocity.performed -= GetAction_AngularVelocity;

        controlMap.Petroller.Disable();
    }
    void Start()
    {
        oldAngularVelocity = OVRInput.GetLocalControllerAngularVelocity(controller);
        oldVelocity = OVRInput.GetLocalControllerVelocity(controller);
    }
    void Update()
    {
        string message = OVRInput.connectedControllerTypes.ToString();
        CurrentControllerConnection = (message.Contains("RTouch") || (message.Contains("Touch") && !message.Contains("LTouch"))) ? ControllerPairing.Connected : ControllerPairing.Disconnected;
        if (CurrentControllerConnection == ControllerPairing.Disconnected) return;

        CurrentTrackingState = HandleTrackingStatus();

        if (!useOVRInput) return;
        // Debug.Log(OVRInput.connectedControllerTypes);
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
    void GetAction_Pull(InputAction.CallbackContext ctx)
    {
        Vector2 value = ctx.ReadValue<Vector2>();
        JoystickRead = value;
        if (value == Vector2.zero)
        {
            CurrentJoystickDir = JoystickDir.Origin;
        }
        else
        {
            float x = Mathf.Abs(value.x);
            float y = Mathf.Abs(value.y);

            if ((x > y) & value.x > 0)
            {
                CurrentJoystickDir = JoystickDir.RightHand;
            }
            else if ((x > y) & value.x < 0)
            {
                CurrentJoystickDir = JoystickDir.LeftHand;
            }
            else if ((x < y) & value.y > 0)
            {
                CurrentJoystickDir = JoystickDir.Ear;
            }
            else if ((x < y) & value.y < 0)
            {
                CurrentJoystickDir = JoystickDir.Tail;
            }
        }
    }
    void GetAction_VerticalPress(InputAction.CallbackContext ctx)
    {
        float value = ctx.ReadValue<float>();
        VerticalPress = value > 0.5f ? true : false;
    }
    void GetAction_HorizontalPress(InputAction.CallbackContext ctx)
    {
        float value = ctx.ReadValue<float>();
        HorizontalPress = value > 0.5f ? true : false;
    }
    void GetAction_Pos(InputAction.CallbackContext ctx)
    {
        Vector3 value = ctx.ReadValue<Vector3>();
        GetPos(value);
    }
    void GetPos(Vector3 value)
    {
        transform.position = value;
    }
    void GetAction_Rot(InputAction.CallbackContext ctx)
    {
        Quaternion value = ctx.ReadValue<Quaternion>();
        GetRot(value);
    }
    void GetRot(Quaternion value)
    {
        transform.rotation = value;
    }
    void GetAction_Velocity(InputAction.CallbackContext ctx)
    {
        Vector3 value = ctx.ReadValue<Vector3>();
        GetVelocity(value);
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
    void GetAction_AngularVelocity(InputAction.CallbackContext ctx)
    {
        Vector3 value = ctx.ReadValue<Vector3>();
        GetAngularVelocity(value);
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

    // Extension Function //
    public void ChangePresumptiveTrackingState(bool tracked) { presumptiveTracked = tracked; }
    public void AddZoneID(string zoneID) { currentZoneIDs.Add(zoneID); }
    public void RemoveZoneID(string zoneID) { currentZoneIDs.Remove(zoneID); }
    public bool IsInZone(string zoneID) { return currentZoneIDs.Contains(zoneID); }
    public void UpdateSlapInfo(int frame, string id)
    {
        LastImpactFrame = frame;
        LastImpactSpeed = Speed;
        LastImpactZoneID = id;
    }
}
