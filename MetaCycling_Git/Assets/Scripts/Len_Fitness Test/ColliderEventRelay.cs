using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ColliderEventRelay : MonoBehaviour
{
    public event Action<GameObject> OnObjHit;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("FitnessTrackedObj"))
        {
            OnObjHit?.Invoke(this.gameObject);
        }
    }
}
