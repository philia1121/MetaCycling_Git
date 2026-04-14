using Mirror;
using UnityEngine;

public class Webcam : NetworkBehaviour
{
    [SerializeField] private Material mat;

    void Start()
    {
        if (isServer)
        {
            SetupWebcam();
        }
        else
        {
            // Optional: You could put a "Waiting for Server Feed" 
            // texture here for the VR users
            Debug.Log("Client joined: Waiting for server to broadcast webcam object.");
        }
    }

    [Server] // Logic inside this only runs on the Laptop
    void SetupWebcam()
    {
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length > 0)
        {
            WebCamTexture texture = new(devices[0].name, 1920, 1080, 30);

            mat.mainTexture = texture;

            texture.Play();
        }
    }
}
