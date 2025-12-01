using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "Condition_OR", menuName = "JC/Conditions/Composite OR")]
public class OrCondition : BaseCondition
{
    public List<BaseCondition> conditions;

    public override bool IsMet(GameObject owner, Transform player)
    {
        return conditions.Any(c => c.IsMet(owner, player));
    }
}
