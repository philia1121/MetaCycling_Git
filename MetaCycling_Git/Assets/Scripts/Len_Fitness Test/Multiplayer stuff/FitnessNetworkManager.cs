using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class FitnessNetworkManager : NetworkManager
{
    public static FitnessNetworkManager instance;

    [Space]
    [Header("Server Side Player Manager")]
    // public GameObject serverXROrigin;
    //public ServerPlayerMgr serverPlayerMgr;

    [Header("Client Side Player Root")]
    public GameObject clientXROrigin;

    private bool isHost = false;
    private bool isMap2ClientXR = false;

    private InputDevice _hmd;
    private bool hmdValid = false;
    private bool preHmdValid = false;

    public override void Awake()
    {
        if(instance == null)
            instance = this;
    }


    //[SerializeField] private GameObject playerFab;
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        if (conn == NetworkServer.localConnection)
        {
            Debug.Log("Host loopback client connected. Skipping player prefab spawning.");
            NetworkServer.SetClientReady(conn);
            return;
        }

        Debug.Log($"Remote client connected (ID: {conn.connectionId}). Spawning player prefab...");

        Transform startPos = GetStartPosition();
        GameObject player = startPos != null
            ? Instantiate(playerPrefab, startPos.position, startPos.rotation)
            : Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);

        //Associate the GameObject with the client connection and spawn it across the network
        NetworkServer.AddPlayerForConnection(conn, player);
    }
    public override void OnClientConnect()
    {
        base.OnClientConnect();
        Debug.Log("Client successfully handled network level handshake!");
    }


    public override void OnStartServer()
    {
        base.OnStartServer();
    }
}
