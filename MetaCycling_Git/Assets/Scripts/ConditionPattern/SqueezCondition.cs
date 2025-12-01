using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Condition_Squeez", menuName = "JC/Conditions/Squeez")]
public class SqueezCondition : BaseCondition
{
    private PetrollerObjectInfo cachedInfo;
    enum SqueezDirection
    {
        Vertical,
        Horizontal
    }
    [SerializeField] private SqueezDirection desiredDir = SqueezDirection.Horizontal;

    public override bool IsMet(GameObject owner, Transform player)
    {
        if (cachedInfo == null)
        {
            cachedInfo = owner.GetComponent<PetrollerObjectInfo>();
        }
        if (cachedInfo == null) return false;

        return (desiredDir == SqueezDirection.Vertical & cachedInfo.VerticalPress) | (desiredDir == SqueezDirection.Horizontal & cachedInfo.HorizontalPress);
    }
}
