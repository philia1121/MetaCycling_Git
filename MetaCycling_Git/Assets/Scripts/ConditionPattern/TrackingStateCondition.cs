using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Condition_TrackingState", menuName = "JC/Conditions/Tracking State")]
public class TrackingStateCondition : BaseCondition
{
    private PetrollerObjectInfo cachedInfo;
    public PetrollerObjectInfo.TrackingStatus desiredState = PetrollerObjectInfo.TrackingStatus.Tracked;

    public override bool IsMet(GameObject owner, Transform player)
    {
        if (cachedInfo == null)
        {
            cachedInfo = owner.GetComponent<PetrollerObjectInfo>();
        }
        if (cachedInfo == null) return false;

        return cachedInfo.CurrentTrackingState == desiredState;
    }
}
