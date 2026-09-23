using Inventory;
using UnityEngine;

public static class ToolDurability
{
    private const string KeyPrefix = "CozyTown_Tool_";
    private const int RepairOreCost = 2;
    private const int RepairGoldPerLevel = 30;

    public static bool IsTool(ItemType type)
    {
        return type == ItemType.HoeTool
            || type == ItemType.WaterCanTool
            || type == ItemType.SickleTool
            || type == ItemType.axeTool
            || type == ItemType.pickaxeTool
            || type == ItemType.FishingRod;
    }

    public static int GetMax(ItemType type)
    {
        int baseMax;

        if (type == ItemType.HoeTool) baseMax = 100;
        else if (type == ItemType.WaterCanTool) baseMax = 120;
        else if (type == ItemType.SickleTool) baseMax = 150;
        else if (type == ItemType.axeTool) baseMax = 120;
        else if (type == ItemType.pickaxeTool) baseMax = 80;
        else if (type == ItemType.FishingRod) baseMax = 100;
        else baseMax = 100;

        return baseMax + (GetLevel(type) - 1) * 40;
    }

    public static int GetUpgradeOreCost(ItemType type)
    {
        int level = GetLevel(type);

        if (level == 1) return 15;
        if (level == 2) return 40;

        return 0;
    }

    public static int GetUpgradeGoldCost(ItemType type)
    {
        int level = GetLevel(type);

        if (level == 1) return 500;
        if (level == 2) return 2000;

        return 0;
    }

    public static int GetUpgradeGemCost(ItemType type)
    {
        return GetLevel(type) == 2 ? 2 : 0;
    }

    public static string GetLevelEffect(ItemType type, int level)
    {
        if (type == ItemType.HoeTool)
        {
            if (level == 1) return "1 格";
            if (level == 2) return "3 格";
            return "5 格";
        }

        if (type == ItemType.WaterCanTool)
        {
            if (level == 1) return "1 格";
            if (level == 2) return "3 格";
            return "5 格";
        }

        if (type == ItemType.SickleTool)
        {
            if (level == 1) return "1 格";
            if (level == 2) return "周围一圈";
            return "3x3";
        }

        if (type == ItemType.pickaxeTool)
        {
            return level + " 份";
        }

        if (type == ItemType.axeTool)
        {
            return level + " 份木材";
        }

        if (type == ItemType.FishingRod)
        {
            if (level == 1) return "咬钩 2.5 秒";
            if (level == 2) return "咬钩 3.5 秒";
            return "咬钩 3.5 秒 + 稀有鱼";
        }

        return "-";
    }

    public static bool CanUpgrade(ItemType type)
    {
        if (!IsTool(type) || GetLevel(type) >= 3)
        {
            return false;
        }

        if (InventoryManager.Instance == null)
        {
            return false;
        }

        int ore = GetUpgradeOreCost(type);
        int gem = GetUpgradeGemCost(type);
        int gold = GetUpgradeGoldCost(type);

        if (InventoryManager.Instance.GetItemAmountInBag(37) < ore)
        {
            return false;
        }

        if (gem > 0 && InventoryManager.Instance.GetItemAmountInBag(38) < gem)
        {
            return false;
        }

        return ShopManager.Instance != null && ShopManager.Instance.PlayerMoney >= gold;
    }

    public static bool TryUpgrade(ItemType type)
    {
        if (!CanUpgrade(type))
        {
            return false;
        }

        InventoryManager.Instance.RemoveItem(37, GetUpgradeOreCost(type));

        if (GetUpgradeGemCost(type) > 0)
        {
            InventoryManager.Instance.RemoveItem(38, GetUpgradeGemCost(type));
        }

        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.PlayerMoney -= GetUpgradeGoldCost(type);
            EventHandler.CallUpdateMoneyEvent(ShopManager.Instance.PlayerMoney);
        }

        SetLevel(type, GetLevel(type) + 1);

        EventHandler.CallFarmEventEvent("工坊", ToolName(type) + "升级到 " + GetLevel(type) + " 级：" + GetLevelEffect(type, GetLevel(type)), false);

        return true;
    }

    public static int GetLevel(ItemType type)
    {
        return PlayerPrefs.GetInt(KeyPrefix + "Lv_" + (int)type, 1);
    }

    public static int GetCurrent(ItemType type)
    {
        int value = PlayerPrefs.GetInt(KeyPrefix + (int)type, GetMax(type));

        return Mathf.Clamp(value, 0, GetMax(type));
    }

    public static bool IsDull(ItemType type)
    {
        return IsTool(type) && GetCurrent(type) <= 0;
    }

    public static void Use(ItemType type)
    {
        if (!IsTool(type))
        {
            return;
        }

        int current = GetCurrent(type);

        if (current <= 0)
        {
            return;
        }

        current--;

        PlayerPrefs.SetInt(KeyPrefix + (int)type, current);
        PlayerPrefs.Save();

        if (current == 40 || current == 10)
        {
            EventHandler.CallFarmEventEvent("工具", ToolName(type) + "耐久只剩 " + current + " 了，快去修一修", true);
        }
    }

    public static void SetLevel(ItemType type, int level)
    {
        PlayerPrefs.SetInt(KeyPrefix + "Lv_" + (int)type, level);
        PlayerPrefs.SetInt(KeyPrefix + (int)type, GetMax(type));
        PlayerPrefs.Save();
    }

    public static bool NeedsRepair(ItemType type)
    {
        return IsTool(type) && GetCurrent(type) < GetMax(type);
    }

    public static int GetRepairOreCost()
    {
        return RepairOreCost;
    }

    public static int GetRepairGoldCost(ItemType type)
    {
        return RepairGoldPerLevel * GetLevel(type);
    }

    public static void RepairFully(ItemType type)
    {
        if (!IsTool(type))
        {
            return;
        }

        PlayerPrefs.SetInt(KeyPrefix + (int)type, GetMax(type));
        PlayerPrefs.Save();
    }

    public static bool TryAutoRepair(ItemType type)
    {
        if (!IsDull(type))
        {
            return true;
        }

        int goldCost = GetRepairGoldCost(type);
        bool hasOre = InventoryManager.Instance != null && InventoryManager.Instance.GetItemAmountInBag(37) >= RepairOreCost;
        bool hasGold = ShopManager.Instance != null && ShopManager.Instance.PlayerMoney >= goldCost;

        if (!hasOre || !hasGold)
        {
            EventHandler.CallFarmEventEvent("工具", ToolName(type) + "已经钝了（矿石×" + RepairOreCost + " + " + goldCost + " 金币可修），现在效率下降", true);
            return false;
        }

        InventoryManager.Instance.RemoveItem(37, RepairOreCost);

        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.PlayerMoney -= goldCost;
            EventHandler.CallUpdateMoneyEvent(ShopManager.Instance.PlayerMoney);
        }

        RepairFully(type);

        EventHandler.CallFarmEventEvent("工具", "花矿石×" + RepairOreCost + " 和 " + goldCost + " 金币修好了" + ToolName(type), false);

        return true;
    }

    public static string ToolName(ItemType type)
    {
        if (type == ItemType.HoeTool) return "锄头";
        if (type == ItemType.WaterCanTool) return "水壶";
        if (type == ItemType.SickleTool) return "镰刀";
        if (type == ItemType.axeTool) return "斧头";
        if (type == ItemType.pickaxeTool) return "镐子";
        if (type == ItemType.FishingRod) return "鱼竿";

        return "工具";
    }
}