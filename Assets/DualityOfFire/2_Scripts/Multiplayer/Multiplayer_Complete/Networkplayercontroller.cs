using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections;

public class NetworkPlayerController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Transform gunPivot;
    [SerializeField] private Transform forcePoint;

    [Header("Fire Settings")]
    [SerializeField] private float fireCooldown = 0.25f;
    private float nextFireTime;

    [Header("Recoil Forces (Increased for Mobile)")]
    // ফোর্সের মান বাড়িয়ে দেওয়া হয়েছে যাতে মোবাইলে এফেক্ট বোঝা যায়
    [SerializeField] private float recoilForce = 20f;
    [SerializeField] private float jumpForce = 15f;
    [SerializeField] private float torqueForce = 10f; // Spin force

    [Header("Gun Spin Recoil (Visual Only)")]
    [SerializeField] private float maxGunRotation = 30f;
    [SerializeField] private float gunReturnSpeed = 10f;

    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private Image healthBar;

    [Header("Particle Systems")]
    [SerializeField] private ParticleSystem shootParticle;
    [SerializeField] private ParticleSystem destroyParticle;

    [Header("Shoot Sound Effect")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] audioShootClips;

    // Custom Sync for Rotation (রোটেশন ম্যানুয়ালি সিঙ্ক করার ভেরিয়েবল)
    private NetworkVariable<float> netRotationZ = new NetworkVariable<float>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    private NetworkVariable<int> currentHealth = new NetworkVariable<int>(
        100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    private Rigidbody2D rb;
    private bool isDead = false;
    private int shootDirection = 1;

    // Gun spin state
    private float currentGunRotation = 0f;
    private Quaternion originalGunRotation;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (gunPivot != null) originalGunRotation = gunPivot.localRotation;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer) currentHealth.Value = maxHealth;
        currentHealth.OnValueChanged += OnHealthChanged;
        StartCoroutine(SetShootDirectionAfterSpawn());
        UpdateHealthBar();
    }

    private IEnumerator SetShootDirectionAfterSpawn()
    {
        yield return null;
        shootDirection = transform.position.x < 0 ? 1 : -1;
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= OnHealthChanged;
        base.OnNetworkDespawn();
    }

    private void Start()
    {
        UpdateHealthBar();
    }

    void Update()
    {
        // 1. INPUT (Owner Only)
        if (IsOwner && !isDead)
        {
            HandleInput();
        }

        // 2. ROTATION SYNC (Manual Fix)
        // যদি NetworkTransform কাজ না করে, এই কোডটি ক্লায়েন্টকে জোর করে ঘোরাবে
        if (IsServer)
        {
            // সার্ভার তার বর্তমান রোটেশন ভেরিয়েবলে আপডেট করবে
            netRotationZ.Value = transform.eulerAngles.z;
        }
        else
        {
            // ক্লায়েন্ট সেই ভেরিয়েবল পড়ে নিজেকে ঘোরাবে (Smoothly)
            float syncedZ = netRotationZ.Value;
            float currentZ = transform.eulerAngles.z;
            float newZ = Mathf.LerpAngle(currentZ, syncedZ, Time.deltaTime * 10f);

            // ফিজিক্সের সাথে যাতে সমস্যা না হয়, তাই শুধু ভিজ্যুয়াল রোটেশন আপডেট করছি
            if (rb.bodyType == RigidbodyType2D.Kinematic || !IsOwner)
                transform.rotation = Quaternion.Euler(0, 0, newZ);
        }

        // 3. GUN ANIMATION (All Clients)
        if (gunPivot != null && Mathf.Abs(currentGunRotation) > 0.1f)
        {
            currentGunRotation = Mathf.Lerp(currentGunRotation, 0f, Time.deltaTime * gunReturnSpeed);
            gunPivot.localRotation = originalGunRotation * Quaternion.Euler(0, 0, currentGunRotation);
        }
    }

    void HandleInput()
    {
        if (Time.time < nextFireTime) return;

        bool shouldShoot = false;
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0)) shouldShoot = true;
#endif
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) shouldShoot = true;

        if (shouldShoot)
        {
            ShootServerRpc(shootDirection);
            nextFireTime = Time.time + fireCooldown;
        }
    }

    [ServerRpc]
    void ShootServerRpc(int direction)
    {
        if (isDead) return;

        // Spawn Bullet
        if (bulletPrefab != null && firePoint != null)
        {
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
            var netObj = bullet.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                var bScript = bullet.GetComponent<NetworkBullet>();
                if (bScript != null) bScript.Initialize(OwnerClientId, direction > 0 ? Vector2.right : Vector2.left);
                netObj.Spawn();
            }
        }

        // Apply Physics
        if (rb != null)
        {
            Vector2 recoilDir = direction > 0 ? Vector2.left : Vector2.right;
            rb.AddForce(recoilDir * recoilForce, ForceMode2D.Impulse);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

            // Add Torque (Spin)
            rb.AddTorque(torqueForce * -direction, ForceMode2D.Impulse);
        }

        PlayShootEffectsClientRpc(direction);
    }

    [ClientRpc]
    void PlayShootEffectsClientRpc(int direction)
    {
        if (shootParticle != null) shootParticle.Play();

        if (audioSource != null && audioShootClips != null && audioShootClips.Length > 0)
        {
            audioSource.clip = audioShootClips[Random.Range(0, audioShootClips.Length)];
            audioSource.Play();
        }

        if (gunPivot != null)
        {
            currentGunRotation = -maxGunRotation;
            gunPivot.localRotation = originalGunRotation * Quaternion.Euler(0, 0, currentGunRotation);
        }
    }

    // Health & Death Logic...
    public void TakeDamage(int damage)
    {
        if (!IsServer || isDead) return;
        currentHealth.Value = Mathf.Clamp(currentHealth.Value - damage, 0, maxHealth);
    }

    private void OnHealthChanged(int oldHealth, int newHealth)
    {
        UpdateHealthBar();
        if (newHealth <= 0 && !isDead) Die();
    }

    void UpdateHealthBar()
    {
        if (healthBar != null) healthBar.fillAmount = (float)currentHealth.Value / maxHealth;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        PlayDeathEffectsClientRpc();
        if (IsServer) Destroy(gameObject, 1f);
    }

    [ClientRpc]
    private void PlayDeathEffectsClientRpc()
    {
        if (GetComponent<SpriteRenderer>()) GetComponent<SpriteRenderer>().enabled = false;
        if (gunPivot != null) gunPivot.gameObject.SetActive(false);
        if (destroyParticle != null) destroyParticle.Play();
        if (GetComponent<Collider2D>()) GetComponent<Collider2D>().enabled = false;
    }
}