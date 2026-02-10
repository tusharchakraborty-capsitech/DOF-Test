using UnityEngine;
using Unity.Netcode;
using TMPro;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.UI;

/// <summary>
/// MULTIPLAYER - Lobby Manager
/// Manages player list, lobby UI, and game start
/// Path: Assets/Scripts/Multiplayer/Lobby/LobbyManager.cs
/// 
/// SETUP:
/// 1. Create GameObject in scene → Add this script
/// 2. Create UI:
///    - Panel for lobby
///    - Player list container (Vertical Layout Group)
///    - Player entry prefab (Name text + Kick button)
///    - Start Game button
/// 3. Assign all UI elements in Inspector
/// 4. Make sure NetworkManager exists in scene
/// </summary>
public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance;

    [Header("Settings")]
    public GameObject playerPrefab;
    public GameObject uiPanel;
    public Button startGameButton;

    [Header("UI List Settings")]
    public Transform playerListContainer; // Container with Vertical Layout Group 
    public GameObject playerEntryPrefab;  // Prefab containing Name Text and Kick Button

    private NetworkList<FixedString64Bytes> playerNames;
    private Dictionary<ulong, string> clientNamesMap = new Dictionary<ulong, string>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Initialize NetworkList
        playerNames = new NetworkList<FixedString64Bytes>();
    }

    public override void OnNetworkSpawn()
    {
        playerNames.OnListChanged += (e) => UpdateStatusUI();

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleDisconnect;
        }

        UpdateStatusUI();
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleDisconnect;
        }

        base.OnNetworkDespawn();
    }

    private void HandleDisconnect(ulong clientId)
    {
        if (clientNamesMap.ContainsKey(clientId))
        {
            playerNames.Remove(clientNamesMap[clientId]);
            clientNamesMap.Remove(clientId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddPlayerNameServerRpc(string name, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (!clientNamesMap.ContainsKey(clientId))
        {
            clientNamesMap.Add(clientId, name);
            playerNames.Add(name);
            Debug.Log($"✅ Player '{name}' added to lobby (ClientID: {clientId})");
        }
    }

    private void UpdateStatusUI()
    {
        // Clear existing list
        foreach (Transform child in playerListContainer)
            Destroy(child.gameObject);

        // Rebuild player list
        for (int i = 0; i < playerNames.Count; i++)
        {
            GameObject entry = Instantiate(playerEntryPrefab, playerListContainer);
            string pName = playerNames[i].ToString();

            // Set player name text
            var nameText = entry.GetComponentInChildren<TMP_Text>();
            if (nameText != null)
                nameText.text = pName;

            // Configure kick button
            Button kickBtn = entry.GetComponentInChildren<Button>();
            if (kickBtn != null)
            {
                if (IsServer)
                {
                    ulong targetId = GetIdFromName(pName);

                    // Hide kick button for host's own entry
                    if (targetId == NetworkManager.Singleton.LocalClientId)
                    {
                        kickBtn.gameObject.SetActive(false);
                    }
                    else
                    {
                        kickBtn.onClick.AddListener(() => KickPlayer(targetId));
                    }
                }
                else
                {
                    // Clients can't kick anyone
                    kickBtn.gameObject.SetActive(false);
                }
            }
        }

        // Enable start button only if enough players
        if (IsServer && startGameButton != null)
        {
            startGameButton.interactable = playerNames.Count >= 2;
        }
    }

    private ulong GetIdFromName(string name)
    {
        foreach (var pair in clientNamesMap)
        {
            if (pair.Value == name)
                return pair.Key;
        }
        return 999; // Invalid ID
    }

    public void KickPlayer(ulong clientId)
    {
        if (!IsServer) return;

        Debug.Log($"🚫 Kicking client {clientId}");

        // Notify the kicked client
        NotifyKickedClientRpc(new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        });

        // Disconnect after short delay
        StartCoroutine(DisconnectDelay(clientId));
    }

    [ClientRpc]
    private void NotifyKickedClientRpc(ClientRpcParams rpcParams = default)
    {
        if (LocalNetworkUI.Instance != null)
        {
            LocalNetworkUI.Instance.ShowKickMessage("You have been kicked from the lobby!");
        }
    }

    private System.Collections.IEnumerator DisconnectDelay(ulong clientId)
    {
        yield return new WaitForSeconds(0.5f);

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.DisconnectClient(clientId);
        }
    }

    public void StartGame()
    {
        if (!IsServer) return;

        Debug.Log("🎮 Starting game...");

        uiPanel.SetActive(false);
        HideUIClientRpc();

        // Spawn player for each connected client
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            GameObject playerInstance = Instantiate(playerPrefab);
            playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);

            // Set player name if available
            if (clientNamesMap.TryGetValue(clientId, out string name))
            {
                var nameDisplay = playerInstance.GetComponent<PlayerNameDisplay>();
                if (nameDisplay != null)
                {
                    nameDisplay.SetPlayerName(name);
                }
            }

            Debug.Log($"✅ Spawned player for client {clientId}");
        }
    }

    [ClientRpc]
    private void HideUIClientRpc()
    {
        if (uiPanel != null)
            uiPanel.SetActive(false);
    }
}