using UnityEngine;
using UnityEngine.UI; // 用于 UI Image
using System.Collections;
using System.Collections.Generic;

public class RoomSoldier : MonoBehaviour
{
    [Header("建筑生命")]
    public float maxHealth = 200f; // 建筑的最大生命值
    private float currentHealth; // 建筑当前生命值

    [Header("UI引用 (可选)")]
    [Tooltip("请拖拽你的HP血条Canvas预制体到这里。此Canvas应包含一个名为'Fill'的Image组件用于血条填充。")]
    public GameObject hpBarPrefab; // 这里拖拽你的 HP_UI Canvas 预制体
    private Image hpFill;
    private Transform hpBarTransform; // 引用实例化出的Canvas的Transform

    [Header("效果引用 (可选)")]
    public GameObject destructionEffectPrefab; // 销毁时的粒子效果或动画

    // --- 士兵预制体设置 ---
    [Header("士兵预制体设置")]
    [Tooltip("要生成的士兵预制体")]
    public GameObject soldierPrefab;

    [Header("初始生成设置")]
    [Tooltip("游戏开始时一次性生成的士兵数量")]
    public int initialSoldierCount = 5;

    [Header("定时生成设置")]
    [Tooltip("是否开启定时生成")]
    public bool enableTimedSpawning = true;
    [Tooltip("每次定时生成的士兵数量")]
    public int timedSpawnCount = 2;
    [Tooltip("两次生成之间的时间间隔（秒）")]
    public float spawnInterval = 10f;

    [Header("生成位置设置")]
    [Tooltip("士兵生成区域的半径，围绕兵营中心")]
    public float spawnRadius = 5f;
    [Tooltip("每次尝试查找生成位置的最大次数，避免无限循环")]
    public int maxSpawnAttempts = 10;
    [Tooltip("士兵预制体的半径或碰撞体大小，用于避免重叠。确保士兵预制体有碰撞体！")]
    public float soldierDetectionRadius = 0.8f;

    private List<GameObject> spawnedSoldiers = new List<GameObject>();


    void Start()
    {
        currentHealth = maxHealth; // 初始化生命值
        SetupHPBar(); // 调用新的设置血条方法

        // 1. 游戏开始时一次性生成士兵
        SpawnSoldiers(initialSoldierCount);

        // 2. 如果开启了定时生成，则启动协程
        if (enableTimedSpawning)
        {
            StartCoroutine(TimedSoldierSpawn());
        }
    }

    void Update()
    {
        // 更新血条位置以跟随建筑
        if (hpBarTransform != null)
        {
            hpBarTransform.position = transform.position + Vector3.up * 3f; // 调整偏移量，保持血条在建筑上方

            // 确保血条始终面向摄像机
            if (Camera.main != null)
            {
                hpBarTransform.forward = Camera.main.transform.forward;
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
            // 实例化血条预制体，并将其定位在建筑上方
            GameObject hpBarInstance = Instantiate(hpBarPrefab, transform.position + Vector3.up * 3f, Quaternion.identity);
            hpBarTransform = hpBarInstance.transform;

            // 尝试查找血条填充 Image 组件。这里假定其路径是 "Background/Fill"
            hpFill = hpBarTransform.Find("Background/Fill")?.GetComponent<Image>();

            if (hpFill == null)
            {
                Debug.LogError($"Barracks: 在 {hpBarInstance.name} 中找不到名为 'Background/Fill' 的 Image 组件。请检查 HP_UI 预制体的结构，并确保Canvas下有此子对象和Image组件。", hpBarInstance);
            }

            // 将血条设置为不依附于任何父对象，使其能独立跟随建筑位置
            hpBarTransform.SetParent(null);
            UpdateHealthUI(); // 初始更新血条UI
        }
        else
        {
            Debug.LogWarning("Barracks: hpBarPrefab 未赋值。建筑将不会显示血条。", this);
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

        // 销毁兵营本身
        Destroy(gameObject);
    }

    /// <summary>
    /// 当兵营被销毁时，同时销毁其血条UI。
    /// </summary>
    void OnDestroy()
    {
        if (hpBarTransform != null)
        {
            // 确保销毁的是血条的 GameObject，而不是仅仅是 Transform
            Destroy(hpBarTransform.gameObject);
        }
        // 可选：清理已生成的士兵列表，或通知他们兵营已被销毁
        // for (int i = spawnedSoldiers.Count - 1; i >= 0; i--)
        // {
        //     if (spawnedSoldiers[i] != null)
        //     {
        //         // spawnedSoldiers[i].GetComponent<SoldierAI>()?.SetBarracksDestroyed();
        //     }
        // }
        // spawnedSoldiers.Clear();
    }


    /// <summary>
    /// 生成指定数量的士兵
    /// </summary>
    /// <param name="count">要生成的士兵数量</param>
    private void SpawnSoldiers(int count)
    {
        if (soldierPrefab == null)
        {
            Debug.LogError("士兵预制体未指定！请在 Inspector 中拖拽 Soldier Prefab。", this);
            return;
        }

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPosition = FindValidSpawnPosition();

            if (spawnPosition != Vector3.zero)
            {
                GameObject newSoldier = Instantiate(soldierPrefab, spawnPosition, Quaternion.identity);
                spawnedSoldiers.Add(newSoldier);

                Debug.Log($"生成了士兵：{newSoldier.name} 在位置 {newSoldier.transform.position}", newSoldier);
            }
            else
            {
                Debug.LogWarning("未能找到一个不重叠的士兵生成位置，请检查 spawnRadius 或 maxSpawnAttempts。", this);
            }
        }
    }

    /// <summary>
    /// 查找一个不重叠的有效生成位置
    /// </summary>
    /// <returns>一个不重叠的Vector3位置，如果失败则返回Vector3.zero</returns>
    private Vector3 FindValidSpawnPosition()
    {
        Vector3 basePosition = transform.position;

        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            // 在兵营周围的圆形区域内随机生成一个点
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 potentialSpawnPosition = new Vector3(basePosition.x + randomCircle.x, basePosition.y, basePosition.z + randomCircle.y);

            // 检查这个位置是否会与现有物体发生重叠
            // Physics.CheckSphere 返回true如果球体与任何碰撞体相交
            if (!Physics.CheckSphere(potentialSpawnPosition, soldierDetectionRadius))
            {
                return potentialSpawnPosition; // 找到有效位置
            }
        }

        return Vector3.zero; // 未能找到有效位置
    }

    /// <summary>
    /// 定时生成士兵的协程
    /// </summary>
    private IEnumerator TimedSoldierSpawn()
    {
        // 只有当兵营存在且开启定时生成时才继续
        while (enableTimedSpawning && currentHealth > 0)
        {
            yield return new WaitForSeconds(spawnInterval);
            SpawnSoldiers(timedSpawnCount);
        }
    }

    // 可选：在编辑器中显示生成范围和检测球体，方便调试
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, soldierDetectionRadius);
    }
}