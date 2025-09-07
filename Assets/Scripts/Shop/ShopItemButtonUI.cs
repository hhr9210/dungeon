using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemButtonUI : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI stockText;
    public Button buyButton;

    public Shop.ShopItem shopItem; 

    public void SetShopItem(Shop.ShopItem item)
    {
        shopItem = item;
        if (shopItem != null && shopItem.itemData != null)
        {
            iconImage.sprite = shopItem.itemData.icon;
            iconImage.enabled = true;
            itemNameText.text = shopItem.itemData.itemName;
            
            priceText.text = $"{shopItem.itemData.price}";
            stockText.text = $"{shopItem.currentStock}";
            
            gameObject.SetActive(true); 
        }
        else
        {
            ClearButton();
        }
    }

    public void ClearButton()
    {
        shopItem = null;
        iconImage.sprite = null;
        iconImage.enabled = false;
        itemNameText.text = "";
        priceText.text = "";
        stockText.text = "";
        buyButton.interactable = false;
        gameObject.SetActive(false);
    }
}
