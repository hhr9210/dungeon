using UnityEngine;
using UnityEngine.UI; // 用于 UI Image
using System.Collections.Generic; // 仍然需要，但不是为了减速效果，以防将来扩展

public class SlowBuilding : MonoBehaviour
{
    // --- 建筑生命值和UI ---
    [Header("建筑生命与UI")]
    public float maxHealth = 300f; // 建筑最大生命值
    private float currentHealth;
    [Tooltip("请拖拽你的HP血条Canvas预制体到这里。此Canvas应包含一个名为'Fill'的Image组件用于血条填充。")]
    public GameObject hpBarPrefab;
    private Image hpFill;
    private Transform hpBarTransform;

    // --- 摧毁爆炸效果 ---
    [Header("摧毁爆炸")]
    public GameObject destructionEffectPrefab; // 销毁时的粒子效果或动画预制体
    [Tooltip("建筑爆炸时对附近敌人造成的伤害值。")]
    public float explosionDamage = 50f;
    [Tooltip("爆炸伤害影响的半径。")]
    public float explosionRadius = 7f;

    // 建筑本体的碰撞体，用于被敌人攻击（不再需要 Is Trigger 来检测减速区域）
    private Collider buildingCollider;

    void Awake()
    {
        currentHealth = maxHealth; // 初始化生命值
    }

    void Start()
    {
        SetupHPBar(); // 设置血条UI

        // 确保建筑有一个 Collider，用于被敌人检测和攻击
        buildingCollider = GetComponent<Collider>();
        if (buildingCollider == null)
        {
            Debug.LogError($"SlowBuilding: {gameObject.name} 需要一个 Collider 组件才能被敌人攻击。", this);
        }
        // 如果这个 Collider 是作为实体存在，不应该勾选 Is Trigger
        if (buildingCollider != null && buildingCollider.isTrigger)
        {
            Debug.LogWarning($"SlowBuilding: {gameObject.name}'s Collider 是 Is Trigger。敌人可能穿过它而不是攻击它。", this);
        }

        // 注意：由于没有减速功能，这里不再需要处理 OnTriggerEnter/Exit 或 Physics.OverlapSphere 的初始检测。
    }

    void Update()
    {
        // 实时更新血条位置和朝向
        UpdateHPBarPosition();

        // 由于没有减速区域，也不需要清理 enemiesInSlowZone 字典了。
    }

    /// <summary>
    /// 更新血条UI在世界空间中的位置和朝向。
    /// </summary>
    void UpdateHPBarPosition()
    {
        if (hpBarTransform != null)
        {
            hpBarTransform.position = transform.position + Vector3.up * 3f; // 调整血条在建筑上方的偏移量
            if (Camera.main != null)
            {
                // 确保血条始终是立着的并且面向摄像机
                Vector3 lookDirection = Camera.main.transform.forward;
                lookDirection.y = 0; // 忽略Y轴，只在水平方向面向摄像机，防止倾斜
                if (lookDirection == Vector3.zero) lookDirection = transform.forward; // 防止摄像机和物体在同一直线上

                hpBarTransform.rotation = Quaternion.LookRotation(lookDirection);
            }
        }
    }

    /// <summary>
    /// 实例化并设置血条UI。
    /// </summary>
    void SetupHPBar()
    {
        if (hpBarPrefab != null)
        {
            GameObject hpBarInstance = Instantiate(hpBarPrefab, transform.position + Vector3.up * 3f, Quaternion.identity);
            hpBarTransform = hpBarInstance.transform;

            // 查找血条填充Image组件
            hpFill = hpBarTransform.Find("Background/Fill")?.GetComponent<Image>();
            if (hpFill == null)
            {
                Debug.LogError($"SlowBuilding: 在 {hpBarInstance.name} 中找不到名为 'Background/Fill' 的 Image 组件。请检查 HP_UI 预制体的结构。", hpBarInstance);
            }

            hpBarTransform.SetParent(null); // 将血条设置为场景根对象，不随建筑自身旋转缩放
            UpdateHealthUI(); // 初始更新血条显示
        }
        else
        {
            Debug.LogWarning("SlowBuilding: hpBarPrefab 未赋值。建筑将不会显示血条。", this);
        }
    }

    /// <summary>
    /// 建筑受到伤害。
    /// </summary>
    /// <param name="amount">受到的伤害值。</param>
    public void TakeDamage(float amount)
    {
        if (currentHealth <= 0) return; // 已经摧毁，不再受伤害

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0); // 确保生命值不为负

        Debug.Log($"{gameObject.name} 受到了 {amount} 点伤害。当前生命值: {currentHealth}");
        UpdateHealthUI(); // 更新血条UI

        if (currentHealth <= 0)
        {
            Explode(); // 生命值耗尽，建筑爆炸
        }
    }

    /// <summary>
    /// 更新血条UI的填充量。
    /// </summary>
    void UpdateHealthUI()
    {
        if (hpFill != null)
        {
            hpFill.fillAmount = currentHealth / maxHealth;
        }
    }

    /// <summary>
    /// 建筑爆炸逻辑。
    /// </summary>
    void Explode()
    {
        Debug.Log($"{gameObject.name} 已被摧毁并爆炸！");

        // 1. 实例化爆炸效果
        if (destructionEffectPrefab != null)
        {
            Instantiate(destructionEffectPrefab, transform.position, Quaternion.identity);
        }

        // 2. 对爆炸范围内的敌人造成伤害
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (var hitCol in hitColliders)
        {
            Enemy enemy = hitCol.GetComponent<Enemy>();
            if (enemy != null && enemy.gameObject.activeInHierarchy) // 确保是敌人且仍然存活
            {
                enemy.TakeDamage(explosionDamage);
                Debug.Log($"SlowBuilding 爆炸: {enemy.name} 受到了 {explosionDamage} 点伤害。");
            }
        }

        // 3. 销毁建筑自身
        Destroy(gameObject);
    }

    /// <summary>
    /// 建筑销毁时，同时销毁血条UI。
    /// </summary>
    void OnDestroy()
    {
        if (hpBarTransform != null)
        {
            Destroy(hpBarTransform.gameObject);
        }
    }

    // 在编辑器中绘制爆炸范围Gizmos
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f); // 爆炸范围 (半透明红色)
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}