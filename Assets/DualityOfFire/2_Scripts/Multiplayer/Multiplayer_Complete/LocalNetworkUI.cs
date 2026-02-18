using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;

public class LocalNetworkUI : MonoBehaviour
{
    public static LocalNetworkUI Instance;

    [Header("UI References")]
    public GameObject uiPanel;
    public TMP_InputField joinCodeInput;
    public TMP_InputField nameInput;
    public TMP_Text discoveryStatusText;
    public TMP_Text hostCodeText;
    public GameObject hostButton;
    public GameObject clientButton;
    public GameObject startGameButton;

    [Header("Network Components")]
    public DiscoveryHandler discoveryHandler;

    private UnityTransport transport;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Get Unity Transport component
        if (NetworkManager.Singleton != null)
        {
            transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            NetworkManager.Singleton.OnClientConnectedCallback += OnConnected;
        }
        else
        {
            Debug.LogError("❌ NetworkManager not found in scene!");
        }

        // Start listening for game broadcasts
        if (discoveryHandler != null)
        {
            discoveryHandler.StartListening((ip) =>
            {
                if (joinCodeInput != null)
                    joinCodeInput.text = ip;

                if (discoveryStatusText != null)
                    discoveryStatusText.text = "Game Found!";
            });
        }
    }

    public void StartHost()
    {
        // Validate name input
        if (string.IsNullOrEmpty(nameInput.text))
        {
            if (discoveryStatusText != null)
                discoveryStatusText.text = "<color=red>Please enter your name!</color>";
            return;
        }

        // Get local IP
        string hostIP = LocalIPFinder.GetLocalIPv4();

        // Fallback to hotspot IP if local IP not found
        if (string.IsNullOrEmpty(hostIP) || hostIP == "127.0.0.1")
        {
            hostIP = "192.168.43.1"; // Default mobile hotspot IP
            Debug.LogWarning("Using default hotspot IP: " + hostIP);
        }

        Debug.Log($"Starting host on IP: {hostIP}");

        // Configure transport
        if (transport != null)
        {
            transport.SetConnectionData(hostIP, 7777);
        }

        // Start host
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.StartHost();

            // Start broadcasting
            if (discoveryHandler != null)
                discoveryHandler.StartBroadcasting(hostIP);

            // Display host IP
            if (hostCodeText != null)
                hostCodeText.text = "HOSTING ON:\n" + hostIP;

            UpdateUI(true);
        }
    }

    public void StartClient()
    {
        // Validate inputs
        if (string.IsNullOrEmpty(nameInput.text))
        {
            if (discoveryStatusText != null)
                discoveryStatusText.text = "<color=red>Please enter your name!</color>";
            return;
        }

        if (string.IsNullOrEmpty(joinCodeInput.text))
        {
            if (discoveryStatusText != null)
                discoveryStatusText.text = "<color=red>Please enter host IP!</color>";
            return;
        }

        Debug.Log($"Connecting to: {joinCodeInput.text}");

        // Configure transport
        if (transport != null)
        {
            transport.SetConnectionData(joinCodeInput.text, 7777);
        }

        // Start client
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.StartClient();
            UpdateUI(true);
        }
    }

    public void ShowKickMessage(string msg)
    {
        if (discoveryStatusText != null)
            discoveryStatusText.text = "<color=red>" + msg + "</color>";

        UpdateUI(false);

        // Shutdown network
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }
    }

    private void UpdateUI(bool connected)
    {
        if (hostButton != null)
            hostButton.SetActive(!connected);

        if (clientButton != null)
            clientButton.SetActive(!connected);

        if (nameInput != null)
            nameInput.interactable = !connected;

        if (joinCodeInput != null)
            joinCodeInput.interactable = !connected;

        // Show/hide main UI panel
        if (uiPanel != null && LobbyManager.Instance != null)
        {
            uiPanel.SetActive(!connected || LobbyManager.Instance.uiPanel.activeSelf);
        }
    }

    private void OnConnected(ulong id)
    {
        // When local client connects, add name to lobby
        if (id == NetworkManager.Singleton.LocalClientId)
        {
            if (LobbyManager.Instance != null && !string.IsNullOrEmpty(nameInput.text))
            {
                LobbyManager.Instance.AddPlayerNameServerRpc(nameInput.text);
                Debug.Log($"Connected as: {nameInput.text}");
            }
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnConnected;
        }
    }
}