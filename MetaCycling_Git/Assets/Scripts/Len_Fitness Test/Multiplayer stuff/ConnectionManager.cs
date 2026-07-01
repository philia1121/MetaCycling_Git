using kcp2k;
using Mirror;
using System.Collections;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConnectionManager : MonoBehaviour
{
    public TMP_Text etcText;
    public Button forceBtn;
    private bool isHost = false;
    private string log = "";

    // 1. SET YOUR STATIC IP HERE FOR THE CLIENT
    // Change this to match whatever your host PC prints on its screen!
    private const string STATIC_SERVER_IP = "10.64.248.181";

    void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        foreach (var device in devices)
        {
            if (device.name.ToLower().Contains("webcam")) isHost = true;
            log += $"{device.name} \n";
        }

        StartCoroutine(InitializeNetworkFlow());
    }

    IEnumerator InitializeNetworkFlow()
    {
        yield return null; // Wait 1 frame to ensure Mirror is fully awake

        if (isHost)
        {
            // Find the machine's true local network IP address
            string serverIP = GetLocalIPAddress();

            etcText.text = $"[HOST MODE]\n" +
                           $"Server IP to use: {serverIP}\n" +
                           $"Port: {FitnessNetworkManager.instance.GetComponent<KcpTransport>().Port}\n" +
                           $"Starting host server now...\n\n" + log;

            // Start the server
            FitnessNetworkManager.instance.StartHost();
        }
        else
        {
            etcText.text = $"[CLIENT MODE]\n" +
                           $"Targeting Static IP: {STATIC_SERVER_IP}\n" +
                           $"Press 'Force Connect' to establish link.";

            // Set up the force button click listener
            forceBtn.onClick.RemoveAllListeners();
            forceBtn.onClick.AddListener(() => {
                etcText.text = $"Attempting hardcoded connection to:\n{STATIC_SERVER_IP}...";

                // Configure Mirror to point exactly at your target IP address
                FitnessNetworkManager.instance.networkAddress = STATIC_SERVER_IP;
                FitnessNetworkManager.instance.StartClient();

                StartCoroutine(MonitorClientConnection());
            });
        }
    }

    // Lightweight verification loop to track if the handshake succeeds
    IEnumerator MonitorClientConnection()
    {
        float timeoutTimer = 0f;
        while (timeoutTimer < 5.0f)
        {
            if (NetworkClient.isConnected)
            {
                etcText.text = $"SUCCESS!\nConnected directly to Host at:\n{STATIC_SERVER_IP}";
                yield break;
            }
            timeoutTimer += Time.deltaTime;
            yield return null;
        }

        etcText.text = $"CONNECTION TIMEOUT!\n" +
                       $"Failed to reach {STATIC_SERVER_IP}.\n" +
                       $"1. Ensure your PC firewall allows Port 7777.\n" +
                       $"2. Double check if the PC IP changed.";
    }

    // Helper method to dig out the actual local IP address interface

    private string GetLocalIPAddress()
    {
        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            // Safety filter: Ensure the adapter is actively running
            if (ni.OperationalStatus == OperationalStatus.Up)
            {
                // CRITICAL FIX: Skip virtual interfaces (WSL, Hyper-V, VirtualBox, vEthernet)
                string name = ni.Name.ToLower();
                string desc = ni.Description.ToLower();
                if (name.Contains("virtual") || name.Contains("wsl") || name.Contains("vbox") || name.Contains("ethernet") ||
                    desc.Contains("virtual") || desc.Contains("hyper-v") || desc.Contains("subsystem"))
                {
                    continue; // Skip this virtual interface and look for the real Wi-Fi card!
                }

                // Only grab Wireless (Wi-Fi) interface profiles
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                    ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            return ip.Address.ToString();
                        }
                    }
                }
            }
        }
        return "IP NOT FOUND (Ensure PC is connected to the Hotspot)";
    }
    //private string GetLocalIPAddress()
    //{
    //    foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
    //    {
    //        if (ni.OperationalStatus == OperationalStatus.Up &&
    //            ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
    //        {
    //            foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
    //            {
    //                if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
    //                {
    //                    return ip.Address.ToString();
    //                }
    //            }
    //        }
    //    }
    //    return "IP NOT FOUND (Check Wi-Fi connection)";
    //}
}
