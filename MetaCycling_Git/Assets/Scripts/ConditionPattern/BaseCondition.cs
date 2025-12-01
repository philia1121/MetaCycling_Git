using UnityEngine;

public abstract class BaseCondition : ScriptableObject
{
    public abstract bool IsMet(GameObject owner, Transform player);
}
