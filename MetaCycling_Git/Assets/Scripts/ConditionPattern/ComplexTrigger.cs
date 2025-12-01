using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class ComplexTrigger : MonoBehaviour
{
    public GameObject petrollerObject;
    public Transform playerReference;

    public List<TriggerRule> rules;

    void Update()
    {
        foreach (var rule in rules)
        {
            if (!rule.Ignore) rule.Tick(petrollerObject, playerReference, Time.deltaTime);
        }
    }
}
