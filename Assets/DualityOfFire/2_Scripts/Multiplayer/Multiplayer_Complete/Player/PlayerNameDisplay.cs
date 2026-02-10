using Unity.Netcode;
using TMPro;
using Unity.Collections;
using UnityEngine;

/// <summary>
/// MULTIPLAYER - Player Name Display (Billboard)
/// Shows player name above their character
/// Path: Assets/Scripts/Multiplayer/Player/PlayerNameDisplay.cs
/// 
/// SETUP:
/// 1. Attach to Player GameObject (same object as NetworkPlayerGun)
/// 2. Create UI Text (TextMeshPro) as child of player
/// 3. Position text above player sprite
/// 4. Assign text to nameTagText field in Inspector
/// 5. Text will always face camera (billboard effect)
/// </summary>
public class PlayerNameDisplay : NetworkBehaviour
{
    public TMP_Text nameTagText;

    private NetworkVariable<FixedString64Bytes> playerName = new NetworkVariable<FixedString64Bytes>(
        "",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        playerName.OnValueChanged += (oldV, newV) => nameTagText.text = newV.ToString();

        if (nameTagText != null)
            nameTagText.text = playerName.Value.ToString();
    }

    public void SetPlayerName(string name)
    {
        if (IsServer)
            playerName.Value = name;
    }

    private void LateUpdate()
    {
        // Billboard effect - always face camera
        if (Camera.main != null && nameTagText != null)
        {
            transform.LookAt(transform.position + Camera.main.transform.forward);
        }
    }
}