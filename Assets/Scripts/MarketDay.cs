using UnityEngine;

public static class MarketDay
{
    private const int Period = 7;
    private static int lastAnnouncedDay = -1;

    public static bool IsMarketDay(int day)
    {
        return day > 0 && day % Period == 0;
    }

    public static bool IsTomorrowMarket(int day)
    {
        return (day + 1) % Period == 0;
    }

    public static float GetPriceMultiplier(int day)
    {
        return IsMarketDay(day) ? 1.5f : 1f;
    }

    public static void Announce(int day)
    {
        if (day == lastAnnouncedDay)
        {
            return;
        }

        lastAnnouncedDay = day;

        if (IsMarketDay(day))
        {
            EventHandler.CallFarmEventEvent("集市日", "今天是集市日！所有东西卖价 ×1.5，快去卖货", false);
        }
        else if (IsTomorrowMarket(day))
        {
            EventHandler.CallFarmEventEvent("集市", "明天是集市日，卖价 ×1.5，可以先囤着不急卖", false);
        }
    }
}