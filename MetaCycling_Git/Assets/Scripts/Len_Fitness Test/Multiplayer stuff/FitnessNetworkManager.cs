using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FitnessNetworkManager : NetworkManager
{
    //[SerializeField] private GameObject playerFab;
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);

        //Debug.Log($"{conn.connectionId}");

        //GameObject _unitSpwn = Instantiate(playerFab, conn.identity.transform.position, conn.identity.transform.rotation);
        //NetworkServer.Spawn(_unitSpwn, conn);

        //GameObject _mng = conn.identity.gameObject;
    }
    public override void OnClientConnect()
    {
        base.OnClientConnect();
    }


    public override void OnStartServer()
    {
        GameObject[] g = GameObject.FindGameObjectsWithTag("Player");
        if (g.Length > 0)
            g[0].SetActive(false);
        base.OnStartServer();

        //ServerChangeScene("Game Scene");
    }
}
