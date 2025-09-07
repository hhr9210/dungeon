using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AI;
using System.Collections.Generic;


[RequireComponent(typeof(Collider))]
public class Enemy : MonoBehaviour, IDamageable
{

    [System.Serializable]
    public class EnemyUIData
    {
        public GameObject hpBarPrefab;
        public float hpBarHeight = 2f;
        public bool alwaysFaceCamera = true;
        [Space]
        public string displayName = "Enemy";
        public Color nameColor = Color.white;
        public TMP_FontAsset defaultFontAsset;
        [Space]
        public Color hpTextColor = Color.white;
    }

    [Header("基础属性")]
    public float maxHP = 100f;
    [SerializeField] private EnemyUIData uiSettings;

    [Header("行为属性")]
    public float chaseRange = 5f;
    public bool isRangedAttacker = false; // 标记远程攻击者

    [Header("奖励设置")]
    public int experienceOnDefeat = 50;
    public int goldReward = 10;

    // 内部状态变量
    private float _currentHP;
    public float currentHP => _currentHP;
    private GameObject _currentAttackTarget;
    private bool _isDead = false; 

    // 组件引用
    private NavMeshAgent _navMeshAgent;
    private EnemyMovement _enemyMovement;
    private EnemyAttack _enemyAttack;
    private EnemyShoot _enemyShoot;
    private EnemyExplosion _enemyExplosion;
    private EnemyRush _enemyRush;

    // UI引用
    private Transform hpBarTransform;
    private Transform cameraTransform;
    private Image hpFill;
    private TMP_Text hpText;
    private TMP_Text nameText;

    // 敌人状态枚举，新增了Rush和RangedAttack状态
    private enum EnemyState { Patrol, Chase, Attack, RangedAttack, Rush, SelfDestruct }
    private EnemyState _currentState = EnemyState.Patrol;

    void Awake()
    {
        _currentHP = maxHP;
        _navMeshAgent = GetComponent<NavMeshAgent>();
        _enemyMovement = GetComponent<EnemyMovement>();
        _enemyAttack = GetComponent<EnemyAttack>();
        _enemyShoot = GetComponent<EnemyShoot>();
        _enemyExplosion = GetComponent<EnemyExplosion>();
        _enemyRush = GetComponent<EnemyRush>();

        // 缓存主摄像机
        cameraTransform = Camera.main?.transform;

        // 初始化移动脚本
        if (_enemyMovement != null)
        {
            // 根据攻击类型设置停止距离
            if (isRangedAttacker && _enemyShoot != null)
            {
                _navMeshAgent.stoppingDistance = _enemyShoot.attackRange * 0.9f;
            }
            else if (_enemyAttack != null)
            {
                _navMeshAgent.stoppingDistance = _enemyAttack.attackRange * 0.9f;
            }
        }
    }

    void Start()
    {
        SetupEnemyUI();
    }

    void Update()
    {
        // 敌人死亡则不执行行为
        if (_isDead) return;

        // 优先检查自爆条件
        if (_enemyExplosion != null && _currentHP <= _enemyExplosion.selfDestructHPThreshold)
        {
            _currentState = EnemyState.SelfDestruct;
        }
        else
        {
            // 查找攻击目标
            _currentAttackTarget = FindClosestTargetInChaseRange();

            if (_currentAttackTarget != null)
            {
                float distanceToTarget = Vector3.Distance(transform.position, _currentAttackTarget.transform.position);

                // 检查是否需要冲刺
                if (_enemyRush != null && !_enemyRush.IsCoolingDown && distanceToTarget <= _enemyRush.chargeRange)
                {
                    _currentState = EnemyState.Rush;
                }
                // 如果是远程攻击者，则远程攻击
                else if (isRangedAttacker && _enemyShoot != null && distanceToTarget <= _enemyShoot.attackRange)
                {
                    _currentState = EnemyState.RangedAttack;
                }
                // 如果目标在近战攻击范围内，进行近战攻击
                else if (_enemyAttack != null && distanceToTarget <= _enemyAttack.attackRange)
                {
                    _currentState = EnemyState.Attack;
                }
                // 否则，追逐目标
                else
                {
                    _currentState = EnemyState.Chase;
                }
            }
            else // 如果没有找到目标，则执行巡逻
            {
                _currentState = EnemyState.Patrol;
            }
        }

        // 根据状态调用行为
        switch (_currentState)
        {
            case EnemyState.Patrol:
                _enemyMovement?.HandlePatrolling();
                break;
            case EnemyState.Chase:
                _enemyMovement?.HandleChasing(_currentAttackTarget.transform.position);
                break;
            case EnemyState.Attack:
                _enemyMovement?.StopMovement();
                _enemyMovement?.RotateTowards(_currentAttackTarget.transform.position);
                _enemyAttack?.HandleAttacking(_currentAttackTarget);
                break;
            case EnemyState.RangedAttack:
                _enemyMovement?.StopMovement();
                _enemyMovement?.RotateTowards(_currentAttackTarget.transform.position);
                _enemyShoot?.HandleShooting(_currentAttackTarget);
                break;
            case EnemyState.Rush:
                // 冲刺脚本会处理自己的移动和旋转
                _enemyRush?.HandleCharging(_currentAttackTarget);
                break;
            case EnemyState.SelfDestruct:
                _enemyExplosion?.StartSelfDestruct();
                break;
        }
        UpdateUI();
    }

    // 受到伤害的逻辑
    public void TakeDamage(float damage)
    {
        // 敌人已死亡则返回
        if (_isDead) return;

        _currentHP = Mathf.Max(0, _currentHP - damage);
        UpdateUI();

        if (_currentHP <= 0)
        {
            HandleDeath();
        }
    }

    // 处理死亡
    private void HandleDeath()
    {
        // 确保死亡逻辑只执行一次
        if (_isDead) return;
        _isDead = true;

        // 停止所有移动
        _enemyMovement?.StopMovement();
        if (_navMeshAgent != null) _navMeshAgent.enabled = false;

        // 禁用碰撞体
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        // 给予奖励
        GiveRewards();
        // 延迟销毁
        Invoke("DestroyEnemy", 2f);
    }

    // 给予玩家奖励
    public void GiveRewards()
    {
        PlayerEXP playerExp = FindObjectOfType<PlayerEXP>();
        playerExp?.AddExperience(experienceOnDefeat);
    }

    // 销毁敌人
    public void DestroyEnemy()
    {
        if (hpBarTransform != null)
        {
            Destroy(hpBarTransform.gameObject);
        }
        Destroy(gameObject);
    }

    #region 目标查找

    private GameObject FindClosestTargetInChaseRange()
    {
        // 在追逐范围内查找所有目标
        Collider[] targets = Physics.OverlapSphere(transform.position, chaseRange);
        return FindBestTarget(targets);
    }

    private GameObject FindBestTarget(Collider[] targets)
    {
        GameObject bestTarget = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider target in targets)
        {
            // 仅追逐玩家和房屋
            if (!IsValidTarget(target)) continue;

            float distance = Vector3.Distance(transform.position, target.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                bestTarget = target.gameObject;
            }
        }
        return bestTarget;
    }

    private bool IsValidTarget(Collider target)
    {
        return target != null && target.gameObject.activeInHierarchy &&
                (target.CompareTag("House") || target.CompareTag("Player"));
    }

    #endregion

    #region UI管理

    private void SetupEnemyUI()
    {
        if (uiSettings.hpBarPrefab == null) return;

        hpBarTransform = Instantiate(uiSettings.hpBarPrefab, transform).transform;
        hpBarTransform.localPosition = Vector3.up * uiSettings.hpBarHeight;

        Canvas canvas = hpBarTransform.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.worldCamera = Camera.main;
        }

        hpFill = hpBarTransform.Find("Background/Fill")?.GetComponent<Image>();
        hpText = hpBarTransform.Find("HP_Text")?.GetComponent<TMP_Text>();
        nameText = hpBarTransform.Find("Name_Text")?.GetComponent<TMP_Text>();
        if (nameText == null) CreateNameText();
        UpdateUI();
    }

    private void CreateNameText()
    {
        GameObject nameObj = new GameObject("Name_Text");
        nameObj.transform.SetParent(hpBarTransform);
        nameObj.transform.localPosition = Vector3.up * 0.4f;
        nameObj.transform.localScale = Vector3.one;
        nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = uiSettings.displayName;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = uiSettings.nameColor;
        if (uiSettings.defaultFontAsset != null) nameText.font = uiSettings.defaultFontAsset;
    }

    private void UpdateUI()
    {
        if (hpFill != null) hpFill.fillAmount = _currentHP / maxHP;
        if (hpText != null) hpText.text = $"{Mathf.RoundToInt((_currentHP / maxHP) * 100)}%";
        if (uiSettings.alwaysFaceCamera && cameraTransform != null && hpBarTransform != null)
            hpBarTransform.LookAt(hpBarTransform.position + cameraTransform.forward);
    }

    #endregion

    // 在编辑器中绘制Gizmos以可视化范围
    void OnDrawGizmosSelected()
    {
        // 绘制近战攻击范围
        if (_enemyAttack != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _enemyAttack.attackRange);
        }

        // 绘制远程攻击范围
        if (isRangedAttacker && _enemyShoot != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _enemyShoot.attackRange);
        }

        // 绘制追逐范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        // 绘制冲刺范围
        if (_enemyRush != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, _enemyRush.chargeRange);
        }

        // 绘制自爆范围
        if (_enemyExplosion != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, _enemyExplosion.selfDestructRange);
        }
    }
}
