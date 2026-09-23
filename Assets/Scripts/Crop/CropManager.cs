using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 作物管理器 - 单例
/// 功能：管理作物种植、生长、季节判断、显示刷新
/// </summary>
public class CropManager : Singleton<CropManager>
{
    public CropData_SO cropData;
    private Transform cropParent;
    private Grid currentGrid;
    private Season currentSeason;

    /// <summary>
    /// 已种植作物的字典（键："x坐标x y坐标y"，值：作物组件）
    /// </summary>
    private Dictionary<string, Crop> plantedCrops = new Dictionary<string, Crop>();

    private void Start()
    {
        GameObject cropParentObj = GameObject.Find("CropParent");
        if (cropParentObj == null)
        {
            cropParentObj = new GameObject("CropParent");
        }
        cropParent = cropParentObj.transform;

        currentGrid = FindObjectOfType<Grid>();
    }

    private void OnEnable()
    {
        EventHandler.PlantSeedEvent += OnPlantSeedEvent;
        EventHandler.GameDayEvent += OnGameDayEvent;
    }

    private void OnDisable()
    {
        EventHandler.PlantSeedEvent -= OnPlantSeedEvent;
        EventHandler.GameDayEvent -= OnGameDayEvent;
    }

    /// <summary>
    /// 游戏天数变化事件
    /// </summary>
    /// <param name="day">当前天数</param>
    /// <param name="season">当前季节</param>
    private void OnGameDayEvent(int day, Season season)
    {
        currentSeason = season;
    }

    /// <summary>
    /// 在指定位置种植作物
    /// </summary>
    /// <param name="ID">种子物品ID</param>
    /// <param name="tileDetails">目标地块详情</param>
    private void OnPlantSeedEvent(int ID, TileDetails tileDetails)
    {
        CropDetails currentCrop = GetCropDetails(ID);
        if (currentCrop != null && (SeasonAvailable(currentCrop) || tileDetails.inGreenhouse) && tileDetails.seedItemID == -1)
        {
            tileDetails.seedItemID = ID;
            tileDetails.growthDays = 0;

            // 显示作物
            DisplayCropPlant(tileDetails, currentCrop);
        }
    }

    public void ClearAllCrops()
    {
        foreach (var pair in plantedCrops)
        {
            if (pair.Value != null)
            {
                Destroy(pair.Value.gameObject);
            }
        }

        plantedCrops.Clear();
    }

    /// <summary>
    /// 显示种植的作物
    /// </summary>
    /// <param name="tileDetails">目标地块详情</param>
    /// <param name="currentCrop">当前作物详情</param>
    private void DisplayCropPlant(TileDetails tileDetails, CropDetails currentCrop)
    {
        // 计算生长阶段
        int currentStage = GetGrowthStage(tileDetails, currentCrop);

        if (currentCrop.growthPrefabs == null || currentCrop.growthPrefabs.Length == 0)
        {
            return;
        }
        currentStage = Mathf.Clamp(currentStage, 0, currentCrop.growthPrefabs.Length - 1);

        GameObject cropPrefab = currentCrop.growthPrefabs[currentStage];
        Sprite cropSprite = null;

        if (currentCrop.growthSprites != null && currentStage < currentCrop.growthSprites.Length)
        {
            cropSprite = currentCrop.growthSprites[currentStage];
        }

        // 地块中心坐标（网格坐标转世界坐标）
        Vector3 pos = new Vector3(tileDetails.girdX + 0.5f, tileDetails.girdY + 0.5f, 0);

        // 实例化作物
        GameObject cropInstance = Instantiate(cropPrefab, pos, Quaternion.identity, cropParent);

        Crop cropComponent = cropInstance.GetComponent<Crop>();
        if (cropComponent == null)
        {
            cropComponent = cropInstance.AddComponent<Crop>();
        }

        cropComponent.cropDetails = currentCrop;
        cropComponent.tileDetails = tileDetails;

        // 设置SpriteRenderer显示当前阶段的精灵
        SpriteRenderer spriteRenderer = cropInstance.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null && cropSprite != null)
        {
            spriteRenderer.sprite = cropSprite;
        }

        // 记录到已种植字典
        string tileKey = $"{tileDetails.girdX}x{tileDetails.girdY}y";
        if (plantedCrops.ContainsKey(tileKey))
        {
            plantedCrops[tileKey] = cropComponent;
        }
        else
        {
            plantedCrops.Add(tileKey, cropComponent);
        }
    }

    /// <summary>
    /// 获取作物当前的生长阶段
    /// </summary>
    /// <param name="tileDetails">地块详情</param>
    /// <param name="currentCrop">作物详情</param>
    /// <returns>生长阶段索引（从0开始）</returns>
    public int GetGrowthStage(TileDetails tileDetails, CropDetails currentCrop)
    {
        if (tileDetails.growthDays <= 0) return 0;

        int dayCounter = 0;
        for (int i = 0; i < currentCrop.growthDays.Length; i++)
        {
            dayCounter += currentCrop.growthDays[i];
            // 如果当前生长天数小于累计天数，说明处于当前阶段
            if (tileDetails.growthDays < dayCounter)
                return i;
        }
        // 超过所有阶段天数，返回最后一个阶段（成熟阶段）
        return currentCrop.growthDays.Length - 1;
    }

    /// <summary>
    /// 刷新指定地块的作物显示
    /// </summary>
    /// <param name="tileDetails">目标地块详情</param>
    public void RefreshCropVisual(TileDetails tileDetails)
    {
        if (tileDetails.seedItemID == -1) return;

        CropDetails cropDetails = GetCropDetails(tileDetails.seedItemID);
        if (cropDetails == null) return;

        string tileKey = $"{tileDetails.girdX}x{tileDetails.girdY}y";

        if (plantedCrops.TryGetValue(tileKey, out Crop existingCrop) && existingCrop != null)
        {
            existingCrop.UpdateGrowthVisual(tileDetails);
            return;
        }

        plantedCrops.Remove(tileKey);
        DisplayCropPlant(tileDetails, cropDetails);
    }

    /// <summary>
    /// 重新显示指定地块的作物（用于加载存档后）
    /// </summary>
    /// <param name="tileDetails">目标地块详情</param>
    public void RedisplayCrop(TileDetails tileDetails)
    {
        RefreshCropVisual(tileDetails);
    }

    /// <summary>
    /// 从字典中移除指定地块的作物记录（收获/铲除时调用）
    /// </summary>
    /// <param name="tileDetails">目标地块详情</param>
    public void RemoveCropFromDict(TileDetails tileDetails)
    {
        string tileKey = $"{tileDetails.girdX}x{tileDetails.girdY}y";
        if (plantedCrops.ContainsKey(tileKey))
        {
            plantedCrops.Remove(tileKey);
        }
    }

    public bool IsCropMature(TileDetails tileDetails)
    {
        if (tileDetails == null || tileDetails.seedItemID == -1)
        {
            return false;
        }

        CropDetails details = GetCropDetails(tileDetails.seedItemID);

        if (details == null || details.growthDays == null || details.growthDays.Length == 0)
        {
            return false;
        }

        return GetGrowthStage(tileDetails, details) >= details.growthDays.Length - 1;
    }

    public void DestroyCrop(TileDetails tileDetails)
    {
        if (tileDetails == null)
        {
            return;
        }

        string tileKey = "{tileDetails.girdX}x{tileDetails.girdY}y";

        if (plantedCrops.TryGetValue(tileKey, out Crop planted) && planted != null)
        {
            plantedCrops.Remove(tileKey);
            Destroy(planted.gameObject);
        }

        tileDetails.seedItemID = -1;
        tileDetails.growthDays = -1;
        tileDetails.growthFraction = 0f;
    }

    public void ClearAllFruits()
    {
        foreach (var fruit in FindObjectsOfType<FruitPickup>())
        {
            fruit.ReleaseToPool();
        }
    }

    public void SpawnFruit(int itemID, Vector3 position)
    {
        GameObject prefab = GetFruitPrefab(itemID);

        if (prefab == null)
        {
            return;
        }

        FruitPickup.Spawn(prefab, itemID, position);
    }

    private GameObject GetFruitPrefab(int itemID)
    {
        if (cropData == null || cropData.cropDetailsList == null)
        {
            return null;
        }

        GameObject fallback = null;

        foreach (var details in cropData.cropDetailsList)
        {
            if (details.growthPrefabs == null)
            {
                continue;
            }

            for (int j = 0; j < details.growthPrefabs.Length; j++)
            {
                if (details.growthPrefabs[j] == null)
                {
                    continue;
                }

                Crop fruitOwner = details.growthPrefabs[j].GetComponent<Crop>();

                if (fruitOwner == null || fruitOwner.fruitPrefab == null)
                {
                    continue;
                }

                if (fallback == null)
                {
                    fallback = fruitOwner.fruitPrefab;
                }

                if (details.producedItemID == null)
                {
                    continue;
                }

                for (int i = 0; i < details.producedItemID.Length; i++)
                {
                    if (details.producedItemID[i] == itemID)
                    {
                        return fruitOwner.fruitPrefab;
                    }
                }
            }
        }

        return fallback;
    }

    public Season GetCurrentSeason()
    {
        return currentSeason;
    }

    public bool CanObtainProduct(int productItemID, Season season)
    {
        if (cropData == null || cropData.cropDetailsList == null)
        {
            return false;
        }

        bool producedByCrop = false;

        for (int c = 0; c < cropData.cropDetailsList.Count; c++)
        {
            CropDetails details = cropData.cropDetailsList[c];

            if (details.producedItemID == null)
            {
                continue;
            }

            for (int i = 0; i < details.producedItemID.Length; i++)
            {
                if (details.producedItemID[i] != productItemID)
                {
                    continue;
                }

                producedByCrop = true;

                if (details.season == null)
                {
                    continue;
                }

                for (int s = 0; s < details.season.Length; s++)
                {
                    if (details.season[s] == season)
                    {
                        return true;
                    }
                }
            }
        }

        return !producedByCrop;
    }

    /// <summary>
    /// 通过种子ID获取作物详情
    /// </summary>
    /// <param name="ID">种子物品ID</param>
    /// <returns>作物详情对象</returns>
    public CropDetails GetCropDetails(int ID)
    {
        if (cropData == null || cropData.cropDetailsList == null)
        {
            return null;
        }

        for (int i = 0; i < cropData.cropDetailsList.Count; i++)
        {
            if (cropData.cropDetailsList[i].seedItemID == ID)
            {
                return cropData.cropDetailsList[i];
            }
        }

        return null;
    }

    /// <summary>
    /// 判断当前季节是否适合该作物种植
    /// </summary>
    /// <param name="crop">作物详情</param>
    /// <returns>true表示适合种植</returns>
    private bool SeasonAvailable(CropDetails crop)
    {
        for (int i = 0; i < crop.season.Length; i++)
        {
            if (crop.season[i] == currentSeason)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 判断种子是否适合当前季节（供CursorManager调用）
    /// </summary>
    /// <param name="seedID">种子物品ID</param>
    /// <returns>true表示适合种植</returns>
    public bool IsSeedSeasonAvailable(int seedID)
    {
        CropDetails crop = GetCropDetails(seedID);
        if (crop == null) return false;

        foreach (var season in crop.season)
        {
            if (season == currentSeason)
                return true;
        }
        return false;
    }
    /// <summary>
    /// 直接设置当前季节，供读档时同步使用
    /// </summary>
    public void SetSeason(Season season)
    {
        currentSeason = season;
    }

}