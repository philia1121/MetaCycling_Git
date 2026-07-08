using Mirror;
using Mirror.Discovery;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AutoConnect : MonoBehaviour
{
    public NetworkDiscovery networkDiscovery;

    public GameObject panelBg;
    public Button hostBtn;
    public Button clientBtn;

    private bool isHost = false;

    private string log = "";
    private Coroutine clientSearchCoroutine;
    private bool serverFound = false;

    void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        foreach (var device in devices)
        {
            if (device.name.ToLower().Contains("webcam")) isHost = true;

            log += $"{device.name} \n";
        }
        hostBtn.onClick.AddListener(() => { isHost = true; panelBg.gameObject.SetActive(false); });
        clientBtn.onClick.AddListener(() => { isHost = false; panelBg.gameObject.SetActive(false); });

        // Delay setup by one frame to guarantee Mirror is completely ready
        StartCoroutine(InitializeNetworkFlow());
    }

    IEnumerator InitializeNetworkFlow()
    {
        yield return null; // Wait 1 frame

        // calc IP address
        string dynamicBroadcast = CalculateLocalBroadcastAddress();

        if (isHost)
        {
            Debug.Log("Acting as Host. Starting server...\n" + log);

            // Start the Mirror Host using your custom instance tracker
            FitnessNetworkManager.instance.StartHost();

            // Mathematically calculate the broadcast IP for the current router/hotspot
            if (!string.IsNullOrEmpty(dynamicBroadcast))
            {
                networkDiscovery.BroadcastAddress = dynamicBroadcast;
                Debug.Log($"Dynamic Subnet Broadcast Address calculated: {dynamicBroadcast}");
            }

            // Begin dynamic broadcasting
            networkDiscovery.AdvertiseServer();
        }
        else
        {
            // Set up the listener first
            networkDiscovery.OnServerFound.AddListener(OnDiscoveredServer);

            if (!string.IsNullOrEmpty(dynamicBroadcast))
            {
                networkDiscovery.BroadcastAddress = dynamicBroadcast;
                Debug.Log($"Dynamic Subnet Broadcast Address calculated (Client): {dynamicBroadcast}");
            }

            // START THE PERSISTENT SEARCH LOOP
            serverFound = false;
            clientSearchCoroutine = StartCoroutine(KeepSearchingForHost(dynamicBroadcast));
        }
    }

    // This background loop keeps restarting discovery if the Host isn't turned on yet
    IEnumerator KeepSearchingForHost(string targetAddress)
    {
        string displayAddress = string.IsNullOrEmpty(targetAddress) ? "255.255.255.255" : targetAddress;
        int attemptCounter = 1;

        while (!serverFound)
        {
            // FALLBACK SYSTEM: If we hit 5 broadcast attempts and the router is blocking us...
            if (attemptCounter >= 2 && !string.IsNullOrEmpty(targetAddress))
            {
                // Turn a broadcast IP like "10.64.241.255" into a guessable range root "10.64.241."
                string ipBase = targetAddress.Substring(0, targetAddress.LastIndexOf('.') + 1);

                Debug.Log($"Broadcast blocked by router!\nAttempting Direct Fallback Scan on Subnet: {ipBase}X");

                networkDiscovery.StopDiscovery();

                // Loop and hammer the typical common local host addresses (usually .2 to .15 on mobile hotspots)
                for (int i = 180; i <= 185; i++)
                {
                    if (serverFound) break;

                    string testIp = ipBase + i;
                    Debug.Log($"Direct connecting to fallback guess:\n{testIp}");

                    System.UriBuilder uriBuilder = new System.UriBuilder
                    {
                        Scheme = "kcp",
                        Host = testIp,
                        Port = 7777 // Matching new safe transport port
                    };

                    // Attempt to reach out directly bypassing discovery
                    FitnessNetworkManager.instance.StartClient(uriBuilder.Uri);

                    // Give it a brief window to establish a connection handshake
                    yield return new WaitForSeconds(1.5f);

                    // If Mirror client successfully connects, NetworkManager changes status internally
                    float timer = 0f;
                    while (timer < 4.0f)
                    {
                        if (NetworkClient.isConnected)
                        {
                            Debug.Log($"Direct Link Established successfully!\n" +
                                $"on:{testIp}");
                            yield break;
                        }
                        timer += Time.deltaTime;
                        yield return null; // Wait exactly 1 frame, checking constantly
                    }

                    // Stop client attempt cleanly before trying next fallback IP address slot
                    FitnessNetworkManager.instance.StopClient();
                    yield return new WaitForSeconds(0.1f);
                }
            }

            // --- Standard Broadcast Discovery Loop ---
            if (!serverFound)
            {
                Debug.Log($"Acting as Client (Attempt #{attemptCounter})\nTargeting: {displayAddress}\nSearching for Host...");

                networkDiscovery.StopDiscovery();
                yield return new WaitForSeconds(0.1f);

                networkDiscovery.StartDiscovery();
                attemptCounter++;

                yield return new WaitForSeconds(4.0f);
            }
        }
    }

    private void OnDestroy()
    {
        if (clientSearchCoroutine != null) StopCoroutine(clientSearchCoroutine);
    }

    // Calculates the mathematically exact broadcast address for whatever network the PC is on
    private string CalculateLocalBroadcastAddress()
    {
        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus == OperationalStatus.Up)
            {
                foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        IPAddress address = ip.Address;

                        if (IPAddress.IsLoopback(address)) continue;

                        IPAddress mask = ip.IPv4Mask;

                        if (mask == null)
                        {
                            int prefixLength = ip.PrefixLength;
                            if (prefixLength <= 0 || prefixLength > 32) prefixLength = 24;

                            uint maskValue = ~(0xFFFFFFFF >> prefixLength);
                            byte[] maskBytes32 = System.BitConverter.GetBytes(maskValue);
                            if (System.BitConverter.IsLittleEndian) System.Array.Reverse(maskBytes32);
                            mask = new IPAddress(maskBytes32);
                        }

                        byte[] addressBytes = address.GetAddressBytes();
                        byte[] maskBytes = mask.GetAddressBytes();

                        if (addressBytes.Length != maskBytes.Length) continue;

                        byte[] broadcastBytes = new byte[addressBytes.Length];
                        for (int i = 0; i < broadcastBytes.Length; i++)
                        {
                            broadcastBytes[i] = (byte)(addressBytes[i] | ~maskBytes[i]);
                        }

                        string calculatedBroadcast = new IPAddress(broadcastBytes).ToString();

                        if (calculatedBroadcast != "255.255.255.255" && calculatedBroadcast != "0.0.0.0")
                        {
                            return calculatedBroadcast;
                        }
                    }
                }
            }
        }
        return null;
    }

    // This runs on the VR headset when it "hears" the laptop broadcasting
    public void OnDiscoveredServer(ServerResponse info)
    {
        // Set flag to break the coroutine search loop immediately
        serverFound = true;
        if (clientSearchCoroutine != null) StopCoroutine(clientSearchCoroutine);

        string hostIp = info.uri.Host;
        Debug.Log($"Host found! IP Address is: {hostIp}\nConnecting now...");

        // Stop looking and connect to the discovered IP
        networkDiscovery.StopDiscovery();
        FitnessNetworkManager.instance.StartClient(info.uri);
    }
}