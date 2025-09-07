using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;



public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("玩家生命")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("防御属性")]
    [Range(0f, 0.9f)]
    public float damageResistance = 1.2f;

    [Header("UI引用")]
    public GameObject hpBarPrefab;
    public float hpbarDis;

    private Image hpFill;
    private Transform hpBarTransform;
    private TextMeshProUGUI hpBarNameText;

    [Header("受击反馈")]
    public Material hitEffectMaterial;
    public float borderColorFlashDuration = 0.2f;
    public Color hitFlashColor = Color.red;

    private Color originalBorderColor;

    [Header("玩家信息")]
    public string playerName = "玩家1";

    public TextMeshProUGUI healthRatioText;
    public Image mainUIHealthBar;
    public TextMeshProUGUI damageResistanceText;

    public delegate void OnHealthChanged(float currentHealth, float maxHealth);
    public static event OnHealthChanged onHealthChanged;

    public delegate void OnPlayerDied();
    public static event OnPlayerDied onPlayerDied;

    public float CurrentHealth
    {
        get { return currentHealth; }
    }

    void Awake()
    {
        currentHealth = maxHealth;
        SetupHPBar();

        if (damageResistanceText != null)
        {
            damageResistanceText.text = $"{damageResistance:P0}";
        }

        if (hitEffectMaterial != null)
        {
            if (hitEffectMaterial.HasProperty("_BorderColor"))
            {
                originalBorderColor = hitEffectMaterial.GetColor("_BorderColor");
            }
            else
            {
                Debug.LogWarning("PlayerHealth: 材质的Shader没有名为'_BorderColor'的属性。请检查Shader。", this);
            }
        }
        else
        {
            Debug.LogWarning("PlayerHealth: hitEffectMaterial 未赋值。", this);
        }

        UpdateHealthUI();
    }

    void Update()
    {
        if (hpBarTransform != null)
        {
            hpBarTransform.position = transform.position + Vector3.up * hpbarDis;

            if (Camera.main != null)
            {
                hpBarTransform.forward = Camera.main.transform.forward;
            }
        }
    }

    void SetupHPBar()
    {
        if (hpBarPrefab != null)
        {
            GameObject hpBarInstance = Instantiate(hpBarPrefab, transform.position + Vector3.up * 2.5f, Quaternion.identity);
            hpBarTransform = hpBarInstance.transform;

            hpFill = hpBarTransform.Find("Background/Fill")?.GetComponent<Image>();
            hpBarNameText = hpBarTransform.Find("Name_Text")?.GetComponent<TextMeshProUGUI>();

            if (hpFill == null)
            {
                Debug.LogWarning($"PlayerHealth: 在实例化的血条预制体中未找到 'Background/Fill' Image组件。", hpBarInstance);
            }

            if (hpBarNameText != null)
            {
                hpBarNameText.text = playerName;
            }
            else
            {
                Debug.LogWarning($"PlayerHealth: 在实例化的血条预制体中未找到 'Name_Text' TextMeshProUGUI组件。", hpBarInstance);
            }
        }
        else
        {
            Debug.LogWarning("PlayerHealth: hpBarPrefab 未赋值。", this);
        }
    }

    public void TakeDamage(float amount)
    {
        if (currentHealth <= 0) return;

        float finalDamage = amount * (1f - damageResistance);
        currentHealth -= finalDamage;
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log($"玩家受到了 {amount} 点伤害 (实际承受 {finalDamage} 点)。当前生命值: {currentHealth}");

        UpdateHealthUI();
        onHealthChanged?.Invoke(currentHealth, maxHealth);

        if (hitEffectMaterial != null && hitEffectMaterial.HasProperty("_BorderColor"))
        {
            StartCoroutine(FlashBorderColor());
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (currentHealth >= maxHealth) return;

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        Debug.Log($"玩家恢复了 {amount} 点生命值。当前生命值: {currentHealth}");

        UpdateHealthUI();
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void IncreaseMaxHealth(float amount)
    {
        if (amount <= 0) return;
        maxHealth += amount;
        currentHealth += amount;
        Debug.Log($"最大生命值增加 {amount}。新最大生命值: {maxHealth}");
        UpdateHealthUI();
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void IncreaseResistance(float amount)
    {
        if (amount <= 0) return;
        damageResistance = Mathf.Min(damageResistance + amount, 0.9f);
        Debug.Log($"抗性增加 {amount}。新抗性: {damageResistance:P0}");
        if (damageResistanceText != null)
        {
            damageResistanceText.text = $"{damageResistance:P0}";
        }
    }

    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    void UpdateHealthUI()
    {
        if (hpFill != null)
        {
            hpFill.fillAmount = currentHealth / maxHealth;
        }

        if (mainUIHealthBar != null)
        {
            mainUIHealthBar.fillAmount = currentHealth / maxHealth;
        }

        if (healthRatioText != null)
        {
            healthRatioText.text = $"{Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(maxHealth)}";
        }
    }

    void Die()
    {
        Debug.Log("玩家已死亡！游戏结束。");
        onPlayerDied?.Invoke();
    }

    private IEnumerator FlashBorderColor()
    {
        hitEffectMaterial.SetColor("_BorderColor", hitFlashColor);
        yield return new WaitForSeconds(borderColorFlashDuration);

        hitEffectMaterial.SetColor("_BorderColor", originalBorderColor);
    }

    void OnDestroy()
    {
        if (hpBarTransform != null)
        {
            Destroy(hpBarTransform.gameObject);
        }

        if (hitEffectMaterial != null && hitEffectMaterial.HasProperty("_BorderColor"))
        {
            hitEffectMaterial.SetColor("_BorderColor", originalBorderColor);
        }
    }
}
