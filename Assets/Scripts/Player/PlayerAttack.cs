using UnityEngine;
using TMPro; 
using UnityEngine.UI; 
using UnityEngine.EventSystems; 
using UnityEngine.AI; 
using System.Collections; 

public class PlayerAttack : MonoBehaviour
{
    [Header("A 键：近战攻击属性")]
    public float meleeAttackDamage = 30f;
    public float meleeAttackRange = 2f;
    [Tooltip("此值只用于伤害检测范围，不再控制剑的视觉旋转。")]
    public float meleeAttackArc = 120f;
    public float meleeAttackCooldown = 0.8f;
    public LayerMask meleeAttackableLayers;

    [Header("近战攻击视觉与音频")]
    public GameObject meleeHitEffectPrefab;
    public AudioClip meleeAttackSound;

    [Header("动画设置")]
    [Tooltip("玩家的Animator组件。")]
    public Animator playerAnimator;
    [Tooltip("近战挥舞动画剪辑的名称（在Animator Controller中设置的State名称）。")]
    public string swingAnimationName = "MeleeAttack";

    private float nextMeleeAttackTime;

    [Header("近战攻击锁定")]
    [Tooltip("近战攻击期间玩家位置锁定的时间。")]
    public float meleeLockDuration = 0.3f;
    private bool isMeleeAttackingLocked = false;

    [Header("力量")]
    [SerializeField] private int currentStrength = 0;

    [Header("攻击力增益 (AttUp)")]
    public float attackBuffAmount = 10f;
    public float attackBuffDuration = 10f;
    private float originalMeleeAttackDamage;
    private Coroutine attackBuffCoroutine;

    [Header("UI 显示（攻击）")]
    public TextMeshProUGUI attackDamageText;
    public TextMeshProUGUI attackSpeedText;
    public TextMeshProUGUI attackRangeText;

    private NavMeshAgent playerNavMeshAgent;
    private AudioSource audioSource;
    private bool canAttack = true;

    void Awake()
    {
        playerNavMeshAgent = GetComponent<NavMeshAgent>();
        if (playerNavMeshAgent == null)
        {
            Debug.LogWarning("PlayerAttack: NavMeshAgent component not found on player. Attack stopping will not work.", this);
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        if (playerAnimator == null)
        {
            playerAnimator = GetComponent<Animator>();
            if (playerAnimator == null)
            {
                Debug.LogError("PlayerAttack: Animator component not found on player or not assigned! Please add an Animator to your player.", this);
            }
        }
    }

    void OnEnable()
    {
        if (PauseManager.Instance != null)
        {
            PauseManager.PauseStateChanged += OnPauseStateChanged;
        }
        else
        {
            Debug.LogWarning("PlayerAttack: 场景中没有找到 PauseManager 实例！玩家攻击功能可能无法正确响应暂停。", this);
        }
    }

    void OnDisable()
    {
        if (PauseManager.Instance != null)
        {
            PauseManager.PauseStateChanged -= OnPauseStateChanged;
        }
    }

    void Start()
    {
        nextMeleeAttackTime = Time.time;
        UpdateUI();
        originalMeleeAttackDamage = meleeAttackDamage;
    }

    void Update()
    {
        if (!canAttack)
        {
            if (isMeleeAttackingLocked)
            {
                isMeleeAttackingLocked = false;
                if (playerNavMeshAgent != null && playerNavMeshAgent.enabled)
                {
                    playerNavMeshAgent.isStopped = false;
                }
            }
            return;
        }

        HandleMeleeLock();
        HandleAttackInput();
    }

    private void OnPauseStateChanged(bool isPaused)
    {
        canAttack = !isPaused;
        Debug.Log($"PlayerAttack: canAttack set to {!isPaused} due to game pause state change.");
    }

    private void HandleAttackInput()
    {
        if (!canAttack) return;

        if (!isMeleeAttackingLocked)
        {
            if (Input.GetMouseButtonDown(0) && Time.time >= nextMeleeAttackTime)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    Debug.Log("PlayerAttack: 鼠标悬停在UI上，阻止攻击。");
                    return;
                }
                
                PerformMeleeAttack();
                nextMeleeAttackTime = Time.time + meleeAttackCooldown;
            }
        }
    }

    private void HandleMeleeLock()
    {
        if (isMeleeAttackingLocked)
        {
            if (playerNavMeshAgent != null && playerNavMeshAgent.enabled)
            {
                playerNavMeshAgent.isStopped = true;
                playerNavMeshAgent.velocity = Vector3.zero;
            }
        }
    }

    private void PerformMeleeAttack()
    {
        Debug.Log("执行鼠标左键近战攻击！");

        if (playerAnimator == null)
        {
            Debug.LogWarning("无法执行攻击：Animator 未分配或未找到。");
            return;
        }
        
        playerAnimator.SetTrigger("Attack");

        if (audioSource != null && meleeAttackSound != null)
        {
            audioSource.PlayOneShot(meleeAttackSound);
        }
        else
        {
            Debug.LogWarning("PlayerAttack: 没有分配近战攻击音效或AudioSource为空。无法播放音效。", this);
        }
    }
    
    public void OnAttackStart()
    {
        isMeleeAttackingLocked = true;
    }

    public void OnAttackHitFrame()
    {
        if (canAttack)
        {
            DetectMeleeHits();
        }
    }

    public void OnAttackEnd()
    {
        isMeleeAttackingLocked = false;
        if (playerNavMeshAgent != null && playerNavMeshAgent.enabled)
        {
            playerNavMeshAgent.isStopped = false;
        }
    }

    private void DetectMeleeHits()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, meleeAttackRange, meleeAttackableLayers);

        bool hitSomething = false;
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.gameObject == gameObject) continue;

            Vector3 directionToTarget = (hitCollider.transform.position - transform.position).normalized;
            directionToTarget.y = 0; 
            
            Vector3 playerForwardFlat = transform.forward;
            playerForwardFlat.y = 0;

            float angleToTarget = Vector3.Angle(playerForwardFlat, directionToTarget);

            if (angleToTarget <= meleeAttackArc / 2f)
            {
                IDamageable damageable = hitCollider.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(meleeAttackDamage);
                    Debug.Log($"{hitCollider.name} 受到近战攻击，造成 {meleeAttackDamage} 点伤害。");
                    hitSomething = true;
                    if (meleeHitEffectPrefab != null)
                    {
                        Instantiate(meleeHitEffectPrefab, hitCollider.transform.position + Vector3.up * 0.5f, Quaternion.identity);
                    }
                }
            }
        }

        if (!hitSomething)
        {
            Debug.Log("近战攻击未命中范围内的任何目标。");
        }
    }

    void UpdateUI()
    {
        if (attackDamageText != null)
            attackDamageText.text = $"近战伤害: {meleeAttackDamage}";

        if (attackSpeedText != null)
            attackSpeedText.text = $"近战冷却: {meleeAttackCooldown:F2}s";

        if (attackRangeText != null)
            attackRangeText.text = $"近战范围: {meleeAttackRange}";
    }

    public void IncreaseStrength(int amount)
    {
        if (amount < 0) return;
        currentStrength += amount;
        Debug.Log($"力量增加 {amount} 点。当前力量: {currentStrength}");
        IncreaseAttackDamage(amount * 1.5f);
    }

    public void IncreaseAttackDamage(float amount)
    {
        if (amount <= 0) return;
        meleeAttackDamage += amount;
        Debug.Log($"攻击伤害增加 {amount} 点。新的近战伤害: {meleeAttackDamage}");
        UpdateUI();
    }

    public void IncreaseAttackSpeed(float amount)
    {
        if (amount <= 0) return;
        meleeAttackCooldown = Mathf.Max(0.1f, meleeAttackCooldown - amount);
        Debug.Log($"攻击速度提高。新的近战冷却: {meleeAttackCooldown}s");
        UpdateUI();
    }

    public void ApplyAttackBuff()
    {
        if (attackBuffCoroutine != null)
        {
            StopCoroutine(attackBuffCoroutine);
            meleeAttackDamage = originalMeleeAttackDamage;
        }

        meleeAttackDamage += attackBuffAmount;
        Debug.Log($"攻击力上升了 {attackBuffAmount} 点！新的攻击力为: {meleeAttackDamage}");
        UpdateUI();

        attackBuffCoroutine = StartCoroutine(AttackBuffDurationCoroutine());
    }

    private IEnumerator AttackBuffDurationCoroutine()
    {
        yield return new WaitForSeconds(attackBuffDuration);
        meleeAttackDamage = originalMeleeAttackDamage;
        Debug.Log($"攻击力增益结束。攻击力恢复到: {meleeAttackDamage}");
        UpdateUI();
        attackBuffCoroutine = null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 sphereCenter = transform.position + transform.forward * (meleeAttackRange / 2f);
        Gizmos.DrawWireSphere(sphereCenter, meleeAttackRange / 2f);

        Gizmos.color = Color.yellow;
        Vector3 forward = transform.forward;
        Vector3 origin = transform.position;

        Vector3 leftDir = Quaternion.Euler(0, -meleeAttackArc / 2, 0) * forward;
        Vector3 rightDir = Quaternion.Euler(0, meleeAttackArc / 2, 0) * forward;

        Gizmos.DrawRay(origin, leftDir * meleeAttackRange);
        Gizmos.DrawRay(origin, rightDir * meleeAttackRange);

        Gizmos.DrawLine(origin + leftDir * meleeAttackRange, origin + rightDir * meleeAttackRange);
    }
}
