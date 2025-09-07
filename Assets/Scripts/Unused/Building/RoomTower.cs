using UnityEngine;
using UnityEngine.UI;

public class RoomTower : MonoBehaviour
{
    [Header("建筑生命")]
    public float maxHealth = 200f; // 建筑的最大生命值

    private float currentHealth; // 建筑当前生命值

    [Header("UI引用 (可选)")]
    [Tooltip("请拖拽你的HP血条Canvas预制体到这里。此Canvas应包含一个名为'Fill'的Image组件用于血条填充。")]
    public GameObject hpBarPrefab; // **这里拖拽你的 HP_UI Canvas 预制体**
    private Image hpFill;
    private Transform hpBarTransform; // 引用实例化出的Canvas的Transform

    [Header("效果引用 (可选)")]
    public GameObject destructionEffectPrefab; // 销毁时的粒子效果或动画

    // --- 新增：建筑攻击属性 ---
    [Header("建筑攻击属性")]
    [Tooltip("是否开启建筑攻击功能")]
    public bool enableAttack = true; // 新增：攻击开启/关闭开关
    public float attackDamage = 15f;    // 建筑攻击伤害
    public float attackRange = 7f;      // 建筑攻击范围
    public float attackRate = 1.5f;     // 建筑每秒攻击次数
    public LayerMask targetLayers;      // 建筑可以攻击的层 (例如 Enemy)

    [Header("建筑攻击视觉和音效")]
    public GameObject attackEffectPrefab; // 攻击时的特效预制体 (例如：炮弹、激光)
    public AudioClip attackSound;       // 攻击音效

    private float nextAttackTime;       // 建筑下次可以攻击的时间点
    private AudioSource audioSource;    // 用于播放音效

    void Awake() // 确保 AudioSource 在 Start 之前可用
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Start()
    {
        currentHealth = maxHealth;
        nextAttackTime = Time.time; // 初始化下次攻击时间

        SetupHPBar(); // 调用新的设置血条方法
    }

    void Update()
    {
        // 更新血条位置以跟随建筑
        if (hpBarTransform != null)
        {
            hpBarTransform.position = transform.position + Vector3.up * 3f; // 调整偏移量

            // 确保血条始终面向摄像机
            if (Camera.main != null)
            {
                hpBarTransform.forward = Camera.main.transform.forward;
            }
        }

        // --- 建筑攻击逻辑 ---
        // 只有当 enableAttack 为 true 时才执行攻击逻辑
        if (enableAttack)
        {
            // 1. 寻找攻击目标
            GameObject currentTarget = FindAttackTarget();

            // 2. 如果找到目标，尝试攻击
            if (currentTarget != null)
            {
                // 确保目标仍然存在且在攻击范围内
                float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
                if (distanceToTarget <= attackRange)
                {
                    RotateTowards(currentTarget.transform.position); // 建筑转向目标 (可选，取决于建筑类型)
                    AttackTarget(currentTarget); // 攻击目标
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
                Debug.LogError($"HouseManager: 在 {hpBarInstance.name} 中找不到名为 'Background/Fill' 的 Image 组件。请检查 HP_UI 预制体的结构，并确保Canvas下有此子对象和Image组件。", hpBarInstance);
            }

            hpBarTransform.SetParent(null);
            UpdateHealthUI(); // 初始更新血条UI
        }
        else
        {
            Debug.LogWarning("HouseManager: hpBarPrefab 未赋值。建筑将不会显示血条。", this);
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
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log($"{gameObject.name} 受到了 {amount} 点伤害。当前生命值: {currentHealth}");

        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            DestroyBuilding();
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

        if (destructionEffectPrefab != null)
        {
            Instantiate(destructionEffectPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (hpBarTransform != null)
            Destroy(hpBarTransform.gameObject);
    }

    /// <summary>
    /// 建筑在攻击范围内寻找最近的敌人作为攻击目标。
    /// </summary>
    /// <returns>最近的敌人GameObject，如果没有则返回null。</returns>
    GameObject FindAttackTarget()
    {
        GameObject closestEnemy = null;
        float shortestDistance = attackRange + 1f;

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange, targetLayers);

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.gameObject.activeInHierarchy)
            {
                Enemy enemy = hitCollider.GetComponent<Enemy>();
                if (enemy != null)
                {
                    float distance = Vector3.Distance(transform.position, hitCollider.transform.position);
                    if (distance < shortestDistance)
                    {
                        shortestDistance = distance;
                        closestEnemy = hitCollider.gameObject;
                    }
                }
            }
        }
        return closestEnemy;
    }

    /// <summary>
    /// 建筑攻击指定目标。
    /// </summary>
    /// <param name="target">要攻击的目标GameObject。</param>
    void AttackTarget(GameObject target)
    {
        if (target == null || !target.activeInHierarchy)
        {
            return;
        }

        if (Time.time >= nextAttackTime)
        {
            Debug.Log($"{gameObject.name} 攻击了 {target.name}，造成 {attackDamage} 伤害。");

            Enemy enemy = target.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(attackDamage);
            }

            if (attackEffectPrefab != null)
            {
                Instantiate(attackEffectPrefab, target.transform.position, Quaternion.identity);
            }
            if (audioSource != null && attackSound != null)
            {
                audioSource.PlayOneShot(attackSound);
            }

            nextAttackTime = Time.time + 1f / attackRate;
        }
    }

    /// <summary>
    /// 使建筑面向指定位置。
    /// </summary>
    /// <param name="lookAtPosition">要面向的位置。</param>
    void RotateTowards(Vector3 lookAtPosition)
    {
        Vector3 direction = (lookAtPosition - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}