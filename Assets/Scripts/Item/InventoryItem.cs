using System;
using UnityEngine;

[Serializable] 
public class InventoryItem
{
    // 物品数据和数量
    public Item itemData;
    public int quantity;  

    // 上次使用时间
    private float lastUseTime;

    public InventoryItem(Item item, int amount)
    {
        itemData = item;
        quantity = amount;

        InitializeCooldown();
    }

    // 初始化冷却时间
    public void InitializeCooldown()
    {
        if (itemData != null)
        {
            lastUseTime = -itemData.cooldownTime;
        }
        else
        {
            lastUseTime = 0f;
        }
    }


    public float GetLastUseTime()
    {
        return lastUseTime;
    }


    /// <summary>
    /// 使用物品
    /// </summary>
    public void Use()
    {
        if (itemData != null)
        {
            if (IsOnCooldown())
            {
                return;
            }

            itemData.Use();
            lastUseTime = Time.time;
        }
    }

    /// <summary>
    /// 检查物品当前是否在冷却中。
    /// </summary>
    /// <returns>如果物品在冷却中，返回 true；否则返回 false。</returns>
    public bool IsOnCooldown()
    {
        if (itemData == null || itemData.cooldownTime <= 0)
        {
            return false;
        }
        bool onCooldown = Time.time < lastUseTime + itemData.cooldownTime;
        return onCooldown;
    }

    /// <summary>
    /// 获取物品剩余的冷却时间。
    /// </summary>
    /// <returns>剩余冷却时间（秒），如果没有冷却则返回0。</returns>
    public float GetRemainingCooldown()
    {
        if (itemData == null || itemData.cooldownTime <= 0)
        {
            return 0f;
        }
        float endTime = lastUseTime + itemData.cooldownTime;
        float remaining = endTime - Time.time;
        return Mathf.Max(0f, remaining);
    }
}
