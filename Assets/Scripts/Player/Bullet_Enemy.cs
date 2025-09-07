using UnityEngine;

public class Bullet_Enemy : MonoBehaviour
{
    public float damageAmount = 10f;       
    public float lifetime = 5f;            
    public bool destroyOnHit = true;       

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("EnemyBullet: 击中玩家！");

            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable == null)
            {
                damageable = other.GetComponentInParent<IDamageable>();
            }

            if (damageable != null)
            {
                damageable.TakeDamage(damageAmount);
                Debug.Log("EnemyBullet: 成功调用了玩家的 TakeDamage。");
            }
            else
            {
                Debug.LogWarning("EnemyBullet: 目标未实现 IDamageable 接口。");
            }

            // 命中后销毁
            if (destroyOnHit)
            {
                Destroy(gameObject);
            }
        }
        // 检查墙壁碰撞
        else if (other.CompareTag("Wall"))
        {
            Debug.Log("EnemyBullet: 击中墙壁！");
            if (destroyOnHit)
            {
                Destroy(gameObject);
            }
        }
    }
}
