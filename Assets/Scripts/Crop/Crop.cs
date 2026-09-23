using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//管理农作物的生长和收获逻辑
public class Crop : MonoBehaviour
{
    public CropDetails cropDetails;
    public TileDetails tileDetails;

    [Header("果实预制体")]
    public GameObject fruitPrefab;

    [Header("果实生成随机偏移范围")]
    public Vector2 spawnOffsetRange = new Vector2(0.3f, 0.2f);
    [Header("果实生成高度偏移")]
    public float spawnHeightOffset = 0.1f;

    //收获农作物需要操作的次数
    private int harvestActionCount;

    private SpriteRenderer spriteRenderer;

    /// <summary>
    /// 判断农作物生长天数是否大于总天数
    /// </summary>
    public bool Canharvest
    {
        get
        {
            if (tileDetails == null || cropDetails == null)
                return false;
            return tileDetails.growthDays >= cropDetails.TotalGrowthDays;
        }
    }
    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    /// <summary>
    /// 根据当前生长天数更新作物
    /// </summary>
    /// <param name="currentTile"></param>
    public void UpdateGrowthVisual(TileDetails currentTile)
    {
        this.tileDetails = currentTile;
        if (cropDetails == null) return;
        //计算当前阶段
        int currentStage = CropManager.Instance.GetGrowthStage(currentTile, cropDetails);
        
        if (cropDetails.growthSprites != null && currentStage < cropDetails.growthSprites.Length)
        {
            Sprite stageSprite = cropDetails.growthSprites[currentStage];
            if (stageSprite != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = stageSprite;

            }
        }
    }



    /// <summary>
    /// 计算使用次数,达到次数生成果实
    /// </summary>
    /// <param name="tool">物品</param>
    /// <param name="tileDetails">格子信息</param>
    public void ProcessToolAction(Item tool ,TileDetails tileDetails)
    {
        this.tileDetails = tileDetails;

        //获取工具使用次数
        int ToolActionCount = cropDetails.GetTotalRequirCount(tool.itemID);
        
        //返回则说明工具不适合
        if (ToolActionCount==-1) 
            return;


        if (harvestActionCount < ToolActionCount)
        {
            harvestActionCount++;
        }
        //达到次数要求，执行收获
        if (harvestActionCount >= ToolActionCount)
        {
            //生成相应数量果实
            SpawnHarvestItem();
        }

    }

    /// <summary>
    /// 收获农作物，生成收获物
    /// </summary>
    public void SpawnHarvestItem()
    {
        if (cropDetails == null || tileDetails == null) return;

        for (int i = 0; i < cropDetails.producedItemID.Length; i++)
        {
            //果实数量
            int amountToProduce;
            int produceItemID= cropDetails.producedItemID[i];

            bool mutated = UnityEngine.Random.value < 0.03f;

            if (mutated)
            {
                EventHandler.CallFarmEventEvent("异变丰收", "这株作物发生了异变，收成翻了三倍！", false);
                CropManager.Instance.SpawnFruit(43, CalculateFruitSpawnPosition(0, 1));
            }
            if (cropDetails.producedMinAmount[i] == cropDetails.producedMaxAmount[i])
            {
                amountToProduce = cropDetails.producedMinAmount[i] + FarmEventEffects.harvestBonus;
            }
            else
            {
                amountToProduce = Random.Range(cropDetails.producedMinAmount[i], cropDetails.producedMaxAmount[i] + 1) + FarmEventEffects.harvestBonus;
            }

            if (mutated)
            {
                amountToProduce *= 3;
            }

            //生成相应数量指定物品
            for(int j=0;j<amountToProduce;j++)
            {
                //在世界地图上生成
                Vector3 spawnPos = CalculateFruitSpawnPosition(j, amountToProduce);

                // 生成可交互的果实对象
                SpawnInteractableFruit(produceItemID, spawnPos);

            }
        }
        //收获次数计数
        tileDetails.daysSinceLastHarvest++;

        if (cropDetails.daysToRegrow > 0 && tileDetails.daysSinceLastHarvest < cropDetails.regrowTimes)
        {
            tileDetails.growthDays = cropDetails.TotalGrowthDays - cropDetails.daysToRegrow;
            //刷新作物到再生阶段
            UpdateGrowthVisual(tileDetails);
            // 重置收获动作计数
            harvestActionCount = 0;
        }
        else
        {
            // 不可再生或再生次数已用完，清除作物数据
            tileDetails.daysSinceLastHarvest = -1;
            tileDetails.seedItemID = -1;
            tileDetails.growthDays = -1;

            // 从作物管理器中移除
            CropManager.Instance.RemoveCropFromDict(tileDetails);

            // 销毁作物游戏对象
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 计算果实生长位置
    /// </summary>
    /// <param name="index"></param>
    /// <param name="totalCount"></param>
    /// <returns></returns>
    public Vector3 CalculateFruitSpawnPosition(int index,int totalCount)
    {
        Vector3 basePos=transform.position;

        //根据索引计算角度，实现圆形分布
        float angle = (index / (float)totalCount) * Mathf.PI * 2f;
        float radius = 0.2f + (index * 0.05f);
        // 计算圆形分布位置
        float offsetX = Mathf.Cos(angle) * radius;
        float offsetY = Mathf.Sin(angle) * radius;

        offsetX += Random.Range(-spawnOffsetRange.x, spawnOffsetRange.x) * 0.5f;
        offsetY += Random.Range(-spawnOffsetRange.y, spawnOffsetRange.y) * 0.5f;

        offsetY += spawnHeightOffset;

        // 返回最终位置
        return basePos + new Vector3(offsetX, offsetY, 0);


    }

    /// <summary>
    /// 生成单个可交互的果实
    /// </summary>
    /// <param name="itemID">果实物品ID</param>
    /// <param name="spawnPos">生成位置</param>
    private void SpawnInteractableFruit(int itemID, Vector3 spawnPos)
    {
        FruitPickup.Spawn(fruitPrefab, itemID, spawnPos);

        
    }

}
