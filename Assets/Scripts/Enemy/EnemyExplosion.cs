using UnityEngine;
using System.Collections;
using UnityEngine.AI;

/// <summary>
/// 控制敌人自爆的脚本。当生命值低于阈值时触发。
/// </summary>
public class EnemyExplosion : MonoBehaviour
{
    [Header("自爆属性")]
    public float selfDestructHPThreshold = 30f; 
    public float selfDestructDamage = 50f; 
    public float selfDestructRange = 4f;
    public float selfDestructTime = 2f; // 自爆前的倒计时

    private Enemy _enemy;
    private EnemyMovement _enemyMovement;
    private bool _isSelfDestructing = false;

    void Awake()
    {
        _enemy = GetComponentInParent<Enemy>();
        _enemyMovement = GetComponentInParent<EnemyMovement>();

        if (_enemy == null)
        {
            Debug.LogError("EnemyExplosion 脚本需要附加在带有 Enemy 脚本的游戏对象子物体上。", this);
            enabled = false;
        }
    }

    public void StartSelfDestruct()
    {
        // 确保只执行一次自爆逻辑
        if (_isSelfDestructing)
        {
            return;
        }

        _isSelfDestructing = true;
        Debug.Log($"{_enemy.name} 生命值过低，开始自爆！");


        _enemyMovement?.StopMovement();
        Invoke(nameof(SelfDestruct), selfDestructTime);
    }

    private void SelfDestruct()
    {
        Debug.Log($"{_enemy.name} 自爆了，在范围内造成伤害！");


        Collider[] colliders = Physics.OverlapSphere(transform.position, selfDestructRange);
        foreach (Collider hit in colliders)
        {
            // 确保不伤害自己
            if (hit.gameObject == gameObject)
            {
                continue;
            }

            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(selfDestructDamage);
            }
        }

        _enemy?.DestroyEnemy();
    }
}
