using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Condition_NOT", menuName = "JC/Conditions/Composite NOT")]
public class NotCondition : BaseCondition
{
    public BaseCondition conditionToReverse;
    public override bool IsMet(GameObject owner, Transform player)
    {
        if (conditionToReverse == null) return false;
        return !conditionToReverse.IsMet(owner, player);
    }
}
