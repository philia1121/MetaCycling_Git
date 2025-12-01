using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Condition_Facing", menuName = "JC/Conditions/Facing")]
public class FacingCondition : BaseCondition
{
    private PetrollerObjectInfo cachedInfo;
    [Range(0f, 90f)]
    public float frontAngleThreshold = 45f;
    [Range(0f, 90f)]
    public float backAngleThreshold = 45f;
    public enum FacingState
    {
        Front,
        Back,
        Side
    }

    [SerializeField] private FacingState desiredFacing = FacingState.Front;

    public override bool IsMet(GameObject owner, Transform player)
    {
        if (cachedInfo == null)
        {
            cachedInfo = owner.GetComponent<PetrollerObjectInfo>();
        }
        if (cachedInfo == null) return false;

        return desiredFacing == CalculateFacingDirection(cachedInfo.PetrollerTransform, player);
    }
    private FacingState CalculateFacingDirection(Transform obj, Transform player)
    {
        Vector3 directionToCamera = player.position - obj.position;
        float angle = Vector3.Angle(obj.forward, directionToCamera);


        if (angle <= frontAngleThreshold)
        {
            return FacingState.Front;
        }
        else if (angle >= 180f - backAngleThreshold)
        {
            return FacingState.Back;
        }
        else
        {
            return FacingState.Side;
        }
    }
}
