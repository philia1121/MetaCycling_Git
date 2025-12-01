using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelAlignment : MonoBehaviour
{
    [SerializeField]private Transform target;
    [SerializeField]private Transform follower;
    [SerializeField]private bool alignPosition = true;
    [SerializeField]private bool alignRotation = true;
    void Start()
    {
        if(!follower)
            follower = this.transform;
    }
    
    void Update()
    {
        if(alignPosition)
            follower.transform.position = target.position;
        if(alignRotation)
            follower.transform.rotation = target.rotation;
    }
}
