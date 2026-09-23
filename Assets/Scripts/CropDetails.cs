using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CropDetails 
{
    public int seedItemID;
    [Header("不同天数所需要的天数")]
    public int[] growthDays;

    [Header("不同生长阶段物品的预制体")]
    public GameObject[] growthPrefabs;
    [Header("不同阶段的图片")]
    public Sprite[] growthSprites;
    [Header("可种植的季节")]
    public Season[] season;

    [Space]//使用 [Space] 特性在 Unity 编辑器中添加一个空行，用于分隔不同的属性组。
    [Header("收割工具")]
    public int[] harvestToolItemID;
    [Header("每种工具使用次数")]
    public int[] requireActionCount;

    [Space]
    [Header("收割果实信息")]
    public int[] producedItemID;
    public int[] producedMinAmount;
    public int[] producedMaxAmount;

    [Header("再次生长时间")]
    public int daysToRegrow;//再次生长所需的天数
    public int regrowTimes;//再生次数

    /// <summary>
    /// 农作物的总生长天数
    /// </summary>
    public int TotalGrowthDays
    {
        get
        {
            int amount = 0;
            foreach(var day in growthDays)
            {
                amount += day;
            }
            return amount;
        }
    }

    /// <summary>
    /// 检查当前工具是否可用
    /// </summary>
    /// <param name="toolID">当前工具ID</param>
    /// <returns></returns>
    public bool CheckToolAvailable(int toolID)
    {
        foreach(var tool in harvestToolItemID)
        {
            if (tool == toolID)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    ///工具需要使用的次数
    /// </summary>
    /// <param name="toolID"></param>
    /// <returns></returns>
    public int GetTotalRequirCount(int toolID)
    {
        for(int i = 0; i < harvestToolItemID.Length; i++)
        {
            if(harvestToolItemID[i] == toolID)
                return requireActionCount[i];
        }
        return -1;
    }
}
