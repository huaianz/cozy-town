using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    [Header("单个特效预制体配置 - 在Inspector中拖拽赋值")]
    [Tooltip("锄头挖掘特效 - 在点击位置播放挖坑动画")]
    public GameObject hoeEffectPrefab;      // 数组索引: 0

    [Tooltip("浇水特效 - 在点击位置播放浇水动画")]
    public GameObject waterEffectPrefab;    // 数组索引: 1

    [Tooltip("斧头砍树特效 - 在点击位置播放砍树动画")]
    public GameObject axeEffectPrefab;      // 数组索引: 2

    [Tooltip("镐子挖石头特效 - 在点击位置播放挖石头动画")]
    public GameObject pickaxeEffectPrefab;  // 数组索引: 3

    [Tooltip("镰刀收割特效 - 在点击位置播放收割动画")]
    public GameObject sickleEffectPrefab;   // 数组索引: 4


    private bool isPlayingEffect;

    [Header("体力")]
    [SerializeField] private int maxStamina = 100;
    [SerializeField] private int staminaCostHoe = 2;
    [SerializeField] private int staminaCostWater = 1;
    [SerializeField] private int staminaCostSeed = 1;
    [SerializeField] private int staminaCostHarvest = 2;
    [SerializeField] private int staminaCostAxe = 3;
    [SerializeField] private int staminaCostPickaxe = 3;
    [SerializeField] private int staminaCostDeed = 1;

    private int currentStamina;

    private void OnEnable()
    {
        EventHandler.MouseClickedEvent += OnMouseClicked;
        EventHandler.GameDayEvent += OnGameDayEvent;
    }


    private void OnDisable()
    {
        EventHandler.MouseClickedEvent -= OnMouseClicked;
        EventHandler.GameDayEvent -= OnGameDayEvent;
    }

    private void Start()
    {
        currentStamina = maxStamina;
        EventHandler.CallStaminaChangedEvent(currentStamina, maxStamina);
    }

    private void OnGameDayEvent(int day, Season season)
    {
        RestoreStamina();
    }

    /// <summary>
    /// 鼠标点击事件处理函数 - 特效系统入口
    /// 当玩家在有效位置点击鼠标时触发
    /// 【重构保留】函数签名和基本逻辑完全不变
    /// </summary>
    /// <param name="mouseWorldPos">鼠标点击的世界坐标位置</param>
    /// <param name="item">当前选中的工具/物品</param>
    private void OnMouseClicked(Vector3 mouseWorldPos, Item item)
    {
        Debug.Log("[输入] 收到点击 " + mouseWorldPos + "  物品=" + (item != null ? item.itemName : "空") + "  体力=" + currentStamina);
        if (isPlayingEffect || item == null)
            return;

        if (!TrySpendStamina(item.itemType))
        {
            Debug.LogWarning("体力不足，先休息一下吧");
            return;
        }

        StartCoroutine(PlayToolEffect(mouseWorldPos, item));
    }

    public int CurrentStamina
    {
        get { return currentStamina; }
    }

    public int MaxStamina
    {
        get { return maxStamina; }
    }

    public bool HasStaminaFor(ItemType type)
    {
        return currentStamina >= GetStaminaCost(type);
    }

    public bool TrySpendStamina(ItemType type)
    {
        int cost = GetStaminaCost(type);
        if (ToolDurability.IsDull(type))
        {
            ToolDurability.TryAutoRepair(type);

            if (ToolDurability.IsDull(type))
            {
                cost += 1;
            }
        }

        if (currentStamina < cost)
        {
            return false;
        }

        currentStamina -= cost;
        EventHandler.CallStaminaChangedEvent(currentStamina, maxStamina);

        ToolDurability.Use(type);

        return true;
    }

    public void RestoreStamina()
    {
        currentStamina = maxStamina;
        EventHandler.CallStaminaChangedEvent(currentStamina, maxStamina);
    }

    public void SetStamina(int value)
    {
        currentStamina = Mathf.Clamp(value, 0, maxStamina);
        EventHandler.CallStaminaChangedEvent(currentStamina, maxStamina);
    }

    private int GetStaminaCost(ItemType type)
    {
        switch (type)
        {
            case ItemType.HoeTool:
                return staminaCostHoe;

            case ItemType.WaterCanTool:
                return staminaCostWater;

            case ItemType.Seed:
                return staminaCostSeed;

            case ItemType.SickleTool:
                return staminaCostHarvest;

            case ItemType.axeTool:
                return staminaCostAxe;

            case ItemType.pickaxeTool:
                return staminaCostPickaxe;

            case ItemType.Deed:
            case ItemType.Scarecrow:
            case ItemType.Greenhouse:
                return staminaCostDeed;

            default:
                return 0;
        }
    }

  
    /// </summary>
    /// <param name="mouseWorldPos">鼠标点击的世界坐标</param>
    /// <param name="item">当前使用的工具/物品</param>
    /// <returns>协程迭代器</returns>
    private IEnumerator PlayToolEffect(Vector3 mouseWorldPos, Item item)
    {
        isPlayingEffect = true;


        GameObject effectToSpawn = null;
        // 特效持续时长（秒）
        float effectDuration = 0.5f;

        switch (item.itemType)
        {
            // 锄头
            case ItemType.HoeTool:
                effectToSpawn = hoeEffectPrefab;
                break;


            // 水壶 
            case ItemType.WaterCanTool:
                effectToSpawn = waterEffectPrefab;
                break;

            // 斧头 
            case ItemType.axeTool:
                effectToSpawn = axeEffectPrefab;
                break;

            // 镐子 
            case ItemType.pickaxeTool:
                effectToSpawn = pickaxeEffectPrefab;
                break;

            // 镰刀 
            case ItemType.SickleTool:
                effectToSpawn = sickleEffectPrefab;
                break;

            case ItemType.Deed:
            case ItemType.Scarecrow:
            case ItemType.Greenhouse:
                effectToSpawn = hoeEffectPrefab;
                break;

            case ItemType.FishingRod:
                effectToSpawn = waterEffectPrefab;
                break;
        }

        if (effectToSpawn != null)
        {
         
            GameObject spawnedEffect = Instantiate(
                effectToSpawn,
                mouseWorldPos,
                Quaternion.identity
            );

            
            Animation anim = spawnedEffect.GetComponent<Animation>();
            Animator animator = spawnedEffect.GetComponent<Animator>();

            if (anim != null && anim.clip != null)
            {
                // 从Animation组件获取时长
                effectDuration = anim.clip.length;
            }
            else if (animator != null)
            {
                // 从Animator组件获取时长
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                effectDuration = stateInfo.length;
            }

          
            Destroy(spawnedEffect, effectDuration);

           
            yield return new WaitForSeconds(effectDuration);
        }
        else
        {
            
            yield return new WaitForSeconds(0.5f);
        }

      
        try
        {
            EventHandler.CallExecuteActionAfterAnimation(mouseWorldPos, item);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[动作] 点击处理出错：" + e);
        }
        finally
        {
            isPlayingEffect = false;
        }
    }

}