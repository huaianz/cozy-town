using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 背包物品数据
/// </summary>
[System.Serializable]
public class SerializableInventoryItem
{
    public int itemID;      
    public int itemAmount;  

    public SerializableInventoryItem(int id, int amount)
    {
        itemID = id;
        itemAmount = amount;
    }

    /// <summary>
    /// 转换为InventoryItem结构体
    /// </summary>
    public InventoryItem ToInventoryItem()
    {
        return new InventoryItem(itemID, itemAmount);
    }
}

/// <summary>
/// 土地数据结构
/// </summary>
[System.Serializable]
public class SerializableTileData
{
    public int gridX;               
    public int gridY;              
    public bool canDig;            
    public int daysSinceDug;        
    public bool isWatered;          
    public int daysSinceWatered;    
    public int seedItemID;          
    public int growthDays;
    public float growthFraction;          
    public int daysSinceLastHarvest;
    public ObstacleType obstacleType; 
    public bool hasObstacle;
    public bool hasScarecrow;
    public bool inGreenhouse;
    public bool isUnlocked = true;       

    /// <summary>
    /// 从TileDetails创建可序列化数据
    /// </summary>
    public static SerializableTileData FromTileDetails(TileDetails tile)
    {
        return new SerializableTileData
        {
            gridX = tile.girdX,
            gridY = tile.girdY,
            canDig = tile.canDig,
            daysSinceDug = tile.daysSinceDug,
            isWatered = tile.isWatered,
            daysSinceWatered = tile.daysSinceWatered,
            seedItemID = tile.seedItemID,
            growthDays = tile.growthDays,
            growthFraction = tile.growthFraction,
            daysSinceLastHarvest = tile.daysSinceLastHarvest,
            obstacleType = tile.obstacleType,
            hasObstacle = tile.hasObstacle,
            hasScarecrow = tile.hasScarecrow,
            inGreenhouse = tile.inGreenhouse,
            isUnlocked = tile.isUnlocked
        };
    }

    /// <summary>
    /// 转换回TileDetails
    /// </summary>
    public TileDetails ToTileDetails()
    {
        return new TileDetails
        {
            girdX = gridX,
            girdY = gridY,
            canDig = canDig,
            daysSinceDug = daysSinceDug,
            isWatered = isWatered,
            daysSinceWatered = daysSinceWatered,
            seedItemID = seedItemID,
            growthDays = growthDays,
            growthFraction = growthFraction,
            daysSinceLastHarvest = daysSinceLastHarvest,
            obstacleType = obstacleType,
            hasObstacle = hasObstacle,
            hasScarecrow = hasScarecrow,
            inGreenhouse = inGreenhouse,
            isUnlocked = isUnlocked
        };
    }
}

/// <summary>
/// 主存档数据类
/// 存储游戏所有需要持久化的数据
/// </summary>
[System.Serializable]
public class SaveData
{
    [Header("玩家数据")]
    public int playerMoney;                     // 玩家金钱
    public int playerStamina;
    public List<SerializableInventoryItem> inventoryItems;  // 背包物品列表

    [Header("时间系统数据")]
    public int gameSecond;  
    public int gameMinute;  
    public int gameHour;    
    public int gameDay;     
    public int gameMonth;   
    public int gameYear;    
    public int gameSeason; 
    public int weatherState;
    public List<CommissionData> commissions;

    [Header("地图数据")]
    public List<SerializableTileData> tileDataList;  // 土地状态数据

    [Header("场景物品数据")]
    public List<SceneItem> sceneItems;

    [Header("存档元数据")]
    public string saveTime;    // 存档时间
    public int saveVersion;    // 存档版本号

    /// <summary>
    /// 初始化默认值
    /// </summary>
    public SaveData()
    {
        inventoryItems = new List<SerializableInventoryItem>();
        tileDataList = new List<SerializableTileData>();
        sceneItems = new List<SceneItem>();
        commissions = new List<CommissionData>();
        saveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        saveVersion = 2;
    }
}