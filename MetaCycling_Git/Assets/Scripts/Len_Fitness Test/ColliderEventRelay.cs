using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ColliderEventRelay : MonoBehaviour
{
    public event Action<GameObject, bool> OnObjHit;
    public bool isDestroyable;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("FitnessTrackedObj"))
        {
            Debug.Log("hit");
            OnObjHit?.Invoke(this.gameObject, isDestroyable);
        }
    }
}
