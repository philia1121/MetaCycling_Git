using Mirror;
using Mirror.Discovery;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class AutoConnect : MonoBehaviour
{
    public NetworkDiscovery networkDiscovery;
    public TMP_Text etcText;
    private bool isHost = false;

    void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        foreach (var device in devices)
        {
            if(device.name.Contains("WebCam")) isHost= true;
        }

        //if (WebCamTexture.devices.Length > 0)
        //{
        //    isHost = true;
        //    Debug.Log("Webcam detected. Acting as Host.");
        //}
        //else
        //{
        //    isHost = false;
        //    Debug.Log("No webcam detected. Acting as VR Client.");
        //}

        // 2. Execute Logic
        if (isHost)
        {
            // Start the server and start broadcasting 
            NetworkManager.singleton.StartHost();
            networkDiscovery.AdvertiseServer();
        }
        else
        {
            // Start listening for a host on the hotspot network
            networkDiscovery.OnServerFound.AddListener(OnDiscoveredServer);
            networkDiscovery.StartDiscovery();
        }
        Debug.Log($"found IP: "+networkDiscovery.BroadcastAddress);
    }

    // This runs on the VR headset when it "hears" the laptop broadcasting
    public void OnDiscoveredServer(ServerResponse info)
    {
        string hostIp = info.uri.Host;

        etcText.text = ($"Host found! IP Address is: {hostIp}");

        // Stop looking and connect to the discovered IP
        networkDiscovery.StopDiscovery();
        NetworkManager.singleton.StartClient(info.uri);
    }
}
