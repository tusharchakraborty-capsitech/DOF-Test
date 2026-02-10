using UnityEngine;
using Unity.Netcode;

/// <summary>
/// MULTIPLAYER - Network Health Controller (Singleton)
/// Manages damage calculation for multiplayer
/// Path: Assets/Scripts/Multiplayer/Managers/NetworkHealthController.cs
/// 
/// NOTE: NetworkBehaviours cannot use DontDestroyOnLoad
/// 
/// SETUP:
/// 1. Create GameObject in scene
/// 2. Attach this script
/// 3. Set damage value in Inspector
/// 4. Optional - you might not need this if using NetworkVariables for health
/// 
/// ALTERNATIVE:
/// Instead of using this singleton, you can directly handle damage
/// in NetworkPlayerGun and NetworkEnemyGun scripts using NetworkVariables
/// </summary>
public class NetworkHealthController : NetworkBehaviour
{
    public static NetworkHealthController Instance;

    [SerializeField] private int damage = 20;

    private void Awake()
    {
        // NetworkBehaviour objects can't use DontDestroyOnLoad
        // Only set Instance
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Apply damage to current health
    /// </summary>
    /// <param name="currentHealth">Current health value</param>
    /// <returns>New health value after damage</returns>
    public int ApplyDamage(int currentHealth)
    {
        Debug.Log($"💥 Taking damage: {damage}");
        return Mathf.Max(0, currentHealth - damage);
    }

    /// <summary>
    /// Get the damage value
    /// </summary>
    public int GetDamage()
    {
        return damage;
    }

    /// <summary>
    /// Set the damage value (server only)
    /// </summary>
    public void SetDamage(int newDamage)
    {
        if (IsServer)
        {
            damage = newDamage;
            Debug.Log($"⚙️ Damage set to: {damage}");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}