using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "Condition_AND", menuName = "JC/Conditions/Composite AND")]
public class AndCondition : BaseCondition
{
    public List<BaseCondition> conditions;

    public override bool IsMet(GameObject owner, Transform player)
    {
        return conditions.All(c => c.IsMet(owner, player));
    }
}
