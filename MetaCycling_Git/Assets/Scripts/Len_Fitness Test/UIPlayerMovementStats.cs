using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIPlayerMovementStats : MonoBehaviour
{
    [SerializeField] private TMP_Text infoText;

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

    public void ChangeDisplay(string type)
    {
        FetchMotionData();

        ClearDisplay();
        switch (type.ToLower())
        {
            case "vertical jump":
                HandleVerticalJump();
                break;
            case "long jump":
                HandleLongJump();
                break;
            case "jump rope":
                HandleJumpRope();
                break;
            case "speed walking":
                HandleWalking(true);
                break;
            case "walking":
                HandleWalking(false);
                break;
            case "row machine":
                HandleRowing();
                break;
            case "others":
                HandleWalking(true);
                break;
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
    private void HandleVerticalJump()
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
        infoText.text = $"Est. Height: {startHeight*100f:F1}cm \n Highest Point: {jumpHeight:F1} cm \n JumpHeight: {jumpHeight:F1} cm";
    }

    private void HandleLongJump()
    {
        if (hmdMotionPoints.Count < 2) return;

        // Total horizontal displacement (ignoring Y)
        Vector3 start = hmdMotionPoints[0].position;
        Vector3 end = hmdMotionPoints[hmdMotionPoints.Count - 1].position;
        start.y = 0; end.y = 0;

        float dist = CalcDist(start, end)* 100f;
        infoText.text = $"Jump Distance: {dist:F1} cm";
    }
    private void HandleJumpRope()
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

        infoText.text = $"Est. jumps: {jumps-1}";
    }

    private void HandleRowing()
    {
        int reps = 0;
        float maxSpeed = 0;
        bool isPulling = false;

        // Detection Settings
        float pullVelocityThreshold = 0.3f; // Must be pulling at least this fast
        float returnVelocityThreshold = -0.1f; // Speed to reset for next rep
        float finishDistance = 0.4f; // Must be within 45cm of HMD to count as "at the chest"

        for (int i = 1; i < lHandMotionPoints.Count; i++)
        {
            // 1. Calculate the Bar (Midpoint)
            Vector3 barPosCurr = (lHandMotionPoints[i].position + rHandMotionPoints[i].position) / 2f;
            Vector3 barPosPrev = (lHandMotionPoints[i - 1].position + rHandMotionPoints[i - 1].position) / 2f;
            Vector3 hmdPos = hmdMotionPoints[i].position;

            // 2. Distances and Velocity
            float distCurr = Vector3.Distance(barPosCurr, hmdPos);
            float distPrev = Vector3.Distance(barPosPrev, hmdPos);

            // Relative velocity toward the chest (Positive = Pulling)
            float relVel = (distPrev - distCurr) / 0.015f;

            // 3. Log Max Speed (Peak Velocity during any pull)
            if (relVel > maxSpeed) maxSpeed = relVel;

            // 4. Rep State Machine
            if (!isPulling)
            {
                // Transition to Pulling: Fast movement toward chest
                if (relVel > pullVelocityThreshold)
                {
                    isPulling = true;
                }
            }
            else
            {
                // Transition to Rep Complete: 
                // Velocity drops (stop) AND they are actually near the chest
                if (relVel < 0.05f && distCurr < finishDistance)
                {
                    reps++;
                    isPulling = false;
                }
                // Fail-safe: If they push back out without finishing, reset state
                else if (relVel < returnVelocityThreshold)
                {
                    isPulling = false;
                }
            }
        }

        infoText.text = $"Reps: {reps}\n" +
                        $"Max Speed: {maxSpeed:F1} m/s";
    }

    private void HandleWalking(bool speedStats)
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
