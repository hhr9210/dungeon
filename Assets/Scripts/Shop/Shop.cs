using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class Shop : MonoBehaviour
{
    [System.Serializable]
    public class ShopItem
    {
        public Item itemData;
        public int initialStock;
        [HideInInspector] public int currentStock;
    }

    [Header("商店设置")]
    public List<ShopItem> availableItems = new List<ShopItem>();

    [Header("UI 引用")]
    public GameObject shopPanel;
    public Transform itemButtonsParent;
    public GameObject shopItemButtonPrefab;

    private Inventory playerInventory;
    private List<ShopItemButtonUI> currentShopItemButtons = new List<ShopItemButtonUI>();

    void Awake()
    {
        playerInventory = FindObjectOfType<Inventory>();
        if (playerInventory == null)
        {
            Debug.LogError("Shop: 未找到 Inventory 脚本！请确保玩家GameObject上挂载了Inventory脚本。", this);
            enabled = false;
            return;
        }

        foreach (var shopItem in availableItems)
        {
            shopItem.currentStock = shopItem.initialStock;
        }

        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }

        InitializeShopButtons();
    }

    void OnEnable()
    {
        Inventory.OnMoneyChanged += UpdateShopUI;
        Inventory.OnInventoryContentChanged += UpdateShopUI;

        if (PauseManager.Instance != null)
        {
            PauseManager.PauseStateChanged += OnGamePauseStateChanged;
        }
        else
        {
            Debug.LogError("Shop: 未找到 PauseManager 实例，商店功能可能无法正常与暂停系统交互。", this);
        }
    }

    void OnDisable()
    {
        Inventory.OnMoneyChanged -= UpdateShopUI;
        Inventory.OnInventoryContentChanged -= UpdateShopUI;

        if (PauseManager.Instance != null)
        {
            PauseManager.PauseStateChanged -= OnGamePauseStateChanged;
        }
    }

    void Update()
    {

    }

    private void OnGamePauseStateChanged(bool isPaused)
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(isPaused);
            if (isPaused)
            {
                UpdateShopUI();
            }
        }
    }

    private void InitializeShopButtons()
    {
        if (itemButtonsParent == null || shopItemButtonPrefab == null)
        {
            Debug.LogError("Shop: 商品按钮父对象或预制体未赋值！", this);
            return;
        }

        foreach (Transform child in itemButtonsParent)
        {
            Destroy(child.gameObject);
        }
        currentShopItemButtons.Clear();

        foreach (var shopItem in availableItems)
        {
            GameObject buttonObj = Instantiate(shopItemButtonPrefab, itemButtonsParent);
            ShopItemButtonUI buttonUI = buttonObj.GetComponent<ShopItemButtonUI>();

            if (buttonUI != null)
            {
                buttonUI.SetShopItem(shopItem);
                buttonUI.buyButton.onClick.AddListener(() => TryBuyItem(shopItem));
                currentShopItemButtons.Add(buttonUI);
            }
            else
            {
                Debug.LogError($"Shop: 商品按钮预制体 '{shopItemButtonPrefab.name}' 未找到 ShopItemButtonUI 组件！", shopItemButtonPrefab);
            }
        }
        UpdateShopUI();
    }

    public void TryBuyItem(ShopItem shopItem)
    {
        if (shopItem == null || shopItem.itemData == null)
        {
            Debug.LogWarning("Shop: 尝试购买空物品。", this);
            return;
        }

        if (shopItem.currentStock <= 0)
        {
            Debug.Log($"Shop: {shopItem.itemData.itemName} 库存不足！");
            return;
        }

        if (playerInventory.CanAfford(shopItem.itemData.price))
        {
            if (playerInventory.AddItem(shopItem.itemData, 1))
            {
                playerInventory.SpendMoney(shopItem.itemData.price);
                shopItem.currentStock--;
                Debug.Log($"Shop: 成功购买 {shopItem.itemData.itemName}，花费 {shopItem.itemData.price}。");
                UpdateShopUI();
            }
            else
            {
                Debug.Log($"Shop: 背包已满，无法购买 {shopItem.itemData.itemName}。");
            }
        }
        else
        {
            Debug.Log($"Shop: 金钱不足，无法购买 {shopItem.itemData.itemName}。需要 {shopItem.itemData.price}，你有 {playerInventory.currentMoney}。");
        }
    }

    public void UpdateShopUI(float newMoneyAmount = 0f)
    {
        UpdateShopUI();
    }

    public void UpdateShopUI()
    {
        foreach (var buttonUI in currentShopItemButtons)
        {
            if (buttonUI.shopItem != null && buttonUI.shopItem.itemData != null)
            {
                buttonUI.itemNameText.text = buttonUI.shopItem.itemData.itemName;
                buttonUI.priceText.text = $"{buttonUI.shopItem.itemData.price:F0}";
                buttonUI.stockText.text = $"{buttonUI.shopItem.currentStock}";
                buttonUI.iconImage.sprite = buttonUI.shopItem.itemData.icon;
                buttonUI.iconImage.enabled = true;

                bool canAfford = playerInventory.CanAfford(buttonUI.shopItem.itemData.price);
                bool hasStock = buttonUI.shopItem.currentStock > 0;
                bool hasInventorySpace = playerInventory.HasFreeSpace();

                buttonUI.buyButton.interactable = canAfford && hasStock && hasInventorySpace;
            }
            else
            {
                buttonUI.ClearButton();
            }
        }
    }

    public void ToggleShopUI()
    {
        if (shopPanel != null && PauseManager.Instance != null)
        {
            bool isActive = !shopPanel.activeSelf;
            shopPanel.SetActive(isActive);

            if (isActive)
            {
                PauseManager.Instance.SetGamePaused(true);
                UpdateShopUI();
            }
            else
            {
                PauseManager.Instance.SetGamePaused(false);
            }
        }
    }
}
