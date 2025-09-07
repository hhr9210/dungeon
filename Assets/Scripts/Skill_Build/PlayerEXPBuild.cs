using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections;

public class PlayerEXPBuild : MonoBehaviour
{
    [Header("技能点UI引用")]
    [SerializeField] private TextMeshProUGUI skillPointsText;
    [SerializeField] private Button spendSkillPointButton;
    [SerializeField] private GameObject skillMenuPanel;

    [Header("技能升级配置")]
    [SerializeField] private float healthIncreasePerPoint = 10f;
    [SerializeField] private float resistanceIncreasePerPoint = 0.01f;
    [SerializeField] private float manaIncreasePerPoint = 15f;
    [SerializeField] private float manaRegenIncreasePerPoint = 0.5f;
    [SerializeField] private float attackDamageIncreasePerPoint = 2f;
    [SerializeField] private float attackSpeedIncreasePerPoint = 0.1f;
    [SerializeField] private float staminaIncreasePerPoint = 25f;
    [SerializeField] private int strengthIncreasePerPoint = 1;

    [Header("升级按钮")]
    [SerializeField] private Button upgradeHealthResistanceButton;
    [SerializeField] private Button upgradeManaButton;
    [SerializeField] private Button upgradeAttackButton;
    [SerializeField] private Button upgradeStaminaButton;
    [SerializeField] private Button upgradeStrengthButton;

    [Header("已分配点数UI (主显示)")]
    [SerializeField] private TextMeshProUGUI healthResistanceAllocatedText;
    [SerializeField] private TextMeshProUGUI manaAllocatedText;
    [SerializeField] private TextMeshProUGUI attackAllocatedText;
    [SerializeField] private TextMeshProUGUI staminaAllocatedText;
    [SerializeField] private TextMeshProUGUI strengthAllocatedText;

    [Header("已分配点数UI (副显示)")]
    [SerializeField] private TextMeshProUGUI healthResistanceAllocatedText2;
    [SerializeField] private TextMeshProUGUI manaAllocatedText2;
    [SerializeField] private TextMeshProUGUI attackAllocatedText2;
    [SerializeField] private TextMeshProUGUI staminaAllocatedText2;
    [SerializeField] private TextMeshProUGUI strengthAllocatedText2;

    private PlayerEXP playerEXP;
    private PlayerHealth playerHealth;
    private PlayerMana playerMana;
    private PlayerController playerController;
    private PlayerAttack playerAttack;

    private int allocatedHealthResistancePoints = 5;
    private int allocatedManaPoints = 2;
    private int allocatedAttackPoints = 4;
    private int allocatedStaminaPoints = 3;
    private int allocatedStrengthPoints = 4;

    void Awake()
    {
        playerEXP = FindObjectOfType<PlayerEXP>();
        if (playerEXP == null)
        {
            Debug.LogError("PlayerEXPBuild: 未在场景中找到PlayerEXP脚本。请确保它已附加到玩家对象。");
        }

        playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("PlayerEXPBuild: 未在场景中找到PlayerHealth脚本。请确保它已附加到玩家对象。");
        }

        playerMana = FindObjectOfType<PlayerMana>();
        if (playerMana == null)
        {
            Debug.LogError("PlayerEXPBuild: 未在场景中找到PlayerMana脚本。请确保它已附加到玩家对象。");
        }

        playerController = FindObjectOfType<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("PlayerEXPBuild: 未在场景中找到PlayerController脚本。请确保它已附加到玩家对象。");
        }

        playerAttack = FindObjectOfType<PlayerAttack>();
        if (playerAttack == null)
        {
            Debug.LogError("PlayerEXPBuild: 未在场景中找到PlayerAttack脚本。请确保它已附加到玩家对象。");
        }

        if (spendSkillPointButton != null)
        {
            spendSkillPointButton.onClick.AddListener(() =>
            {
                if (playerEXP != null)
                {
                    playerEXP.SpendSkillPoint("通用技能");
                }
            });
        }

        if (upgradeHealthResistanceButton != null)
        {
            upgradeHealthResistanceButton.onClick.AddListener(OnUpgradeHealthResistanceButtonClicked);
        }

        if (upgradeManaButton != null)
        {
            upgradeManaButton.onClick.AddListener(OnUpgradeManaButtonClicked);
        }

        if (upgradeAttackButton != null)
        {
            upgradeAttackButton.onClick.AddListener(OnUpgradeAttackButtonClicked);
        }

        if (upgradeStaminaButton != null)
        {
            upgradeStaminaButton.onClick.AddListener(OnUpgradeStaminaButtonClicked);
        }

        if (upgradeStrengthButton != null)
        {
            upgradeStrengthButton.onClick.AddListener(OnUpgradeStrengthButtonClicked);
        }

        if (skillMenuPanel != null)
        {
            skillMenuPanel.SetActive(false);
        }
    }

    void OnEnable()
    {
        if (playerEXP != null)
        {
            PlayerEXP.OnSkillPointsChanged += UpdateSkillPointsUI;
        }

        if (playerEXP != null)
        {
            UpdateSkillPointsUI(playerEXP.CurrentSkillPoints);
        }
        UpdateAllocatedPointsUI();
    }

    void OnDisable()
    {
        if (playerEXP != null)
        {
            PlayerEXP.OnSkillPointsChanged -= UpdateSkillPointsUI;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            ToggleSkillMenu();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (skillMenuPanel != null && skillMenuPanel.activeSelf)
            {
                ToggleSkillMenu();
            }
        }
    }

    private void ToggleSkillMenu()
    {
        if (skillMenuPanel != null)
        {
            bool isActive = !skillMenuPanel.activeSelf;
            skillMenuPanel.SetActive(isActive);
            Debug.Log($"技能菜单已{(isActive ? "显示" : "隐藏")}");

            if (isActive)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
        else
        {
            Debug.LogWarning("PlayerEXPBuild: 技能菜单面板 (skillMenuPanel) 未设置。无法切换显示。");
        }
    }

    private void OnUpgradeHealthResistanceButtonClicked()
    {
        if (playerEXP == null || playerHealth == null)
        {
            Debug.LogWarning("PlayerEXPBuild: PlayerEXP或PlayerHealth引用未设置，无法升级属性。");
            return;
        }

        if (playerEXP.SpendSkillPoint("生命/抗性升级"))
        {
            playerHealth.IncreaseMaxHealth(healthIncreasePerPoint);
            playerHealth.IncreaseResistance(resistanceIncreasePerPoint);
            allocatedHealthResistancePoints++;
            UpdateAllocatedPointsUI();
            Debug.Log($"成功消耗1点技能点。最大生命值增加{healthIncreasePerPoint}，抗性增加{resistanceIncreasePerPoint:P0}。");
        }
        else
        {
            Debug.Log("技能点不足，无法升级生命/抗性！");
        }
    }

    private void OnUpgradeManaButtonClicked()
    {
        if (playerEXP == null || playerMana == null)
        {
            Debug.LogWarning("PlayerEXPBuild: PlayerEXP或PlayerMana引用未设置，无法升级魔法属性。");
            return;
        }

        if (playerEXP.SpendSkillPoint("魔法升级"))
        {
            playerMana.IncreaseMaxMana(manaIncreasePerPoint);
            playerMana.IncreaseManaRegenRate(manaRegenIncreasePerPoint);
            allocatedManaPoints++;
            UpdateAllocatedPointsUI();
            Debug.Log($"成功消耗1点技能点。最大魔法值增加{manaIncreasePerPoint}，魔法恢复速度增加{manaRegenIncreasePerPoint}。");
        }
        else
        {
            Debug.Log("技能点不足，无法升级魔法！");
        }
    }

    private void OnUpgradeAttackButtonClicked()
    {
        if (playerEXP == null || playerAttack == null)
        {
            Debug.LogWarning("PlayerEXPBuild: PlayerEXP或PlayerAttack引用未设置，无法升级攻击属性。");
            return;
        }

        if (playerEXP.SpendSkillPoint("攻击升级"))
        {
            playerAttack.IncreaseAttackDamage(attackDamageIncreasePerPoint);
            playerAttack.IncreaseAttackSpeed(attackSpeedIncreasePerPoint);
            allocatedAttackPoints++;
            UpdateAllocatedPointsUI();
            Debug.Log($"成功消耗1点技能点。攻击伤害增加{attackDamageIncreasePerPoint}，攻击速度增加{attackSpeedIncreasePerPoint}。");
        }
        else
        {
            Debug.Log("技能点不足，无法升级攻击！");
        }
    }

    private void OnUpgradeStaminaButtonClicked()
    {
        if (playerEXP == null || playerController == null)
        {
            Debug.LogWarning("PlayerEXPBuild: PlayerEXP或PlayerController引用未设置，无法升级体力属性。");
            return;
        }

        if (playerEXP.SpendSkillPoint("体力升级"))
        {
            playerController.IncreaseMaxStamina(staminaIncreasePerPoint);
            allocatedStaminaPoints++;
            UpdateAllocatedPointsUI();
            Debug.Log($"成功消耗1点技能点。最大体力值增加{staminaIncreasePerPoint}。");
        }
        else
        {
            Debug.Log("技能点不足，无法升级体力！");
        }
    }

    private void OnUpgradeStrengthButtonClicked()
    {
        if (playerEXP == null || playerAttack == null)
        {
            Debug.LogWarning("PlayerEXPBuild: PlayerEXP或PlayerAttack引用未设置，无法升级力量属性。");
            return;
        }

        if (playerEXP.SpendSkillPoint("力量升级"))
        {
            playerAttack.IncreaseStrength(strengthIncreasePerPoint);
            allocatedStrengthPoints++;
            UpdateAllocatedPointsUI();
            Debug.Log($"成功消耗1点技能点。力量增加{strengthIncreasePerPoint}。");
        }
        else
        {
            Debug.Log("技能点不足，无法升级力量！");
        }
    }

    private void UpdateSkillPointsUI(int currentSkillPoints)
    {
        if (skillPointsText != null)
        {
            skillPointsText.text = $"SKP : {currentSkillPoints}";
        }
        else
        {
            Debug.LogWarning("PlayerEXPBuild: 技能点文本UI引用未设置。无法更新技能点显示。");
        }

        bool hasSkillPoints = currentSkillPoints > 0;

        if (spendSkillPointButton != null)
        {
            spendSkillPointButton.interactable = hasSkillPoints;
        }

        if (upgradeHealthResistanceButton != null)
        {
            upgradeHealthResistanceButton.interactable = hasSkillPoints;
        }

        if (upgradeManaButton != null)
        {
            upgradeManaButton.interactable = hasSkillPoints;
        }

        if (upgradeAttackButton != null)
        {
            upgradeAttackButton.interactable = hasSkillPoints;
        }

        if (upgradeStaminaButton != null)
        {
            upgradeStaminaButton.interactable = hasSkillPoints;
        }

        if (upgradeStrengthButton != null)
        {
            upgradeStrengthButton.interactable = hasSkillPoints;
        }
    }

    private void UpdateAllocatedPointsUI()
    {
        if (healthResistanceAllocatedText != null)
        {
            healthResistanceAllocatedText.text = $"{allocatedHealthResistancePoints}";
        }
        else
        {
            Debug.LogWarning("PlayerEXPBuild: 生命/抗性已分配文本UI引用 (主) 未设置。");
        }

        if (manaAllocatedText != null)
        {
            manaAllocatedText.text = $"{allocatedManaPoints}";
        }
        else
        {
            Debug.LogWarning("PlayerEXPBuild: 魔法已分配文本UI引用 (主) 未设置。");
        }

        if (attackAllocatedText != null)
        {
            attackAllocatedText.text = $"{allocatedAttackPoints}";
        }
        else
        {
            Debug.LogWarning("PlayerEXPBuild: 攻击已分配文本UI引用 (主) 未设置。");
        }

        if (staminaAllocatedText != null)
        {
            staminaAllocatedText.text = $"{allocatedStaminaPoints}";
        }
        else
        {
            Debug.LogWarning("PlayerEXPBuild: 体力已分配文本UI引用 (主) 未设置。");
        }

        if (strengthAllocatedText != null)
        {
            strengthAllocatedText.text = $"{allocatedStrengthPoints}";
        }
        else
        {
            Debug.LogWarning("PlayerEXPBuild: 力量已分配文本UI引用 (主) 未设置。");
        }

        if (healthResistanceAllocatedText2 != null)
        {
            healthResistanceAllocatedText2.text = $"{allocatedHealthResistancePoints}";
        }
        else
        {
            Debug.LogWarning("PlayerEXPBuild: 生命/抗性已分配文本UI引用 (副) 未设置。");
        }

        if (manaAllocatedText2 != null)
        {
            manaAllocatedText2.text = $"{allocatedManaPoints}";
        }
        else
        {
            Debug.LogWarning("PlayerEXPBuild: 魔法已分配文本UI引用 (副) 未设置。");
        }

        if (attackAllocatedText2 != null)
        {
            attackAllocatedText2.text = $"{allocatedAttackPoints}";
        }
        else
        {
            Debug.LogWarning("PlayerEXPBuild: 攻击已分配文本UI引用 (副) 未设置。");
        }

        if (staminaAllocatedText2 != null)
        {
            staminaAllocatedText2.text = $"{allocatedStaminaPoints}";
        }
        else
        {
            Debug.LogWarning("PlayerEXPBuild: 体力已分配文本UI引用 (副) 未设置。");
        }

        if (strengthAllocatedText2 != null)
        {
            strengthAllocatedText2.text = $"{allocatedStrengthPoints}";
        }
        else
        {
            Debug.LogWarning("PlayerEXPBuild: 力量已分配文本UI引用 (副) 未设置。");
        }
    }
}
