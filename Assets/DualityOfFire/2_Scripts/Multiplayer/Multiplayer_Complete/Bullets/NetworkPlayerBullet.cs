using UnityEngine;

/// <summary>
/// MULTIPLAYER - Player Bullet
/// Targets enemies
/// Path: Assets/Scripts/Multiplayer/Bullets/NetworkPlayerBullet.cs
/// 
/// SETUP:
/// 1. Attach this to your Player Bullet Prefab
/// 2. Add NetworkObject component
/// 3. Add to NetworkManager Prefabs List
/// 4. Set Target Tag in Inspector to "Enemy" or "AIGun"
/// </summary>
public class NetworkPlayerBullet : NetworkBulletController
{
    protected override string TargetTag => "Enemy"; // Change to "AIGun" if your enemy uses that tag
    protected override Vector2 MoveDirection => Vector2.right;
}