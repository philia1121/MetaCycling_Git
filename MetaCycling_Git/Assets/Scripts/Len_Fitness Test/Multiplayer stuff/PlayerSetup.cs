using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
public class PlayerSetup : NetworkBehaviour
{
    [SerializeField] private GameObject camRig;
    [SerializeField] private GameObject passthrough;

    private void Start()
    {
        if (!isLocalPlayer)
        {
            camRig.SetActive(false);
            passthrough.SetActive(false);
        }
    }

    public override void OnStartLocalPlayer()
    {
        if (isServer && isClient)
        {
            gameObject.SetActive(false);
        }
    }
}
