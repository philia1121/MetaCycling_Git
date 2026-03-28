using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    public Rigidbody rg;
    public Vector3 velocity;
    void Update()
    {
        velocity = rg.velocity;
    }
}
