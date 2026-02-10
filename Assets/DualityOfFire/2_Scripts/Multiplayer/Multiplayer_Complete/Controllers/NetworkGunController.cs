using UnityEngine;
using Unity.Netcode;
using System;

/// <summary>
/// MULTIPLAYER - Base Gun Controller
/// Server-authoritative shooting system
/// Path: Assets/Scripts/Multiplayer/Controllers/NetworkGunController.cs
/// </summary>
public abstract class NetworkGunController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] protected GameObject bulletPrefab;
    [SerializeField] protected Transform firePoint;
    [SerializeField] protected Transform forcePoint;

    [Header("Recoil")]
    [SerializeField] protected float recoilForce = 2f;
    [SerializeField] protected float torqueForce = 0.25f;

    [Header("Particle Systems")]
    [SerializeField] protected ParticleSystem shootParticle;
    [SerializeField] protected ParticleSystem destroyParticle;

    [Header("Shoot Sound Effect")]
    [SerializeField] protected AudioSource audioSources;
    [SerializeField] protected AudioClip[] audioShootClips;

    protected Rigidbody2D rb;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Validation
        if (bulletPrefab == null)
            Debug.LogError($"❌ [{gameObject.name}] Bullet Prefab is NULL!");
        else if (bulletPrefab.GetComponent<NetworkObject>() == null)
            Debug.LogError($"❌ [{gameObject.name}] Bullet prefab missing NetworkObject component!");

        if (firePoint == null)
            Debug.LogError($"❌ [{gameObject.name}] Fire Point is NULL!");
    }

    // Shared input helper
    protected bool HasValidTouch(Func<Vector2, bool> zoneCheck)
    {
        foreach (Touch touch in Input.touches)
        {
            if (touch.phase != TouchPhase.Began)
                continue;

            if (zoneCheck(touch.position))
                return true;
        }
        return false;
    }

    // Client calls this, server spawns bullet
    protected virtual void Shoot(int direction, string layer_gun, string opp)
    {
        // CHECK 1: NetworkManager running
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning("⚠️ NetworkManager not started! Cannot shoot.");
            return;
        }

        // CHECK 2: NetworkObject spawned
        if (!IsSpawned)
        {
            Debug.LogWarning("⚠️ NetworkObject not spawned! Cannot shoot.");
            return;
        }

        // CHECK 3: Bullet prefab valid
        if (bulletPrefab == null)
        {
            Debug.LogError("❌ Bullet Prefab is NULL! Assign in Inspector!");
            return;
        }

        // Request server to shoot
        ShootServerRpc(direction, layer_gun, opp);
    }

    [ServerRpc(RequireOwnership = false)]
    protected void ShootServerRpc(int direction, string layer_gun, string opp)
    {
        if (bulletPrefab == null || firePoint == null)
        {
            Debug.LogError("❌ Server: Missing bullet prefab or fire point!");
            return;
        }

        // Server spawns the bullet
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

        NetworkObject netObj = bullet.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError("❌ Bullet missing NetworkObject component!");
            Destroy(bullet);
            return;
        }

        netObj.Spawn();

        // Play effects on all clients
        PlayShootEffectsClientRpc(direction, layer_gun, opp);
    }

    [ClientRpc]
    protected void PlayShootEffectsClientRpc(int direction, string layer_gun, string opp)
    {
        // Play particle
        if (shootParticle != null)
            shootParticle.Play();

        // Play sound
        if (audioSources != null && audioShootClips != null && audioShootClips.Length > 0)
        {
            audioSources.clip = audioShootClips[UnityEngine.Random.Range(0, audioShootClips.Length)];
            audioSources.Play();
        }

        // Apply recoil physics
        if (rb != null && firePoint != null)
        {
            Vector2 direct = direction * firePoint.right;

            RaycastHit2D hit = Physics2D.CapsuleCast(
                firePoint.position,
                new Vector2(0.2f, 0.4f),
                CapsuleDirection2D.Horizontal,
                0f,
                direct,
                8f,
                LayerMask.GetMask(layer_gun)
            );

            if (hit.collider != null && hit.collider.CompareTag(opp))
            {
                if (SlowMotionManager.Instance != null)
                    SlowMotionManager.Instance.TriggerSlowMotion(0.5f);
            }

            rb.AddForce(-direction * firePoint.right * recoilForce * 0.15f, ForceMode2D.Impulse);
            rb.AddForce(forcePoint.up * recoilForce * 0.75f, ForceMode2D.Impulse);
            rb.AddTorque(torqueForce * direction, ForceMode2D.Impulse);
        }
    }
}