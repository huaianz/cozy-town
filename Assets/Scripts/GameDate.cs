using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//物品的基本信息
[System.Serializable]//序列化
public class Item
{
    public int itemID;
    public string itemName;
    public ItemType itemType;
    public string itemDescription;

    public Sprite itemIcon;
    public Sprite itemOnWorldSprite;

    public int itemPrice;
    [Range(0, 1)]
    public float sellPercentage;
    public int maxStack;

    public bool isDraggable;
    public bool isUseable;
}

//背包
[System.Serializable]
public struct InventoryItem
{
    public int itemID;
    public int itemAmount;


    public InventoryItem(int id, int amount)
    {
        itemID = id;
        itemAmount = amount;
    }
}

//商品
[System.Serializable]
public struct ShopGoods
{
    public int itemId;          // 物品ID
    public int buyPrice;        // 购买单价
    public ShopCategory cate;   // 所属分类
    public bool isOnSale;       // 是否上架售卖
}

[System.Serializable]
public class CommissionData
{
    public int itemID;
    public int amount;
    public int reward;
    public bool delivered;
    public int daysLeft = 3;
}

//unity自带的vector3不能被序列化，这个作为替身类，能存 x/y/z，能序列化，最后还能转回去
[System.Serializable]
public class SerializableVector3
{
    public float x, y, z;

    public SerializableVector3(Vector3 pos)
    {
        this.x = pos.x;
        this.y = pos.y;
        this.z = pos.z;
    }
    //将传入的pos的三个方向坐标赋值给x,y,z

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
    //将SerializableVector3 的数据转换回 Vector3 类型

    public Vector2Int ToVector2Int()
    {
        return new Vector2Int((int)x, (int)y);
    }
    //将 SerializableVector3 的 x 和 y 分量转换为 Vector2Int 类型
}

//储存场景中的物品信息
[System.Serializable]
public class SceneItem 
{
    public int itemID;
    public SerializableVector3 position; //物品在场景中的位置坐标
}


//储存格子的属性信息
[System.Serializable]
public class TileProperty
{
    public Vector2Int tileCoordinate;//储存格子的坐标
    public GridType gridType;//表示格子类型
    public bool isUnlocked;    // 是否解锁

    public int unlockPrice;    //解锁该土地所需的金币价格
    public ObstacleType obstacleType;//瓦片上的障碍物类型
    public bool hasObstacle;  //是否有障碍物;
    public bool isWatered;//瓦片是否已浇水
    public int cropID; //瓦片上种植的作物ID(如果是-1，就是无农作物)
    public int growthStage;//作物生长阶段
    public int growthDays;//作物生长天数

}



[System.Serializable]
public class TileDetails //定义一个类，储存格子的详细信息
{
    public int girdX, girdY;//格子的坐标
    public bool canDig;//是否可以被挖坑
    public int daysSinceDug = -1;//挖坑时间
    public bool isWatered = false;//当前是否处于已浇水状态
    public int daysSinceWatered = -1;//浇水时间
    public int seedItemID = -1;//保存种子的信息
    public int growthDays = -1;//种子成长了多少天
    public float growthFraction = 0f;
    public int daysSinceLastHarvest = -1;//储存格子自上次收获以来的天数

    public ObstacleType obstacleType = ObstacleType.None;
    public bool hasObstacle = false;
    public bool hasScarecrow = false;
    public bool inGreenhouse = false;
    public bool isOreNode = false;
    public bool isPondWater = false;
    public bool isUnlocked = true;
}

