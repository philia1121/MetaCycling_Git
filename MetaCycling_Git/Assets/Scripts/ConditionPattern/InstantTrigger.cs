using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstantTrigger : MonoBehaviour
{
    public string conditionName;
    public GameObject owner, player;
    public BaseCondition condition;
    bool fired;

    // Update is called once per frame
    void Update()
    {
        bool met = condition.IsMet(owner, player.transform);
        if (met & !fired)
        {
            Debug.Log(conditionName + " Is Triggered");
            fired = true;
        }
        else if (!met & fired)
        {
            fired = false;
        }
    }
}
