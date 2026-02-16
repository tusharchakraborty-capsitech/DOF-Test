using UnityEngine;
using Unity.Netcode;
using TMPro;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// UNIVERSAL LOBBY MANAGER
/// Works with the universal player controller.
/// 
/// ✅ Host spawns at (1.5, -2) facing LEFT
/// ✅ Client spawns at (-1.5, -2) facing RIGHT
/// ✅ Players look at each other, bullets fly correctly.
/// </summary>
public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance;

    [Header("Settings")]
    public GameObject playerPrefab;
    public GameObject uiPanel;
    public Button startGameButton;

    [Header("UI List Settings")]
    public Transform playerListContainer;
    public GameObject playerEntryPrefab;

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
            Debug.Log($"✅ Player '{name}' joined (ID: {clientId})");
        }
    }

    private void UpdateStatusUI()
    {
        foreach (Transform child in playerListContainer)
            Destroy(child.gameObject);

        for (int i = 0; i < playerNames.Count; i++)
        {
            GameObject entry = Instantiate(playerEntryPrefab, playerListContainer);
            string pName = playerNames[i].ToString();

            TMP_Text nameText = entry.GetComponentInChildren<TMP_Text>();
            if (nameText != null)
                nameText.text = pName;

            Button kickBtn = entry.GetComponentInChildren<Button>();
            if (kickBtn != null)
            {
                if (IsServer)
                {
                    ulong targetId = GetIdFromName(pName);
                    if (targetId == NetworkManager.Singleton.LocalClientId)
                        kickBtn.gameObject.SetActive(false);
                    else
                        kickBtn.onClick.AddListener(() => KickPlayer(targetId));
                }
                else
                {
                    kickBtn.gameObject.SetActive(false);
                }
            }
        }

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
        return ulong.MaxValue;
    }

    public void KickPlayer(ulong clientId)
    {
        if (!IsServer) return;

        Debug.Log($"🚫 Kicking client {clientId}");

        NotifyKickedClientRpc(new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        });

        StartCoroutine(DisconnectDelay(clientId));
    }

    [ClientRpc]
    private void NotifyKickedClientRpc(ClientRpcParams rpcParams = default)
    {
        if (LocalNetworkUI.Instance != null)
            LocalNetworkUI.Instance.ShowKickMessage("You have been kicked!");
    }

    private IEnumerator DisconnectDelay(ulong clientId)
    {
        yield return new WaitForSeconds(0.5f);
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.DisconnectClient(clientId);
        }
    }

    //public void StartGame()
    //{
    //    if (!IsServer) return;

    //    Debug.Log("🎮 Starting game...");

    //    uiPanel.SetActive(false);
    //    HideUIClientRpc();

    //    List<ulong> clients = new List<ulong>(NetworkManager.Singleton.ConnectedClientsIds);

    //    for (int i = 0; i < clients.Count; i++)
    //    {
    //        ulong clientId = clients[i];
    //        GameObject player = Instantiate(playerPrefab);

    //        // ---------- POSITION ----------
    //        if (i == 0) // Host → RIGHT (1.5, -2)
    //        {
    //            player.transform.position = new Vector3(1f, -2f, 0f);
    //            Debug.Log($"👑 HOST spawning at RIGHT (1.5, -2)");
    //        }
    //        else // Client → LEFT (-1.5, -2)
    //        {
    //            player.transform.position = new Vector3(-1f, -2f, 0f);
    //            Debug.Log($"👤 CLIENT spawning at LEFT (-1.5, -2)");
    //        }

    //        // ---------- FACING (flip sprite) ----------
    //        Vector3 localScale = player.transform.localScale;
    //        if (i == 0) // Host faces LEFT (toward client)
    //            localScale.x = -Mathf.Abs(localScale.x);
    //        else        // Client faces RIGHT (toward host)
    //            localScale.x = Mathf.Abs(localScale.x);
    //        player.transform.localScale = localScale;

    //        // ---------- SPAWN ----------
    //        NetworkObject netObj = player.GetComponent<NetworkObject>();
    //        netObj.SpawnAsPlayerObject(clientId);

    //        Debug.Log($"✅ Player {i} (ClientId: {clientId}) spawned at {player.transform.position} facing {(i == 0 ? "LEFT" : "RIGHT")}");
    //    }
    //}

    public void StartGame()
    {
        if (!IsServer) return;
        Debug.Log("🎮 Starting game...");
        uiPanel.SetActive(false);
        HideUIClientRpc();

        List<ulong> clients = new List<ulong>(NetworkManager.Singleton.ConnectedClientsIds);

        for (int i = 0; i < clients.Count; i++)
        {
            ulong clientId = clients[i];
            GameObject player = Instantiate(playerPrefab);

            // ---------- POSITION ----------
            if (i == 0) // Host → RIGHT (1, -2)
            {
                player.transform.position = new Vector3(1f, -2f, 0f);
                Debug.Log($"👑 HOST spawning at RIGHT (1, -2)");
            }
            else // Client → LEFT (-1, -2)
            {
                player.transform.position = new Vector3(-1f, -2f, 0f);
                Debug.Log($"👤 CLIENT spawning at LEFT (-1, -2)");
            }

            // ---------- ROTATION (Y-axis flip) ----------
            if (i == 0) // Host faces LEFT (toward client)
                player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            else        // Client faces RIGHT (toward host)
                player.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

            // ---------- SPAWN ----------
            NetworkObject netObj = player.GetComponent<NetworkObject>();
            netObj.SpawnAsPlayerObject(clientId);

            Debug.Log($"✅ Player {i} (ClientId: {clientId}) spawned at {player.transform.position} facing {(i == 0 ? "LEFT" : "RIGHT")}");
        }
    }

    [ClientRpc]
    private void HideUIClientRpc()
    {
        if (uiPanel != null)
            uiPanel.SetActive(false);
    }
}