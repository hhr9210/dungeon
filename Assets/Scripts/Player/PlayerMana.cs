using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PlayerMana : MonoBehaviour
{
    [Header("魔法值设置")]
    public float maxMana = 100f;
    [SerializeField] public float currentMana;
    public float manaRegenRate = 5f;
    public float regenTickInterval = 0.5f;

    private float nextRegenTime;

    [Header("魔法值UI引用")]
    public Image manaFillImage;
    public TextMeshProUGUI manaText;

    public float CurrentMana
    {
        get { return currentMana; }
    }

    public float MaxMana
    {
        get { return maxMana; }
    }

    void Awake()
    {
        currentMana = maxMana;
        nextRegenTime = Time.time + regenTickInterval;
        UpdateManaUI();
    }

    void Update()
    {
        if (Time.time >= nextRegenTime)
        {
            RegenerateMana();
            nextRegenTime = Time.time + regenTickInterval;
        }
    }

    public bool ConsumeMana(float amount)
    {
        if (amount < 0) return false;

        if (currentMana >= amount)
        {
            currentMana -= amount;
            UpdateManaUI();
            Debug.Log($"消耗了 {amount} 点魔法值。当前魔法值: {currentMana}/{maxMana}");
            return true;
        }
        else
        {
            Debug.LogWarning($"魔法值不足！需要 {amount}，当前 {currentMana}。");
            return false;
        }
    }

    public void RegenerateMana()
    {
        if (currentMana < maxMana)
        {
            currentMana += manaRegenRate * regenTickInterval;
            currentMana = Mathf.Min(currentMana, maxMana);
            UpdateManaUI();
        }
    }

    public void IncreaseMaxMana(float amount)
    {
        if (amount <= 0) return;
        maxMana += amount;
        currentMana += amount;
        Debug.Log($"最大魔法值增加了 {amount}。新的最大魔法值: {maxMana}");
        UpdateManaUI();
    }

    public void IncreaseManaRegenRate(float amount)
    {
        if (amount <= 0) return;
        manaRegenRate += amount;
        Debug.Log($"魔法值回复速率增加了 {amount}。新的魔法值回复速率: {manaRegenRate}/秒");
    }

    void UpdateManaUI()
    {
        if (manaFillImage != null)
        {
            manaFillImage.fillAmount = currentMana / maxMana;
        }
        if (manaText != null)
        {
            manaText.text = $"{Mathf.CeilToInt(currentMana)}/{Mathf.CeilToInt(maxMana)}";
        }
    }
}
