using UnityEngine;

/// <summary>
/// MULTIPLAYER - Enemy Bullet
/// Targets player
/// Path: Assets/Scripts/Multiplayer/Bullets/NetworkEnemyBullet.cs
/// 
/// SETUP:
/// 1. Attach this to your Enemy Bullet Prefab
/// 2. Add NetworkObject component
/// 3. Add to NetworkManager Prefabs List
/// 4. Bullet moves left automatically
/// </summary>
public class NetworkEnemyBullet : NetworkBulletController
{
    protected override string TargetTag => "Player";
    protected override Vector2 MoveDirection => Vector2.left;
}