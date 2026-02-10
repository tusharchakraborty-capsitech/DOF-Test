using UnityEngine;
using Unity.Netcode;

/// <summary>
/// MULTIPLAYER - Base Bullet Controller
/// Server-authoritative bullet movement and collision
/// Path: Assets/Scripts/Multiplayer/Controllers/NetworkBulletController.cs
/// </summary>
public abstract class NetworkBulletController : NetworkBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private int damage = 20;
    [SerializeField] private GameObject BulletVisual;

    [Header("Particle Systems")]
    [SerializeField] private ParticleSystem hitParticleSystem;
    [SerializeField] private ParticleSystem wallHitParticleSystem;

    protected abstract string TargetTag { get; }
    protected virtual Vector2 MoveDirection => Vector2.right;

    protected virtual void Update()
    {
        // Only the server moves bullets to avoid desync
        if (!IsServer) return;

        transform.Translate(MoveDirection * speed * Time.deltaTime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Only server handles collision logic
        if (!IsServer) return;

        if (collision.gameObject.CompareTag(TargetTag))
        {
            // Trigger slow motion on all clients
            TriggerSlowMotionClientRpc(0.3f);

            // Play particle on all clients
            if (hitParticleSystem != null)
                PlayHitParticleClientRpc();

            // Get components
            NetworkEnemyGun enemy = collision.gameObject.GetComponent<NetworkEnemyGun>();
            NetworkPlayerGun player = collision.gameObject.GetComponent<NetworkPlayerGun>();

            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
            if (player != null)
            {
                player.TakeDamage(damage);
            }

            DestroyBulletClientRpc();
        }
        else if (collision.gameObject.CompareTag("Wall"))
        {
            PlayWallHitParticleClientRpc();
            DestroyBulletClientRpc();
        }
        else if (collision.gameObject.CompareTag("Bullet"))
        {
            TriggerSlowMotionClientRpc(0.5f);

            if (hitParticleSystem != null)
                PlayHitParticleClientRpc();

            DestroyBulletClientRpc();
        }
    }

    [ClientRpc]
    private void TriggerSlowMotionClientRpc(float duration)
    {
        if (SlowMotionManager.Instance != null)
            SlowMotionManager.Instance.TriggerSlowMotion(duration);
    }

    [ClientRpc]
    private void PlayHitParticleClientRpc()
    {
        if (hitParticleSystem != null)
            hitParticleSystem.Play();
    }

    [ClientRpc]
    private void PlayWallHitParticleClientRpc()
    {
        if (wallHitParticleSystem != null)
            wallHitParticleSystem.Play();
    }

    [ClientRpc]
    private void DestroyBulletClientRpc()
    {
        if (BulletVisual != null)
            BulletVisual.SetActive(false);

        var collider = GetComponent<BoxCollider2D>();
        if (collider != null)
            collider.enabled = false;

        if (IsServer)
        {
            Destroy(gameObject, 0.15f);
        }
    }
}