using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Condition_MovingState", menuName = "JC/Conditions/Moving State")]
public class MovingStateCondition : BaseCondition
{
    private PetrollerObjectInfo cachedInfo;
    public bool desiredState = true;

    public override bool IsMet(GameObject owner, Transform player)
    {
        if (cachedInfo == null)
        {
            cachedInfo = owner.GetComponent<PetrollerObjectInfo>();
        }
        if (cachedInfo == null) return false;

        return desiredState == cachedInfo.IsMoving;
    }
}
