using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Condition_JoystickDir", menuName = "JC/Conditions/Joystick Direction")]
public class PullCondition : BaseCondition
{
    private PetrollerObjectInfo cachedInfo;
    public PetrollerObjectInfo.JoystickDir desiredDir = PetrollerObjectInfo.JoystickDir.Origin;

    public override bool IsMet(GameObject owner, Transform player)
    {
        if (cachedInfo == null)
        {
            cachedInfo = owner.GetComponent<PetrollerObjectInfo>();
        }
        if (cachedInfo == null) return false;

        return cachedInfo.CurrentJoystickDir == desiredDir;
    }
}
