using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIPlayerMovementStats : MonoBehaviour
{
    [SerializeField] private TMP_Text infoText;

    [SerializeField] private AudioSource source;
    [SerializeField] private AudioClip effectSFX;

    private LiveRowingTracker rowingTracker = new LiveRowingTracker();
    private bool isRowing = false;
    private float distPrev;
    private PathVisualizer m_path;


    List<MotionPointSimple> hmdMotionPoints;
    List<MotionPointSimple> rHandMotionPoints;
    List<MotionPointSimple> lHandMotionPoints;

    private void Start()
    {
        m_path = PathVisualizer.instance;
        hmdMotionPoints = new List<MotionPointSimple>();
        rHandMotionPoints = new List<MotionPointSimple>();
        lHandMotionPoints = new List<MotionPointSimple>();
    }

    public void ClearDisplay()
    {
        infoText.text = "";
    }
    public void ChangeDisplayActiveState(bool _b)
    {
        infoText.gameObject.SetActive(_b);
    }

    public void ChangeDisplay(string type, bool _printInfo)
    {
        FetchMotionData();

        ClearDisplay();
        switch (type.ToLower())
        {
            case "vertical jump":
                HandleVerticalJump(_printInfo);
                break;
            case "long jump":
                HandleLongJump(_printInfo);
                break;
            case "jump rope":
                HandleJumpRope(_printInfo);
                break;
            case "speed walking":
                HandleWalking(true, _printInfo);
                break;
            case "walking":
                HandleWalking(false, _printInfo);
                break;
            case "row machine":
                HandleRowing(_printInfo);
                break;
            case "others":
                HandleWalking(true, _printInfo);
                break;
        }
    }

    private void FixedUpdate()
    {
        if(!isRowing)
            return;

        // Detection Settings
        float pullVelocityThreshold = 0.15f; // Must be pulling at least this fast
        float returnVelocityThreshold = -0.12f; // Speed to reset for next rep
        float finishDistance = 0.4f; // Must be within n cm of HMD to count as "at the chest"
        float recoveryDistance = 0.51f; //user must kinda release their hand to this distance for it to count again

        // calc bar and head pos
        Vector3 barPosCurr = m_path.LHand_Transform.position;
        Vector3 hmdPos = m_path.HMD_Transform.position;
        float distCurr = Vector3.Distance(barPosCurr, hmdPos);

        float relVel = 0f;
        if (rowingTracker.distPrev > 0f)
        {
            relVel = (rowingTracker.distPrev - distCurr) / Time.fixedDeltaTime;
        }
        rowingTracker.distPrev = distCurr;

        if (relVel > rowingTracker.maxSpeed) rowingTracker.maxSpeed = relVel;

        if (rowingTracker.needsRecovery)
        {
            if (distCurr > recoveryDistance || relVel < returnVelocityThreshold)
            {
                rowingTracker.needsRecovery = false;
            }
            return; // Exit frame safely
        }

        // 6. Rep State Machine Processing
        if (!rowingTracker.isPulling)
        {
            if (relVel > pullVelocityThreshold)
            {
                rowingTracker.isPulling = true;
            }
        }
        else
        {
            if (relVel < 0.05f && distCurr < finishDistance)
            {
                rowingTracker.reps++;
                rowingTracker.isPulling = false;
                rowingTracker.needsRecovery = true;

                source.PlayOneShot(effectSFX);
            }
            else if (relVel < returnVelocityThreshold)
            {
                rowingTracker.isPulling = false;
            }
        }
    }

    public void UpdateMovementType(string name, bool active)
    {
        if (name.ToLower() != "row  machine")
            return;
        rowingTracker.Reset();
        isRowing = active;
    }

    private void HandleLiveRowing()
    {
        bool isPulling = false; //check pulling or not
        bool needsRecovery = false; //extra check to stop the bouncing

        // Detection Settings
        float pullVelocityThreshold = 0.15f; // Must be pulling at least this fast
        float returnVelocityThreshold = -0.12f; // Speed to reset for next rep
        float finishDistance = 0.4f; // Must be within n cm of HMD to count as "at the chest"
        float recoveryDistance = 0.51f; //user must kinda release their hand to this distance for it to count again

        // calc bar and head pos
        Vector3 barPosCurr = m_path.LHand_Transform.position;
        Vector3 hmdPos = m_path.HMD_Transform.position;
        float distCurr = Vector3.Distance(barPosCurr, hmdPos);

        float relVel = 0f;
        if (distPrev > 0f) // Skip the very first frame so we don't get a massive spike
        {
            // (distPrev - distCurr) is positive when hands are moving closer to the HMD (chest)
            relVel = (distPrev - distCurr) / Time.fixedDeltaTime;
        }

        // Save current distance for the next frame's calculation
        distPrev = distCurr;

        if (needsRecovery)
        {
            if (distCurr > recoveryDistance || relVel < returnVelocityThreshold)
            {
                needsRecovery = false; // pushed the bar away
            }
            return; // skip till they finish recover
        }

        // Rep State Machine
        if (!isPulling)
        {
            if (relVel > pullVelocityThreshold)
            {
                isPulling = true;
            }
        }
        else
        {
            // Successfully finished pulling
            if (relVel < 0.05f && distCurr < finishDistance)
            {
                isPulling = false;
                needsRecovery = true; // LOCK the state until they extend arms
                source.PlayOneShot(effectSFX);
            }
            // Fail-safe: aborted stroke
            else if (relVel < returnVelocityThreshold)
            {
                isPulling = false;
            }
        }

    }

    private void FetchMotionData()
    {
        hmdMotionPoints.Clear();
        rHandMotionPoints.Clear();
        lHandMotionPoints.Clear();

        hmdMotionPoints = m_path.hmdMotionPoints;
        rHandMotionPoints = m_path.rHandMotionPoints;
        lHandMotionPoints = m_path.lHandMotionPoints;
    }
    private void HandleVerticalJump( bool _printInfo)
    {
        // We look for the highest Y point reached by the HMD (head) or Hands
        float maxY = -Mathf.Infinity;
        Vector3 start = hmdMotionPoints[0].position; //this is the hmd height

        foreach (var p in hmdMotionPoints)
        {
            if (p.position.y > maxY) maxY = p.position.y;
        }
        foreach (var p in lHandMotionPoints)
        {
            if (p.position.y > maxY) maxY = p.position.y;
        }
        foreach (var p in rHandMotionPoints)
        {
            if (p.position.y > maxY) maxY = p.position.y;
        }

        float startHeight = start.y;
        float jumpHeight = (maxY - startHeight) * 100f;
        if(_printInfo)
            infoText.text = $"Est. Height: {startHeight*100f:F1}cm \n Highest Point: {jumpHeight:F1} cm \n JumpHeight: {jumpHeight:F1} cm";
    }

    private void HandleLongJump(bool _printInfo)
    {
        if (hmdMotionPoints.Count < 2) return;

        // Total horizontal displacement (ignoring Y)
        Vector3 start = hmdMotionPoints[0].position;
        Vector3 end = hmdMotionPoints[hmdMotionPoints.Count - 1].position;
        start.y = 0; end.y = 0;

        float dist = CalcDist(start, end)* 100f;
        if(_printInfo)
            infoText.text = $"Jump Distance: {dist:F1} cm";
    }
    private void HandleJumpRope(bool _printInfo)
    {
        if (hmdMotionPoints.Count < 10) return;

        int jumps = 0;
        bool isRising = false;

        // We need to establish the 'Floor' (standing height)
        float floorY = hmdMotionPoints[0].position.y;
        float jumpThreshold = 0.05f; // jump height required to count as a jump
        float landingThreshold = 0.02f; // return to within howevery many cm of floor to 'reset'

        foreach (var p in hmdMotionPoints)
        {
            float currentY = p.position.y;

            // STATE 1: We are on the ground, looking for a jump
            if (!isRising)
            {
                if (currentY > floorY + jumpThreshold)
                {
                    jumps++;
                    isRising = true; // We are now 'in the air'
                }
            }
            // STATE 2: We are in the air, looking for a landing
            else
            {
                if (currentY < floorY + landingThreshold)
                {
                    isRising = false; // We landed, ready for next jump
                }
            }

            // Dynamic Floor Adjustment (Optional)
            // If the user's standing height drifts, slowly nudge floorY
            // floorY = Mathf.Lerp(floorY, currentY, 0.001f); 
        }
        if(_printInfo)
            infoText.text = $"Est. jumps: {jumps-1}";
    }

    private void HandleRowing(bool _printInfo)
    {
        int reps = 0;
        float maxSpeed = 0;

        bool isPulling = false; //check pulling or not
        bool needsRecovery = false; //extra check to stop the bouncing

        // Detection Settings
        float pullVelocityThreshold = 0.15f; // Must be pulling at least this fast
        float returnVelocityThreshold = -0.12f; // Speed to reset for next rep
        float finishDistance = 0.4f; // Must be within n cm of HMD to count as "at the chest"
        float recoveryDistance = 0.51f; //user must kinda release their hand to this distance for it to count again

        for (int i = 1; i < lHandMotionPoints.Count; i++)
        {
            // calc bar and head pos
            Vector3 barPosCurr = lHandMotionPoints[i].position; //(lHandMotionPoints[i].position + rHandMotionPoints[i].position) / 2f;
            Vector3 barPosPrev = lHandMotionPoints[i - 1].position; //(lHandMotionPoints[i - 1].position + rHandMotionPoints[i - 1].position) / 2f;
            Vector3 hmdPos = hmdMotionPoints[i].position;

            // calc distance and velocity
            float distCurr = Vector3.Distance(barPosCurr, hmdPos);
            float distPrev = Vector3.Distance(barPosPrev, hmdPos);

            // relative velocity toward the chest (Positive = Pulling)
            float relVel = (distPrev - distCurr) / 0.015f;

            // log max speed for extra flavour basically
            if (relVel > maxSpeed) maxSpeed = relVel;

            //check is player kinda recovering or not
            if (needsRecovery)
            {
                if (distCurr > recoveryDistance || relVel < returnVelocityThreshold)
                {
                    needsRecovery = false; // pushed the bar away
                }
                continue; // skip till they finish recover
            }

            // Rep State Machine
            if (!isPulling)
            {
                if (relVel > pullVelocityThreshold)
                {
                    isPulling = true;
                }
            }
            else
            {
                // Successfully finished pulling
                if (relVel < 0.05f && distCurr < finishDistance)
                {
                    reps++;
                    isPulling = false;
                    needsRecovery = true; // LOCK the state until they extend arms
                }
                // Fail-safe: aborted stroke
                else if (relVel < returnVelocityThreshold)
                {
                    isPulling = false;
                }
            }
        }
        if(_printInfo)
            infoText.text = $"Reps: {reps}\n" + $"Max Speed: {maxSpeed:F1} m/s";
    }

    private void HandleWalking(bool speedStats, bool _printinfo)
    {
        float totalDist = 0;
        float maxSpeed = 0;

        // Using a 4-frame window for smoothing (approx .015*4s)
        int windowSize = 4;

        for (int i = 1; i < hmdMotionPoints.Count; i++)
        {
            float d = Vector3.Distance(hmdMotionPoints[i].position, hmdMotionPoints[i - 1].position);
            totalDist += d;

            if (speedStats && i >= windowSize)
            {
                // Calculate distance over the last 4 frames
                float windowDist = Vector3.Distance(hmdMotionPoints[i].position, hmdMotionPoints[i - windowSize].position);
                float windowTime = 0.015f * windowSize;

                float smoothedSpeed = windowDist / windowTime;

                if (smoothedSpeed > maxSpeed) maxSpeed = smoothedSpeed;
            }
        }
        if (!_printinfo)
            return;
        if (speedStats)
        {
            infoText.text = $"traveled {totalDist:F1}m\n" +
                            $"Max: {maxSpeed:F1}m/s";
        }
        else
        {
            infoText.text = $"Total Distance: {totalDist:F1} m";
        }
    }

    private float CalcDist(Vector3 start, Vector3 end)
    {
        float sqDist = (end - start).sqrMagnitude;
        return (MathF.Sqrt(sqDist));
    }
}

[System.Serializable]
public class LiveRowingTracker
{
    public bool isPulling = false;
    public bool needsRecovery = false;
    public float distPrev = 0f;
    public int reps = 0;
    public float maxSpeed = 0f;

    public void Reset()
    {
        isPulling = false;
        needsRecovery = false;
        distPrev = 0f;
        reps = 0;
        maxSpeed = 0f;
    }
}