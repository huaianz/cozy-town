using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EventHandler
{
    /// <summary>
    /// 事件系统事件
    /// </summary>
    public static event Action<int, int> GameMinuteEvent;
    //通知订阅者游戏时间的小时，分钟变化
    public static void CallGameMinuteEvent(int minute, int hour)
    {
        GameMinuteEvent?.Invoke(minute, hour);
        //传递分钟和小时
    }

    public static event Action<int, Season> GameDayEvent;
    //通知订阅者游戏时间的天数，季节变化
    public static void CallGameDayEvent(int day, Season season)
    {
        GameDayEvent?.Invoke(day, season);
        //传递传递当前天数和季节
    }

    public static event Action<int, int, int, int, Season> GameDateEvent;
    //通知订阅者游戏时间的整体变化
    public static void CallGameDateEvent(int hour, int day, int month, int year, Season season)
    {
        GameDateEvent?.Invoke(hour, day, month, year, season);
        //传递当前小时，天，月，年和季节
    }


    /// <summary>
    /// 背包系统事件
    /// </summary>


    /// <summary>
    /// 物品是否被选择
    /// </summary>
    public static event Action<Item, bool> ItemSelectEvent;
    public static void CallItemSelectedEvent(Item item,bool isSelected)
    {
        ItemSelectEvent?.Invoke(item, isSelected);
    }

    /// <summary>
    /// 用来订阅一些背包的更新
    /// </summary>
    public static event Action<InventoryLocation, List<InventoryItem>> UpdateInventoryUI;
    public static void CallUpdaeInventoryUI(InventoryLocation inventoryLocation,List<InventoryItem> inventoryItem)
    {
        UpdateInventoryUI?.Invoke(inventoryLocation, inventoryItem);
    }


    /// <summary>
    /// 传递鼠标点击的物品和位置
    /// </summary>
    public static event Action<Vector3, Item> MouseClickedEvent;
    public static void CallMouseClickedEvent(Vector3 pos,Item item)
    {
        MouseClickedEvent?.Invoke(pos, item);
    }



    /// <summary>
    /// 金币更新事件
    /// </summary>
    public static event Action<int> UpdateMoneyEvent;
    public static void CallUpdateMoneyEvent(int money)
    {
        UpdateMoneyEvent?.Invoke(money);
    }

    public static event Action<int, int> StaminaChangedEvent;
    public static void CallStaminaChangedEvent(int current, int max)
    {
        StaminaChangedEvent?.Invoke(current, max);
    }

    public static event Action<WeatherType> WeatherChangedEvent;
    public static void CallWeatherChangedEvent(WeatherType weather)
    {
        WeatherChangedEvent?.Invoke(weather);
    }

    public static event Action CommissionChangedEvent;
    public static void CallCommissionChangedEvent()
    {
        CommissionChangedEvent?.Invoke();
    }

    /// <summary>
    /// 刷新商店UI事件
    /// </summary>
    public static event Action RefreshShopUIEvent;
    public static void CallRefreshShopUIEvent()
    {
        RefreshShopUIEvent?.Invoke();
    }


    /// <summary>
    /// 工具执行的动画
    /// </summary>
    public static event Action<Vector3, Item> ExecuteActionAfterAnimation;
    public static void CallExecuteActionAfterAnimation(Vector3 pos,Item item)
    {
        ExecuteActionAfterAnimation?.Invoke(pos, item);
    }

    /// <summary>
    /// 通知播种种子
    /// </summary>
    public static event Action<int, TileDetails> PlantSeedEvent;
    public static void CallPlantSeedEvent(int ID,TileDetails tileDetails)
    {
        PlantSeedEvent?. Invoke(ID, tileDetails);
    }

    //刷新地图
    public static event Action RefreshCurrentMap;
    public static void CallRefreshCurrentMap()
    {
        RefreshCurrentMap?.Invoke();
    }
    /// <summary>
    /// 农田随机事件
    /// </summary>
    public static event Action<string, string, bool> FarmEventEvent;
    public static void CallFarmEventEvent(string title, string description, bool disaster)
    {
        FarmEventEvent?.Invoke(title, description, disaster);
    }

    /// <summary>
    /// 主线完成：播放归乡宴结局过场（仅在最后一期还清时触发一次）
    /// </summary>
    public static event Action StoryEndingEvent;
    public static void CallStoryEndingEvent()
    {
        StoryEndingEvent?.Invoke();
    }
}
