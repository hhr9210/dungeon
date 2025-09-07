using UnityEngine;
using TMPro;
using System.Collections;

public class Bullet : MonoBehaviour
{
    public float damageAmount = 10f;

    public float lifetime = 5f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter(Collider other)
    {
        GameObject hitObject = other.gameObject;

        Debug.Log($"子弹触发了碰撞，对象：{hitObject.name}，标签：{hitObject.tag}");

        if (hitObject.CompareTag("Player"))
        {
            Debug.Log("子弹击中玩家，准备造成伤害。");

            IDamageable damageable = hitObject.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damageAmount);
                Debug.Log($"子弹成功对玩家造成 {damageAmount} 点伤害。");
            }
            else
            {
                Debug.LogWarning("玩家对象没有 IDamageable 接口，无法造成伤害。");
            }

            return;
        }

        if (hitObject.CompareTag("Enemy"))
        {
            Debug.Log($"子弹击中一个标签为 'Enemy' 的对象: {hitObject.name}");
            IDamageable damageable = hitObject.GetComponent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(damageAmount);
                Debug.Log($"子弹 (Ballet) 成功对 {hitObject.name} (敌人) 造成 {damageAmount} 点伤害。");
            }
            else
            {
                Debug.LogWarning($"对象 {hitObject.name} 有 'Enemy' 标签但没有 IDamageable 组件。请检查预制体/对象是否附加了 Enemy 脚本。");
            }
            Destroy(gameObject);
            return;
        }

        if (hitObject.CompareTag("Box"))
        {
            Debug.Log($"子弹击中一个标签为 'Box' 的对象: {hitObject.name}");
            IDamageable damageable = hitObject.GetComponent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(damageAmount);
                Debug.Log($"子弹 (Ballet) 成功对 {hitObject.name} (Box) 造成 {damageAmount} 点伤害。");
            }
            else
            {
                Debug.LogWarning($"对象 {hitObject.name} 有 'Box' 标签但没有 IDamageable 组件。请确保预制体/对象附加了 IDamageable 接口的实现。");
            }
            Destroy(gameObject);
            return;
        }

        if (hitObject.CompareTag("Wall"))
        {
            Debug.Log($"子弹击中一个标签为 'Wall' 的对象: {hitObject.name}，正在销毁子弹。");
            Destroy(gameObject);
            return;
        }

        Debug.Log($"子弹击中非指定对象 {hitObject.name}。子弹将继续穿透。");
    }
}
