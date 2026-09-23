

/// <summary>
/// 季节类型
/// </summary>
public enum Season//表示游戏中的季节
{
    春天, 夏天, 秋天, 冬天
}

public enum ItemType
{
    Seed,       // 种子
    axeTool,        // 斧头
    HoeTool,        // 锄头耕地
    WaterCanTool,   // 水壶浇水
    SickleTool,     // 镰刀收获
    pickaxeTool,    // 稿子
    Product,   // 商品
    Deed,
    Scarecrow,
    Greenhouse,
    FishingRod
}



/// <summary>
/// 瓦片地块类型
/// </summary>
public enum GridType
{
    EmptyLand,      // 空地 未开垦
    Tillable,       //可开垦
    TilledLand,     // 已耕地
    WateredLand,    // 已浇水
    Obstacle,        // 障碍物/未解锁禁地
}

/// <summary>
/// 障碍物类型
/// </summary>
public enum ObstacleType
{
    None,
    Tree,
    Rock,
    Weed,
}


public enum ShopCategory
{
    All,        // 全部
    Seed,       // 种子
    Tool,       // 农具
}

/// <summary>
/// 商店大标签类型
/// </summary>
public enum ShopTabType
{
    Buy,  // 购买区
    Sell  // 回收区
}

public enum SlotType//分类存储物品的类型
{
    Bag, Box, Shop
}

//物品储存位置
public enum InventoryLocation
{
    Player,Box
}

/// <summary>
/// 昼夜状态
/// </summary>
public enum DayNightState
{
    Dawn,       // 黎明
    Day,        // 白天
    Dusk,       // 黄昏
    Night       // 夜晚
}

public enum WeatherType
{
    Sunny,
    Cloudy,
    Rainy,
    LightRain,
    HeavyRain,
    Thunderstorm,
    Mist,
    DenseFog,
    LightSnow,
    HeavySnow,
    Overcast
}

public enum SfxType
{
    Dig,
    Water,
    Plant,
    Harvest,
    Chop,
    Mine,
    Pickup,
    Buy,
    Sell,
    Deliver,
    Sleep
}