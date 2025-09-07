using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class EnemyShoot : MonoBehaviour
{
    [Header("远程攻击属性")]
    public GameObject projectilePrefab;
    public float attackRate = 1f;
    public float projectileSpeed = 10f;
    public float attackRange = 15f;

    private float _nextAttackTime;

    void Awake()
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("Projectile prefab is not assigned on EnemyShoot script!", this);
        }
    }

    public void HandleShooting(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        if (Time.time >= _nextAttackTime)
        {
            if (projectilePrefab != null)
            {
                GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);

                Vector3 direction = (target.transform.position - transform.position).normalized;

                if (projectile.TryGetComponent(out Rigidbody rb))
                {
                    rb.velocity = direction * projectileSpeed;
                }
                else
                {
                    Debug.LogWarning("Projectile prefab does not have a Rigidbody component. It won't move.", this);
                }

                _nextAttackTime = Time.time + 1f / attackRate;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
