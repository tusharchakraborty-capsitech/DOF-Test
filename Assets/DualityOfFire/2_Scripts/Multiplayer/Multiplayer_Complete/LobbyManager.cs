using UnityEngine;
using Unity.Netcode;
using TMPro;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.UI;
using System.Collections;

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
            Debug.Log($"Player '{name}' joined (ID: {clientId})");
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

        Debug.Log($" Kicking client {clientId}");

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

    public void StartGame()
    {
        if (!IsServer) return;

        uiPanel.SetActive(false);
        HideUIClientRpc();

        List<ulong> clients = new List<ulong>(NetworkManager.Singleton.ConnectedClientsIds);

        for (int i = 0; i < clients.Count; i++)
        {
            ulong clientId = clients[i];
            GameObject player = Instantiate(playerPrefab);

            Vector3 spawnPos;
            Quaternion spawnRot = Quaternion.identity;
            Vector3 localScale = player.transform.localScale;

            if (i == 0)
            {
                spawnPos = new Vector3(-3f, -2f, 0f);
                spawnRot = Quaternion.identity; 
                localScale.x = Mathf.Abs(localScale.x); 
            }
            else
            {
                spawnPos = new Vector3(3f, -2f, 0f);
                spawnRot = Quaternion.identity;
                localScale.x = -Mathf.Abs(localScale.x); 
            }

            player.transform.position = spawnPos;
            player.transform.rotation = spawnRot;
            player.transform.localScale = localScale;

            // Spawn with ownership
            NetworkObject netObj = player.GetComponent<NetworkObject>();
            netObj.SpawnAsPlayerObject(clientId);
        }
    }

    [ClientRpc]
    private void HideUIClientRpc()
    {
        if (uiPanel != null)
            uiPanel.SetActive(false);
    }
}