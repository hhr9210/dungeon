using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AI;
using System.Collections.Generic;

/// <summary>
/// 控制敌人近战攻击的脚本。
/// </summary>
public class EnemyAttack : MonoBehaviour
{
    [Header("攻击属性")]
    public float attackDamage = 10f; 
    public float attackRate = 1f; 
    public float attackRange = 1.5f; 


    private float _nextAttackTime;

    /// <summary>
    /// 处理对目标的攻击逻辑。
    /// </summary>
    /// <param name="target">要攻击的目标对象。</param>
    public void HandleAttacking(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        if (Time.time >= _nextAttackTime)
        {
            IDamageable damageable = target.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(attackDamage);
                _nextAttackTime = Time.time + 1f / attackRate; // 设置下次攻击时间
            }
            else
            {
                Debug.LogWarning($"目标 '{target.name}' 没有实现 IDamageable 接口，无法造成伤害。", this);
            }
        }
    }
}
