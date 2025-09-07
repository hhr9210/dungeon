using UnityEngine;
using UnityEngine.UI; // 用于 UI Image
using System.Collections.Generic;

public class RoomHP : MonoBehaviour
{
    // --- 新增：建筑生命值和UI部分 ---
    [Header("建筑生命")]
    public float maxHealth = 250f; // RoomHP 的最大生命值，可以根据需要调整
    private float currentHealth; // 建筑当前生命值

    [Header("UI引用 (可选)")]
    [Tooltip("请拖拽你的HP血条Canvas预制体到这里。此Canvas应包含一个一个名为'Fill'的Image组件用于血条填充。")]
    public GameObject hpBarPrefab; // 这里拖拽你的 HP_UI Canvas 预制体
    private Image hpFill;
    private Transform hpBarTransform; // 引用实例化出的Canvas的Transform

    [Header("效果引用 (可选)")]
    public GameObject destructionEffectPrefab; // 销毁时的粒子效果或动画
    // --- 新增结束 ---


    [Header("恢复设置")]
    [Tooltip("需要恢复血量的对象Tag")]
    public string targetTag = "Player"; // 默认设置为Player标签
    [Tooltip("每秒恢复的血量值")]
    public float healthPerSecond = 5f;
    [Tooltip("是否显示调试信息")]
    public bool showDebug = false;
    [Tooltip("是否限制最大血量恢复")]
    public bool limitToMaxHealth = true;

    // 存储区域内的所有可恢复对象
    private Dictionary<GameObject, PlayerHealth> targetsInZone = new Dictionary<GameObject, PlayerHealth>();

    void Start()
    {
        currentHealth = maxHealth; // 初始化生命值
        SetupHPBar(); // 设置血条

        // RoomHP 本身需要一个 Collider 来触发 OnTriggerEnter/Exit
        Collider col = GetComponent<Collider>();
        if (col == null || !col.isTrigger)
        {
            Debug.LogWarning($"RoomHP: {gameObject.name} 需要一个触发器 Collider 组件才能工作。", this);
        }
    }

    void Update()
    {
        // 更新血条位置以跟随建筑
        if (hpBarTransform != null)
        {
            hpBarTransform.position = transform.position + Vector3.up * 3f; // 调整偏移量
            if (Camera.main != null)
            {
                hpBarTransform.forward = Camera.main.transform.forward;
            }
        }

        // 每秒恢复血量 (原 RoomHP 逻辑)
        // 使用 ToList 避免在 foreach 中修改集合
        List<GameObject> currentTargets = new List<GameObject>(targetsInZone.Keys);
        foreach (var go in currentTargets)
        {
            // 重新检查是否仍在区域内且有效，以防 OnTriggerExit 延迟
            if (targetsInZone.TryGetValue(go, out PlayerHealth playerHealth))
            {
                if (playerHealth != null && go != null && go.activeInHierarchy)
                {
                    // 检查是否需要恢复（未满血或不受限制）
                    if (!limitToMaxHealth || playerHealth.GetCurrentHealth() < playerHealth.maxHealth)
                    {
                        playerHealth.Heal(healthPerSecond * Time.deltaTime);
                        if (showDebug) Debug.Log($"{go.name} 恢复 {healthPerSecond * Time.deltaTime:F2} 点血量");
                    }
                }
                else
                {
                    // 目标无效，将其标记为移除
                    targetsInZone.Remove(go);
                    if (showDebug) Debug.Log($"RoomHP: 移除无效目标 {go?.name ?? "null"}");
                }
            }
        }
    }

    /// <summary>
    /// 设置并实例化建筑的血条。
    /// </summary>
    void SetupHPBar()
    {
        if (hpBarPrefab != null)
        {
            GameObject hpBarInstance = Instantiate(hpBarPrefab, transform.position + Vector3.up * 3f, Quaternion.identity);
            hpBarTransform = hpBarInstance.transform;

            hpFill = hpBarTransform.Find("Background/Fill")?.GetComponent<Image>();

            if (hpFill == null)
            {
                Debug.LogError($"RoomHP: 在 {hpBarInstance.name} 中找不到名为 'Background/Fill' 的 Image 组件。请检查 HP_UI 预制体的结构，并确保Canvas下有此子对象和Image组件。", hpBarInstance);
            }

            hpBarTransform.SetParent(null); // 设置血条为独立对象
            UpdateHealthUI(); // 初始更新血条UI
        }
        else
        {
            Debug.LogWarning("RoomHP: hpBarPrefab 未赋值。建筑将不会显示血条。", this);
        }
    }

    /// <summary>
    /// 减少建筑当前生命值。
    /// </summary>
    /// <param name="amount">受到的伤害量。</param>
    public void TakeDamage(float amount)
    {
        Debug.Log($"{gameObject.name} 的 TakeDamage 方法被调用，伤害量: {amount}");
        if (currentHealth <= 0)
        {
            Debug.Log($"{gameObject.name} 已经摧毁，不再受到伤害。");
            return;
        }

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0); // 确保生命值不低于0

        Debug.Log($"{gameObject.name} 受到了 {amount} 点伤害。当前生命值: {currentHealth}");

        UpdateHealthUI(); // 更新血条UI

        if (currentHealth <= 0)
        {
            DestroyBuilding(); // 生命值归零，销毁建筑
        }
    }

    /// <summary>
    /// 更新建筑生命值UI（例如，血条填充）。
    /// </summary>
    void UpdateHealthUI()
    {
        if (hpFill != null)
        {
            hpFill.fillAmount = currentHealth / maxHealth;
        }
    }

    /// <summary>
    /// 处理建筑销毁的逻辑。
    /// </summary>
    void DestroyBuilding()
    {
        Debug.Log($"{gameObject.name} 已被摧毁！");

        // 实例化销毁效果
        if (destructionEffectPrefab != null)
        {
            Instantiate(destructionEffectPrefab, transform.position, Quaternion.identity);
        }

        // 销毁 RoomHP 本身
        Destroy(gameObject);
    }

    /// <summary>
    /// 当 RoomHP 被销毁时，同时销毁其血条UI。
    /// </summary>
    void OnDestroy()
    {
        if (hpBarTransform != null)
        {
            Destroy(hpBarTransform.gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 检查进入的对象是否有指定Tag
        if (other.CompareTag(targetTag))
        {
            // 获取PlayerHealth组件
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                // 添加到字典中
                if (!targetsInZone.ContainsKey(other.gameObject))
                {
                    targetsInZone.Add(other.gameObject, playerHealth);
                    if (showDebug) Debug.Log($"{other.name} 进入恢复区域");
                }
            }
            else if (showDebug)
            {
                Debug.LogWarning($"{other.name} 有 {targetTag} 标签但没有 PlayerHealth 组件", other.gameObject);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 检查离开的对象是否有指定Tag
        if (other.CompareTag(targetTag))
        {
            // 从字典中移除
            if (targetsInZone.ContainsKey(other.gameObject))
            {
                targetsInZone.Remove(other.gameObject);
                if (showDebug) Debug.Log($"{other.name} 离开恢复区域");
            }
        }
    }

    // 清理已销毁的对象 (原 RoomHP 逻辑，但现在由 Update 循环中的检查优化)
    // 这个方法可以删除，因为 Update 内部已经有更实时的检查和移除了。
    /*
    private void CleanUpDestroyedObjects()
    {
        List<GameObject> toRemove = new List<GameObject>();

        foreach (var target in targetsInZone)
        {
            if (target.Key == null || target.Value == null)
            {
                toRemove.Add(target.Key);
            }
        }

        foreach (var key in toRemove)
        {
            targetsInZone.Remove(key);
            if (showDebug) Debug.Log("已清理已销毁的对象");
        }
    }
    */

    // 在编辑器可视化区域
    private void OnDrawGizmos()
    {
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f); // 半透明绿色
            if (collider is BoxCollider boxCollider)
            {
                Gizmos.matrix = Matrix4x4.TRS(transform.position + boxCollider.center, transform.rotation, transform.lossyScale);
                Gizmos.DrawCube(Vector3.zero, boxCollider.size);
            }
            else if (collider is SphereCollider sphereCollider)
            {
                Gizmos.matrix = Matrix4x4.TRS(transform.position + sphereCollider.center, transform.rotation, transform.lossyScale);
                Gizmos.DrawSphere(Vector3.zero, sphereCollider.radius);
            }
            else if (collider is CapsuleCollider capsuleCollider)
            {
                // 胶囊体绘制稍复杂，这里简化为圆柱
                Gizmos.matrix = Matrix4x4.TRS(transform.position + capsuleCollider.center, transform.rotation, transform.lossyScale);
                Gizmos.DrawSphere(Vector3.zero, capsuleCollider.radius); // 使用球体代替
            }
        }
    }
}