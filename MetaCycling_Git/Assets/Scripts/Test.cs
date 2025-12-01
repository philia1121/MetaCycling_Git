using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    public Transform A, B;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        this.transform.position = Vector3.Lerp(A.position, B.position, 0.5f);
        this.transform.rotation = Quaternion.Slerp(A.rotation, B.rotation, 0.5f);
    }
}
