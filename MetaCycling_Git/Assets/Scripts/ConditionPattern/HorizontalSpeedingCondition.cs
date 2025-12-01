using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Condition_HorizontalSpeeding", menuName = "JC/Conditions/Horizontal Speeding")]
public class HorizontalSpeedingCondition : BaseCondition
{
    private PetrollerObjectInfo cachedInfo;
    public float speedingThreshold = 3f;

    public override bool IsMet(GameObject owner, Transform player)
    {
        if (cachedInfo == null)
        {
            cachedInfo = owner.GetComponent<PetrollerObjectInfo>();
        }
        if (cachedInfo == null) return false;

        if (cachedInfo.IsMoving && (Mathf.Abs(cachedInfo.WorldMoveDirection.x) > 0 | Mathf.Abs(cachedInfo.WorldMoveDirection.z) > 0))
        {
            return cachedInfo.Speed > speedingThreshold;
        }
        else
        {
            return false;
        }
    }
}