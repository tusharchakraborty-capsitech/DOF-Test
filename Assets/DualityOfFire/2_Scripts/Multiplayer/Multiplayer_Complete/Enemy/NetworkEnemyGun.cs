using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

/// <summary>
/// MULTIPLAYER - Enemy Gun Controller
/// Server controls AI, Player 2 controls manually
/// Path: Assets/Scripts/Multiplayer/Enemy/NetworkEnemyGun.cs
/// 
/// MODES:
/// - Player 1 Mode: AI controlled (server only)
/// - Player 2 Mode: Player controlled (owner input)
/// 
/// SETUP:
/// 1. Attach to Enemy GameObject
/// 2. Add NetworkObject component
/// 3. Add NetworkTransform component
/// 4. Assign all fields in Inspector
/// 5. Add to NetworkManager Prefabs List (if spawning dynamically)
/// </summary>
public class NetworkEnemyGun : NetworkGunController
{
    [Header("Fire Settings")]
    [SerializeField] private float fireCooldown = 0.25f;
    [SerializeField] private float minFireTime = 0.5f;
    [SerializeField] private float maxFireTime = 2f;

    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private Image enemyHealthImg;
    [SerializeField] private PlayerDataScriptableObject playerDataScriptableObject;

    // Network variable for syncing health
    private NetworkVariable<int> currentHealth = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private float nextFireTime;
    private bool canUpdate;
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

        // Only server controls AI behavior
        if (!IsServer) return;

        if (playerDataScriptableObject != null && playerDataScriptableObject.PlayerChoice == 1)
        {
            // AI mode
            canUpdate = false;
            ScheduleNextShot();
            Debug.Log("✅ Enemy: AI mode activated");
        }
        else
        {
            // Player 2 control mode
            var rb2d = GetComponent<Rigidbody2D>();
            if (rb2d != null)
                rb2d.gravityScale = -rb2d.gravityScale;
            canUpdate = true;
            Debug.Log("✅ Enemy: Player 2 mode activated");
        }
    }

    private void Update()
    {
        // Player 2 controls enemy gun
        if (playerDataScriptableObject != null && playerDataScriptableObject.PlayerChoice == 2)
        {
            if (!IsOwner) return;
        }
        else
        {
            // AI mode - only server controls
            if (!IsServer) return;
        }

        if (isDead || !canUpdate || Time.time < nextFireTime)
            return;

        // Editor testing
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(1)) // Right click
        {
            Shoot(-2, "Player", "Player");
            nextFireTime = Time.time + fireCooldown;
            return;
        }
#endif

        if (HasValidTouch(touch => touch.y > Screen.height / 2f))
        {
            Shoot(-2, "Player", "Player");
            nextFireTime = Time.time + fireCooldown;
        }
    }

    // AI Fire - only on server
    private void ScheduleNextShot()
    {
        if (!IsServer) return;
        float delay = Random.Range(minFireTime, maxFireTime);
        Invoke(nameof(AIFire), delay);
    }

    private void AIFire()
    {
        if (!IsServer) return;
        if (isDead) return;

        if (Time.time < nextFireTime)
        {
            ScheduleNextShot();
            return;
        }

        Shoot(-2, "Player", "Player");
        nextFireTime = Time.time + fireCooldown;
        ScheduleNextShot();
    }

    // Server-authoritative damage
    public void TakeDamage(int damage)
    {
        if (!IsServer) return;
        if (isDead) return;

        currentHealth.Value = Mathf.Clamp(currentHealth.Value - damage, 0, maxHealth);
    }

    private void UpdateHealthBar()
    {
        if (enemyHealthImg != null)
            enemyHealthImg.fillAmount = (float)currentHealth.Value / maxHealth;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (IsServer)
        {
            CancelInvoke();
        }

        // Play death effects on all clients
        PlayDeathEffectsClientRpc();

        // Show win screen
        if (IsServer)
        {
            if (UIManager.Instance != null)
                UIManager.Instance.ShowWinWithDelay();
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