using Inventory;
using UnityEngine;

public static class ProgressManager
{
    private const string KeyDelivered = "CozyTown_Delivered";
    private const string KeyChop = "CozyTown_Chop";
    private const string KeyFish = "CozyTown_Fish";
    private const string KeyHarvest = "CozyTown_Harvest";
    private const string KeyAchievements = "CozyTown_Achievements";
    private const string KeySellBonus = "CozyTown_SellBonus";

    public static int Delivered
    {
        get { return PlayerPrefs.GetInt(KeyDelivered, 0); }
    }

    public static int Level
    {
        get { return 1 + Delivered / 5; }
    }

    public static float SellBonus
    {
        get { return PlayerPrefs.GetFloat(KeySellBonus, 0f); }
    }

    public static float GetSellMultiplier()
    {
        return 1f + SellBonus + Delivered * 0.002f;
    }

    public static void RegisterEvent(string title)
    {
        if (string.IsNullOrEmpty(title))
        {
            return;
        }

        if (title.Contains("交付") || title.Contains("委托"))
        {
            Add(KeyDelivered, 1);
            CheckLevel();
            return;
        }

        if (title.Contains("砍树"))
        {
            Add(KeyChop, 1);
            CheckAchievements();
            return;
        }

        if (title.Contains("钓鱼"))
        {
            Add(KeyFish, 1);
            CheckAchievements();
            return;
        }

        if (title.Contains("收获") || title.Contains("果园") || title.Contains("割草") || title.Contains("挖矿"))
        {
            Add(KeyHarvest, 1);
            CheckAchievements();
        }
    }

    public static void CheckAlmanac()
    {
        int count = AlmanacManager.DiscoveredCount;

        if (count >= 10 && !Has(1))
        {
            Unlock(1, "图鉴达人 I", "收集 10 种物品，卖价 +5%", 0.05f);
        }

        if (count >= 25 && !Has(2))
        {
            Unlock(2, "图鉴达人 II", "收集 25 种物品，卖价 +5%", 0.05f);
        }

        if (count >= 40 && !Has(4))
        {
            Unlock(4, "图鉴大师", "收集 40 种物品，卖价 +10%", 0.1f);
        }
    }

    private static void CheckAchievements()
    {
        if (Get(KeyChop) >= 30 && !Has(8))
        {
            Unlock(8, "伐木工", "砍倒 30 棵树，获得 500 金币", 0f);
            AddGold(500);
        }

        if (Get(KeyFish) >= 20 && !Has(16))
        {
            Unlock(16, "钓鱼好手", "钓上 20 条鱼，获得 800 金币", 0f);
            AddGold(800);
        }

        if (Get(KeyHarvest) >= 50 && !Has(32))
        {
            Unlock(32, "勤劳农夫", "收获 50 次，卖价 +5%", 0.05f);
        }
    }

    private static void CheckLevel()
    {
        int level = Level;

        if (level >= 3 && !Has(64))
        {
            Unlock(64, "农场 Lv3", "解锁：农场等级 3，卖价 +5%", 0.05f);
        }

        if (level >= 5 && !Has(128))
        {
            Unlock(128, "农场 Lv5", "解锁：农场等级 5，卖价 +5%", 0.05f);
        }

        if (level >= 8 && !Has(256))
        {
            Unlock(256, "农场 Lv8", "解锁：农场等级 8，卖价 +10%", 0.1f);
        }
    }

    private static void Unlock(int flag, string name, string description, float sellBonus)
    {
        PlayerPrefs.SetInt(KeyAchievements, Get(KeyAchievements) | flag);

        if (sellBonus > 0f)
        {
            PlayerPrefs.SetFloat(KeySellBonus, SellBonus + sellBonus);
        }

        PlayerPrefs.Save();

        EventHandler.CallFarmEventEvent("成就 · " + name, description, false);
    }

    private static bool Has(int flag)
    {
        return (Get(KeyAchievements) & flag) != 0;
    }

    private static int Get(string key)
    {
        return PlayerPrefs.GetInt(key, 0);
    }

    private static void Add(string key, int amount)
    {
        PlayerPrefs.SetInt(key, Get(key) + amount);
        PlayerPrefs.Save();
    }

    private static void AddGold(int amount)
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.PlayerMoney += amount;
            EventHandler.CallUpdateMoneyEvent(ShopManager.Instance.PlayerMoney);
        }
    }
}