using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class Pickup : MonoBehaviour
{
    // 拾取物体状态
    private enum PickupState
    {
        Idle,
        PromptDisplayed,
        DescriptionDisplayed
    }

    [Header("拾取物品数据")]
    public Item itemData;
    public int quantity = 1;

    [Header("UI 设置 - 拾取前")]
    public GameObject pressEPromptPanel;
    public TextMeshProUGUI pressEPromptText;

    [Header("UI 设置 - 拾取后")]
    public GameObject pickupNotificationPanel;
    public Image notificationIcon;
    public TextMeshProUGUI notificationQuantityText;
    public TextMeshProUGUI notificationDescriptionText;

    private Inventory playerInventory;
    private PickupState currentState = PickupState.Idle;

    void Start()
    {
        playerInventory = FindObjectOfType<Inventory>();
        if (playerInventory == null)
        {
            Debug.LogError("Pickup: 未找到 Inventory 脚本！", this);
            enabled = false;
            return;
        }

        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning($"Pickup: 物品 {gameObject.name} 没有 Collider 组件！", this);
            enabled = false;
            return;
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning($"Pickup: 物品 {gameObject.name} 的 Collider 并非触发器，请设置为 Is Trigger。", this);
        }

        if (pressEPromptPanel != null)
        {
            pressEPromptPanel.SetActive(false);
        }

        if (pickupNotificationPanel != null)
        {
            pickupNotificationPanel.SetActive(false);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            HandleFKeyPress();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (currentState == PickupState.Idle)
            {
                ShowPressEPromptUI();
                currentState = PickupState.PromptDisplayed;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ResetPickupState();
        }
    }

    private void HandleFKeyPress()
    {
        switch (currentState)
        {
            case PickupState.PromptDisplayed:
                HidePressEPromptUI();
                if (TryAddItemToInventory())
                {
                    ShowPickupNotificationUI();
                    currentState = PickupState.DescriptionDisplayed;
                }
                else
                {
                    ResetPickupState();
                }
                break;

            case PickupState.DescriptionDisplayed:
                HidePickupNotificationUI();
                Destroy(gameObject);
                break;

            case PickupState.Idle:
                break;
        }
    }

    private void ShowPressEPromptUI()
    {
        if (pressEPromptPanel != null)
        {
            pressEPromptPanel.SetActive(true);
            if (pressEPromptText != null && itemData != null)
            {
                pressEPromptText.text = $"PRESS F TO PICK UP";
            }
        }
    }

    private void HidePressEPromptUI()
    {
        if (pressEPromptPanel != null)
        {
            pressEPromptPanel.SetActive(false);
        }
    }

    private bool TryAddItemToInventory()
    {
        if (playerInventory != null && itemData != null)
        {
            return playerInventory.AddItem(itemData, quantity);
        }
        return false;
    }

    private void ShowPickupNotificationUI()
    {
        if (pickupNotificationPanel != null && itemData != null)
        {
            pickupNotificationPanel.SetActive(true);

            if (notificationIcon != null)
            {
                if (itemData.icon != null)
                {
                    notificationIcon.sprite = itemData.icon;
                    notificationIcon.enabled = true;
                }
                else
                {
                    notificationIcon.enabled = false;
                }
            }

            if (notificationQuantityText != null)
            {
                notificationQuantityText.text = $"{quantity}";
            }

            if (notificationDescriptionText != null)
            {
                notificationDescriptionText.text = itemData.description;
            }
        }
    }

    private void HidePickupNotificationUI()
    {
        if (pickupNotificationPanel != null)
        {
            pickupNotificationPanel.SetActive(false);
        }
    }

    private void ResetPickupState()
    {
        HidePressEPromptUI();
        HidePickupNotificationUI();
        currentState = PickupState.Idle;
    }
}
