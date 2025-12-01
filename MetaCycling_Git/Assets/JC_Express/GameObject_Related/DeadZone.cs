using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeadZone : MonoBehaviour
{
    [SerializeField]private CheckForType checkFor= CheckForType.Everyone;
    [SerializeField]private string target = "respawn";
    [SerializeField]private bool toDestroy = false;
    void OnTriggerEnter(Collider other)
    {
        if(checkFor == CheckForType.Everyone | 
                (checkFor == CheckForType.Tag && other.tag == target) | 
                (checkFor == CheckForType.Name && other.name == target))
        {
            if(toDestroy)
                Destroy(other.gameObject);
            else
                other.gameObject.SetActive(false);
        }
    }
}

// is also declared in SimpleTriggerEvent
// [System.Serializable]
// public enum CheckForType
// {
//     Everyone,
//     Name,
//     Tag,
// }
