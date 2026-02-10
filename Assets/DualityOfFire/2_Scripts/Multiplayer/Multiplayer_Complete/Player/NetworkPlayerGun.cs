using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

/// <summary>
/// MULTIPLAYER - Player Gun Controller
/// Owner controls input, server validates and syncs
/// Path: Assets/Scripts/Multiplayer/Player/NetworkPlayerGun.cs
/// 
/// SETUP:
/// 1. Attach to Player GameObject
/// 2. Add NetworkObject component
/// 3. Add NetworkTransform component (for movement sync)
/// 4. Assign all fields in Inspector
/// 5. Add to NetworkManager Prefabs List
/// </summary>
public class NetworkPlayerGun : NetworkGunController
{
    [Header("Fire Settings")]
    [SerializeField] private float fireCooldown = 0.25f;
    private float nextFireTime;

    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private Image playerHealthImg;
    [SerializeField] private PlayerDataScriptableObject playerDataScriptableObject;

    // Network variable for syncing health
    private NetworkVariable<int> currentHealth = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private bool isDead = false;

    protected override void Awake()
    {
        base.Awake();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Initialize health on server
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }

        // Subscribe to health changes
        currentHealth.OnValueChanged += OnHealthChanged;

        // Update UI immediately
        UpdateHealthBar();
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= OnHealthChanged;
        base.OnNetworkDespawn();
    }

    private void OnHealthChanged(int oldHealth, int newHealth)
    {
        UpdateHealthBar();

        if (newHealth <= 0 && !isDead)
        {
            Die();
        }
    }

    private void Start()
    {
        UpdateHealthBar();
    }

    void Update()
    {
        // Only process input for owned player
        if (!IsOwner) return;
        if (isDead) return;

        PlayerInput();
    }

    void PlayerInput()
    {
        if (Time.time < nextFireTime) return;

        bool shouldShoot = false;

        // Editor testing with mouse
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            shouldShoot = true;
        }
#endif

        if (playerDataScriptableObject != null)
        {
            if (playerDataScriptableObject.PlayerChoice == 1)
            {
                // Player 1 mode - shoot anywhere
                if (HasValidTouch(pos => true))
                {
                    shouldShoot = true;
                }
            }
            else if (playerDataScriptableObject.PlayerChoice == 2)
            {
                // Player 2 mode - shoot on lower half
                if (HasValidTouch(pos => pos.y < Screen.height / 2f))
                {
                    shouldShoot = true;
                }
            }
        }

        if (shouldShoot)
        {
            base.Shoot(1, "Enemy", "AIGun");
            nextFireTime = Time.time + fireCooldown;
        }
    }

    // Server-authoritative damage
    public void TakeDamage(int damage)
    {
        if (!IsServer) return;
        if (isDead) return;

        currentHealth.Value = Mathf.Clamp(currentHealth.Value - damage, 0, maxHealth);
    }

    void UpdateHealthBar()
    {
        if (playerHealthImg != null)
        {
            playerHealthImg.fillAmount = (float)currentHealth.Value / maxHealth;
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        // Play death effects on all clients
        PlayDeathEffectsClientRpc();

        // Show game over only for the owner
        if (IsOwner)
        {
            if (UIManager.Instance != null)
                UIManager.Instance.ShowGameOverWithDelay();
        }
    }

    [ClientRpc]
    private void PlayDeathEffectsClientRpc()
    {
        var spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;

        if (destroyParticle != null)
            destroyParticle.Play();

        if (IsServer)
        {
            Destroy(gameObject, 1f);
        }
    }
}