// 该脚本管理玩家技能，包括施放、冷却和UI更新。

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;


public class PlayerSkill : MonoBehaviour
{
    // --- 单例模式 ---
    // 确保场景中只有一个 PlayerSkill 实例。
    public static PlayerSkill Instance { get; private set; }

    [Header("通用技能属性")]
    [Tooltip("全局冷却时间，防止同时施放多个技能。")]
    public float globalCooldown = 0.2f;
    private float nextGlobalReadyTime;

    // 当前绑定到 Q 和 E 键的技能。
    [Header("绑定技能")]
    [Tooltip("绑定到 Q 键的技能。")]
    public Skill currentQSkill;
    [Tooltip("绑定到 E 键的技能。")]
    public Skill currentESkill;

    // 记录每个技能下次可施放的时间。
    private float q_nextReadyTime;
    private float e_nextReadyTime;

    // --- 技能UI引用 (Q 键) ---
    [Header("技能UI引用 (Q 键)")]
    public Image q_skillImage;
    public TextMeshProUGUI q_cooldownText;
    public CanvasGroup q_canvasGroup;
    public TextMeshProUGUI q_manaCostText;

    [Header("技能UI引用 (E 键)")]
    public Image e_skillImage;
    public TextMeshProUGUI e_cooldownText;
    public CanvasGroup e_canvasGroup;
    public TextMeshProUGUI e_manaCostText;

    // --- 内部引用 ---
    private AudioSource audioSource;
    private PlayerHealth playerHealth;
    private PlayerMana playerMana;
    private PlayerAttack playerAttack;
    private PlayerController playerMovement;

    void Awake()
    {
        // 单例模式实现
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("More than one PlayerSkill instance found. Destroying duplicate.", this);
            Destroy(gameObject);
            return;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("PlayerHealth script not found.");
        }

        playerMana = GetComponent<PlayerMana>();
        if (playerMana == null)
        {
            Debug.LogError("PlayerMana script not found.");
        }

        playerAttack = GetComponent<PlayerAttack>();
        if (playerAttack == null)
        {
            Debug.LogError("PlayerAttack script not found.");
        }

        playerMovement = GetComponent<PlayerController>();
        if (playerMovement == null)
        {
            Debug.LogError("PlayerMovement script not found.");
        }

        nextGlobalReadyTime = Time.time;

        InitializeSkillBindings();
    }

    void Update()
    {
        // 更新所有绑定技能的UI显示。
        UpdateAllSkillUIs();

        // 检查全局冷却时间。
        if (Time.time < nextGlobalReadyTime)
        {
            return;
        }

        // --- Q 技能输入检测 ---
        if (Input.GetKeyDown(KeyCode.Q))
        {
            TryCastSkill(currentQSkill, ref q_nextReadyTime, "Q");
        }

        // --- E 技能输入检测 ---
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryCastSkill(currentESkill, ref e_nextReadyTime, "E");
        }
    }

    /// <summary>
    /// 初始化 Q/E 技能的绑定和UI。
    /// </summary>
    private void InitializeSkillBindings()
    {
        if (currentQSkill != null)
        {
            q_nextReadyTime = Time.time;
            UpdateSkillDisplay(q_skillImage, q_manaCostText, q_canvasGroup, currentQSkill);
        }
        else
        {
            HideSkillUI(q_skillImage, q_cooldownText, q_canvasGroup, q_manaCostText);
        }

        if (currentESkill != null)
        {
            e_nextReadyTime = Time.time;
            UpdateSkillDisplay(e_skillImage, e_manaCostText, e_canvasGroup, currentESkill);
        }
        else
        {
            HideSkillUI(e_skillImage, e_cooldownText, e_canvasGroup, e_manaCostText);
        }
    }

    /// <summary>
    /// 动态绑定一个新技能到 Q 键。
    /// </summary>
    public void AssignQSkill(Skill skillToAssign)
    {
        currentQSkill = skillToAssign;
        q_nextReadyTime = Time.time;
        UpdateSkillDisplay(q_skillImage, q_manaCostText, q_canvasGroup, currentQSkill);
        Debug.Log($"Skill '{skillToAssign.skillName}' assigned to Q key.");
    }

    /// <summary>
    /// 动态绑定一个新技能到 E 键。
    /// </summary>
    public void AssignESkill(Skill skillToAssign)
    {
        currentESkill = skillToAssign;
        e_nextReadyTime = Time.time;
        UpdateSkillDisplay(e_skillImage, e_manaCostText, e_canvasGroup, currentESkill);
        Debug.Log($"Skill '{skillToAssign.skillName}' assigned to E key.");
    }

    /// <summary>
    /// 检查条件并尝试施放指定技能。
    /// </summary>
    void TryCastSkill(Skill skill, ref float nextReadyTimeRef, string keyName)
    {
        if (skill == null || Time.time < nextReadyTimeRef)
        {
            Debug.LogWarning($"Cannot cast {keyName} skill. Skill is on cooldown or not assigned.");
            return;
        }

        if (playerMana != null && playerMana.CurrentMana < skill.manaCost)
        {
            Debug.Log($"{keyName} skill '{skill.skillName}' not enough mana!");
            return;
        }

        if (playerHealth != null && skill.healthCost > 0 && playerHealth.CurrentHealth <= skill.healthCost)
        {
            Debug.Log($"{keyName} skill '{skill.skillName}' not enough health!");
            return;
        }

        ExecuteSkill(skill);

        ApplyCosts(skill);
        nextReadyTimeRef = Time.time + skill.cooldown;
        nextGlobalReadyTime = Time.time + globalCooldown;
    }

    /// <summary>
    /// 执行技能的实际效果。
    /// </summary>
    void ExecuteSkill(Skill skill)
    {
        Debug.Log($"Casting skill: {skill.skillName}!");

        if (skill.effectPrefab != null) { Instantiate(skill.effectPrefab, transform.position, Quaternion.identity); }
        if (audioSource != null && skill.castSound != null) { audioSource.PlayOneShot(skill.castSound); }

        switch (skill.skillName)
        {
            case "AttUp":
                Debug.Log($"Casted {skill.skillName}! Attack increased for {skill.effectDuration}s.");
                if (playerAttack != null) { playerAttack.ApplyAttackBuff(); }
                break;
            case "Heal":
                Debug.Log($"Casted {skill.skillName}! Healed for {skill.healAmount} health.");
                if (playerHealth != null) { playerHealth.Heal(skill.healAmount); }
                break;
            case "FireField":
                Debug.Log($"Casted {skill.skillName}! Deals {skill.damageAmount} area damage.");
                ApplyAreaDamage(skill.effectRadius, skill.damageAmount);
                break;
            case "SpeedUp":
                Debug.Log($"Casted {skill.skillName}! Speed increased by {skill.speedMultiplier}x for {skill.effectDuration}s.");
                if (playerMovement != null) { StartCoroutine(SpeedUpCoroutine(skill.speedMultiplier, skill.effectDuration)); }
                break;
            case "StoneSkin":
                Debug.Log($"Casted {skill.skillName}! Damage resistance increased by {skill.resistanceIncrease:P0} for {skill.effectDuration}s.");
                if (playerHealth != null) { StartCoroutine(DamageResistUpCoroutine(skill.resistanceIncrease, skill.effectDuration)); }
                break;
            case "UnVis":
                Debug.Log($"Casted {skill.skillName}! Entering stealth for {skill.effectDuration}s.");
                StartCoroutine(InvisibilityCoroutine(skill.effectDuration));
                break;
            default:
                Debug.Log($"Casted unknown skill: {skill.skillName}!");
                break;
        }
    }

    // 处理隐身效果的协程。
    IEnumerator InvisibilityCoroutine(float duration)
    {
        string originalTag = gameObject.tag;
        int originalLayer = gameObject.layer;

        gameObject.tag = "Untagged";
        gameObject.layer = LayerMask.NameToLayer("Default");

        Debug.Log("Player is now invisible.");

        yield return new WaitForSeconds(duration);

        gameObject.tag = originalTag;
        gameObject.layer = originalLayer;

        Debug.Log("Invisibility effect ended.");
    }

    // 处理 SpeedUp 效果的协程。
    IEnumerator SpeedUpCoroutine(float speedMultiplier, float duration)
    {
        if (playerMovement == null) { yield break; }

        playerMovement.moveSpeed *= speedMultiplier;
        Debug.Log($"Speed increased to {playerMovement.moveSpeed}.");

        yield return new WaitForSeconds(duration);

        playerMovement.moveSpeed /= speedMultiplier;
        Debug.Log($"Speed bonus ended. Current speed: {playerMovement.moveSpeed}");
    }

    // 处理 DamageResistUp 效果的协程。
    IEnumerator DamageResistUpCoroutine(float increaseAmount, float duration)
    {
        if (playerHealth == null) { yield break; }

        playerHealth.damageResistance = Mathf.Min(playerHealth.damageResistance + increaseAmount, 0.9f);
        if (playerHealth.damageResistanceText != null) { playerHealth.damageResistanceText.text = $"{playerHealth.damageResistance:P0}"; }

        yield return new WaitForSeconds(duration);

        playerHealth.damageResistance = Mathf.Max(playerHealth.damageResistance - increaseAmount, 0f);
        if (playerHealth.damageResistanceText != null) { playerHealth.damageResistanceText.text = $"{playerHealth.damageResistance}"; }
    }

    /// <summary>
    /// 施放后应用技能消耗。
    /// </summary>
    void ApplyCosts(Skill skill)
    {
        if (playerMana != null && skill.manaCost > 0)
        {
            playerMana.ConsumeMana(skill.manaCost);
            Debug.Log($"Consumed {skill.manaCost} mana.");
        }
        if (playerHealth != null && skill.healthCost > 0)
        {
            playerHealth.TakeDamage(skill.healthCost);
            Debug.Log($"Consumed {skill.healthCost} health.");
        }
    }

    /// <summary>
    /// 对指定半径内的可伤害对象造成伤害。
    /// </summary>
    void ApplyAreaDamage(float radius, float damage)
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius);

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.gameObject == gameObject) { continue; }

            IDamageable damageable = hitCollider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
                Debug.Log($"Dealt {damage} damage to {hitCollider.name}.");
            }
        }
    }

    /// <summary>
    /// 更新单个技能的UI状态（冷却、透明度）。
    /// </summary>
    void UpdateSkillUIState(Image skillImage, TextMeshProUGUI cooldownText, CanvasGroup canvasGroup, float nextReadyTime, float totalCooldown)
    {
        if (skillImage == null || cooldownText == null || canvasGroup == null) return;

        float timeLeft = nextReadyTime - Time.time;

        if (timeLeft > 0)
        {
            cooldownText.gameObject.SetActive(true);
            cooldownText.text = Mathf.CeilToInt(timeLeft).ToString();
            skillImage.fillAmount = timeLeft / totalCooldown;
            canvasGroup.alpha = 0.5f;
        }
        else
        {
            cooldownText.gameObject.SetActive(false);
            skillImage.fillAmount = 1f;
            canvasGroup.alpha = 1f;
        }
    }

    // 更新技能UI的图标和魔法消耗。
    void UpdateSkillDisplay(Image skillImage, TextMeshProUGUI manaCostText, CanvasGroup canvasGroup, Skill skill)
    {
        if (skillImage != null) skillImage.sprite = skill.icon;
        if (manaCostText != null) manaCostText.text = Mathf.CeilToInt(skill.manaCost).ToString();
        if (canvasGroup != null) canvasGroup.alpha = 1f;
    }

    // 隐藏指定技能的所有UI元素。
    void HideSkillUI(Image skillImage, TextMeshProUGUI cooldownText, CanvasGroup canvasGroup, TextMeshProUGUI manaCostText)
    {
        if (skillImage != null) skillImage.enabled = false;
        if (cooldownText != null) cooldownText.gameObject.SetActive(false);
        if (canvasGroup != null) canvasGroup.alpha = 0;
        if (manaCostText != null) manaCostText.gameObject.SetActive(false);
    }

    // 在 Scene 视图中绘制技能范围的 Gizmos。
    void OnDrawGizmosSelected()
    {
        if (currentQSkill != null && currentQSkill.effectRadius > 0)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, currentQSkill.effectRadius);
        }

        if (currentESkill != null && currentESkill.effectRadius > 0)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, currentESkill.effectRadius);
        }
    }

    // 辅助方法，用于检查和记录UI引用问题。
    private void CheckUIReference<T>(T uiReference, string referenceName) where T : Object
    {
        if (uiReference == null)
        {
            Debug.LogError($"PlayerSkill: UI reference '{referenceName}' is not assigned!");
        }
    }

        void UpdateAllSkillUIs()
    {
        // 更新 Q 技能 UI
        if (currentQSkill != null)
        {
            UpdateSkillUIState(q_skillImage, q_cooldownText, q_canvasGroup, q_nextReadyTime, currentQSkill.cooldown);
        }
        else
        {
            HideSkillUI(q_skillImage, q_cooldownText, q_canvasGroup, q_manaCostText);
        }

        // 更新 E 技能 UI
        if (currentESkill != null)
        {
            UpdateSkillUIState(e_skillImage, e_cooldownText, e_canvasGroup, e_nextReadyTime, currentESkill.cooldown);
        }
        else
        {
            HideSkillUI(e_skillImage, e_cooldownText, e_canvasGroup, e_manaCostText);
        }
    }
}


