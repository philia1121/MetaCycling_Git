using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PetrollerModelHandler : MonoBehaviour
{
    [SerializeField] private PetrollerObjectInfo petrollerInfo;

    [Header("Model and Appearence Settings")]
    [SerializeField] private bool autoInitializeModel;
    [SerializeField] private Transform modelParentTransform, shellParentTransform;
    [SerializeField] private Transform modelTransform, shellTransform;
    [SerializeField] private Material modelMaterial;
    [SerializeField] private Color[] modelColors = new Color[2];
    [SerializeField] private SolutionSetting modelOffsetSetting;


    [Header("Hand Tracking")]
    public HandModelHandler LHandHandler;
    public HandModelHandler RHandHandler;
    enum HandTrackingStatus { L, R, Both, None };
    HandTrackingStatus currentHandTrackingState = HandTrackingStatus.Both;
    HandTrackingStatus oldHandTrackingState;

    [Header("Debug Settings")]
    public bool useDebugStatus;
    bool oldUseDebug;
    public PetrollerObjectInfo.TrackingStatus debugStatus;
    PetrollerObjectInfo.TrackingStatus oldDebugStatus;

    [Header("Snap Solution Settings")]
    [SerializeField] private LostTrackedSolution solution = LostTrackedSolution.StayLastKnown;
    LostTrackedSolution finalSolution, oldFinalSolution;
    public enum LostTrackedSolution { StayLastKnown, Fixed, SnapToCenter, SnapToRightHand, SnapToLeftHand, AsDefault }
    [SerializeField] private SnapHandException snapHandException = SnapHandException.Available;
    enum SnapHandException { StayLastKnown, Available }
    [SerializeField] private SolutionSetting[] settings;
    private Dictionary<LostTrackedSolution, SolutionSetting> SolutionSettingPairs = new Dictionary<LostTrackedSolution, SolutionSetting>();
    PetrollerObjectInfo.ControllerPairing oldConnection = PetrollerObjectInfo.ControllerPairing.Connected;
    PetrollerObjectInfo.TrackingStatus oldTrackingState;
    bool onChange = false;
    Vector3 modelLastKnownPos, shellLastKnownPos;
    Quaternion modelLastKnownRot, shellLastKnownRot;
    void Awake()
    {
        foreach (var setting in settings)
        {
            SolutionSettingPairs.Add(setting.lostTrackedSolution, setting);
        }
    }
    void Start()
    {
        if (modelTransform == null) modelTransform = this.transform.GetChild(0).transform;

        if (autoInitializeModel)
        {
            modelTransform.position = SolutionSettingPairs[LostTrackedSolution.AsDefault].offsetPos;
            modelTransform.rotation = Quaternion.Euler(SolutionSettingPairs[LostTrackedSolution.AsDefault].offsetRot);
        }

        HandelAppearence();
        HandelOffset();

        oldTrackingState = petrollerInfo.CurrentTrackingState;
        oldDebugStatus = debugStatus;
        oldUseDebug = useDebugStatus;
        oldHandTrackingState = CheckHandTrackingStatus();
    }

    void Update()
    {
        // For the scenario in which the R controller is not connected to the HMD
        if (petrollerInfo.CurrentControllerConnection != oldConnection)
        {
            modelTransform.gameObject.SetActive(petrollerInfo.CurrentControllerConnection == PetrollerObjectInfo.ControllerPairing.Connected ? true : false);
        }
        oldConnection = petrollerInfo.CurrentControllerConnection;

        // Check for Hand Tracking
        currentHandTrackingState = CheckHandTrackingStatus();

        // On State Changed
        onChange = ((oldTrackingState != petrollerInfo.CurrentTrackingState)
         | (useDebugStatus && oldDebugStatus != debugStatus)
         | (useDebugStatus && oldUseDebug != useDebugStatus)
         | (oldHandTrackingState != currentHandTrackingState)) ? true : false;
        if (onChange)
        {
            HandelAppearence();
            HandelOffset();
            onChange = false;
        }

        // updating position and rotation
        HandelModelUpdate();
        HandelShellUpdate();

        // update info for next frame
        oldTrackingState = petrollerInfo.CurrentTrackingState;
        oldDebugStatus = debugStatus;
        oldUseDebug = useDebugStatus;
        oldHandTrackingState = currentHandTrackingState;
        oldFinalSolution = finalSolution;

        // shellLastKnownPos = shellParentTransform.position;
        // shellLastKnownRot = shellParentTransform.rotation;
        Debug.Log("Log Last Known Pos: " + shellLastKnownPos + ", LHand Tracking: " + currentHandTrackingState);
        Debug.Log("Log Last Known Rot: " + shellLastKnownRot + ", " + Quaternion.Angle(shellLastKnownRot, new Quaternion(0, 0, 0, 1)));
    }

    void HandelAppearence()
    {
        switch (useDebugStatus ? debugStatus : petrollerInfo.CurrentTrackingState)
        {
            case PetrollerObjectInfo.TrackingStatus.Tracked:
                shellParentTransform.gameObject.SetActive(false);
                modelMaterial.color = modelColors[0];
                break;
            case PetrollerObjectInfo.TrackingStatus.PresumptiveLostTracked:
                shellParentTransform.gameObject.SetActive(true);
                modelMaterial.color = modelColors[1];
                break;
            case PetrollerObjectInfo.TrackingStatus.LostTracked:
                shellParentTransform.gameObject.SetActive(true);
                modelMaterial.color = modelColors[1];
                break;
        }
    }
    void HandelOffset()
    {
        modelTransform.localPosition = modelOffsetSetting.offsetPos;
        modelTransform.localEulerAngles = modelOffsetSetting.offsetRot;
        shellTransform.localPosition = SolutionSettingPairs[solution].offsetPos;
        shellTransform.localPosition = SolutionSettingPairs[solution].offsetRot;
    }
    void HandelModelUpdate()
    {
        if ((useDebugStatus & debugStatus == PetrollerObjectInfo.TrackingStatus.LostTracked)
         | (!useDebugStatus & petrollerInfo.CurrentTrackingState == PetrollerObjectInfo.TrackingStatus.LostTracked))
        {
            // stay at last known position and rotation when lost tracked
            modelParentTransform.position = modelLastKnownPos;
            modelParentTransform.rotation = modelLastKnownRot;
        }
        else
        {
            // keep following wherever the controller is
            modelParentTransform.position = petrollerInfo.transform.position;
            modelParentTransform.rotation = petrollerInfo.transform.rotation;

            modelLastKnownPos = modelParentTransform.transform.position;
            modelLastKnownRot = modelParentTransform.transform.rotation;
        }
    }
    void HandelShellUpdate()
    {
        if ((useDebugStatus & debugStatus == PetrollerObjectInfo.TrackingStatus.Tracked) | (!useDebugStatus & petrollerInfo.CurrentTrackingState == PetrollerObjectInfo.TrackingStatus.Tracked)) return;

        Vector3 finalPos = Vector3.zero;
        Quaternion finalRot = Quaternion.identity;
        Transform L_TF = LHandHandler.PalmTransform;
        Transform R_TF = RHandHandler.PalmTransform;

        finalSolution = solution;
        if (snapHandException == SnapHandException.Available)
        {
            if (solution == LostTrackedSolution.SnapToCenter & currentHandTrackingState != HandTrackingStatus.Both)
            {
                if (currentHandTrackingState != HandTrackingStatus.None)
                {
                    finalSolution = currentHandTrackingState == HandTrackingStatus.R ? LostTrackedSolution.SnapToRightHand : LostTrackedSolution.SnapToLeftHand;
                }
            }

            if (solution == LostTrackedSolution.SnapToRightHand & currentHandTrackingState != HandTrackingStatus.Both & currentHandTrackingState != HandTrackingStatus.R)
            {
                finalSolution = currentHandTrackingState == HandTrackingStatus.L ? LostTrackedSolution.SnapToLeftHand : finalSolution;
            }

            if (solution == LostTrackedSolution.SnapToLeftHand & currentHandTrackingState != HandTrackingStatus.Both & currentHandTrackingState != HandTrackingStatus.L)
            {
                finalSolution = currentHandTrackingState == HandTrackingStatus.R ? LostTrackedSolution.SnapToRightHand : finalSolution;
            }
        }
        if (finalSolution != oldFinalSolution)
        {
            HandelOffset();
        }

        switch (finalSolution)
        {
            case LostTrackedSolution.StayLastKnown:
                finalPos = SolutionSettingPairs[solution].snapPos;
                finalRot = Quaternion.Euler(SolutionSettingPairs[solution].snapRot);
                UpdateShellFinalCoords(finalPos, finalRot);
                break;
            case LostTrackedSolution.Fixed:
                finalPos = SolutionSettingPairs[solution].snapPos;
                finalRot = Quaternion.Euler(SolutionSettingPairs[solution].snapRot);
                UpdateShellFinalCoords(finalPos, finalRot);
                break;
            case LostTrackedSolution.SnapToCenter:
                if (currentHandTrackingState == HandTrackingStatus.Both)
                {
                    Vector3 LHandPos = L_TF.position;
                    Vector3 RHandPos = R_TF.position;

                    finalPos = Vector3.Lerp(LHandPos, RHandPos, 0.5f);

                    Vector3 leftForward = L_TF.forward;
                    Vector3 leftRight = L_TF.right;

                    Vector3 rightForward = R_TF.forward;
                    Vector3 rightRight = -R_TF.right;

                    Vector3 leftUp = Vector3.Cross(leftForward, leftRight);
                    Quaternion leftStandardRot = Quaternion.LookRotation(leftForward, leftUp);

                    Vector3 rightUp = Vector3.Cross(rightForward, rightRight);
                    Quaternion rightStandardRot = Quaternion.LookRotation(rightForward, rightUp);

                    finalRot = Quaternion.Slerp(leftStandardRot, rightStandardRot, 0.5f);
                    UpdateShellFinalCoords(finalPos, finalRot);
                }
                else
                {
                    finalPos = shellLastKnownPos;
                    finalRot = shellLastKnownRot;
                }
                break;
            case LostTrackedSolution.SnapToRightHand:
                if (currentHandTrackingState == HandTrackingStatus.Both | currentHandTrackingState == HandTrackingStatus.R)
                {
                    finalPos = R_TF.position;
                    finalRot = R_TF.rotation;
                    UpdateShellFinalCoords(finalPos, finalRot);
                }
                else
                {
                    finalPos = shellLastKnownPos;
                    finalRot = shellLastKnownRot;
                }
                break;
            case LostTrackedSolution.SnapToLeftHand:
                if (currentHandTrackingState == HandTrackingStatus.Both | currentHandTrackingState == HandTrackingStatus.L)
                {
                    finalPos = L_TF.position;
                    finalRot = L_TF.rotation;
                    UpdateShellFinalCoords(finalPos, finalRot);
                }
                else
                {
                    finalPos = shellLastKnownPos;
                    finalRot = shellLastKnownRot;
                }
                break;
        }
        shellParentTransform.position = finalPos;
        shellParentTransform.rotation = finalRot;
    }
    HandTrackingStatus CheckHandTrackingStatus()
    {
        bool L = LHandHandler.isTrackingGood && Quaternion.Angle(LHandHandler.PalmTransform.rotation, new Quaternion(0, 0, 0, 1)) != 0;
        bool R = RHandHandler.isTrackingGood && Quaternion.Angle(RHandHandler.PalmTransform.rotation, new Quaternion(0, 0, 0, 1)) != 0;

        if (L & R) return HandTrackingStatus.Both;
        if (L) return HandTrackingStatus.L;
        if (R) return HandTrackingStatus.R;

        return HandTrackingStatus.None;
    }
    void UpdateShellFinalCoords(Vector3 pos, Quaternion rot)
    {
        shellLastKnownPos = pos;
        shellLastKnownRot = rot;
    }
}

[System.Serializable]
public class SolutionSetting
{
    public PetrollerModelHandler.LostTrackedSolution lostTrackedSolution;
    public Vector3 snapPos;
    public Vector3 snapRot;
    public Vector3 offsetPos;
    public Vector3 offsetRot;
}
