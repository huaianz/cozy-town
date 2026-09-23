using Inventory;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;


public class GridMapMangaer : Singleton<GridMapMangaer>
{
    [Header("地形瓦片配置")]
    [Tooltip("已开垦地块的RuleTile")]
    public RuleTile digTile;
    [Tooltip("已浇水地块的RuleTile")]
    public RuleTile waterTile;

    private Tilemap digTilemap;      // 开垦层Tilemap
    private Tilemap waterTilemap;    // 浇水层Tilemap

    [Header("地图数据")]
    [Tooltip("所有地图数据配置文件")]
    public List<MapData_SO> mapData;

    [Header("障碍物瓦片配置")]
    [Tooltip("障碍物（存放树、石头、杂草等）")]
    public Tilemap obstacleTilemap;
    [Tooltip("树木瓦片")]
    public TileBase treeTile;
    [Tooltip("石头瓦片")]
    public TileBase rockTile;
    [Tooltip("杂草瓦片")]
    public TileBase weedTile;

    [Header("障碍物掉落")]
    [SerializeField] private int treeDropItemID = 27;
    [SerializeField] private int treeDropAmount = 2;
    [SerializeField] private int stoneDropItemID = 28;
    [SerializeField] private int stoneDropAmount = 2;
    [SerializeField] private int weedDropItemID = 29;
    [SerializeField] private int weedDropAmount = 1;

    [Header("土地解锁")]
    [Tooltip("初始免费开放范围（以 0,0 为中心的矩形半径）")]
    [SerializeField] private int freeRadiusX = 5;
    [SerializeField] private int freeRadiusY = 4;
    [Tooltip("未开荒地块的瓦片（留空则没有额外视觉）")]
    public TileBase lockedTile;
    public TileBase scarecrowTile;
    public TileBase greenhouseTile;

    private WeatherType currentWeather = WeatherType.Sunny;
    private Collider2D farmAreaCollider;
    private Collider2D clearedAreaCollider;
    private bool farmAreaChecked;
    private bool farmAreaExists;
    private bool clearedAreaChecked;
    private bool clearedAreaExists;
    private readonly Dictionary<int, GameObject> farmGrass = new Dictionary<int, GameObject>();
    private GameObject[] grassPrefabs;
    private Season currentSeason;                     // 当前季节
    private Dictionary<Vector2Int, TileDetails> tileDetailsDict = new Dictionary<Vector2Int, TileDetails>();  // 地块详情字典
    private Grid currentGrid;                         // 网格组件
    private int scarecrowSaves;

    private bool needFindReferences = true;           // 是否需要重新查找引用
    private static readonly Collider2D[] overlapBuffer = new Collider2D[32];
    private const int OreItemID = 37;
    private const int GemItemID = 38;

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }
    #region 生命周期与初始化
    private void Start()
    {
        FindRequiredReferences();

        //直接在编辑器里运行 GameScene 时也能正常种田
        if (tileDetailsDict.Count == 0)
        {
            InitializeMap();
        }
    }

    /// <summary>
    /// 初始化整张地图：先清空旧数据，再按 MapData 建好每一个格子，最后刷新障碍物显示。
    /// 新游戏要调它；读档时也必须先调它，否则存档没有格子可以覆盖。
    /// </summary>
    public void InitializeMap()
    {
        FindRequiredReferences();
        tileDetailsDict.Clear();
        foreach (var mapData in mapData)
        {
            InitTileDetailsDict(mapData);
        }

        RegisterObstaclesFromTilemap();
        RegisterFarmTilesFromGround();
        EnsureStructureTiles();

        // 按数据把障碍物贴图重新画一遍
        RefreshObstacleTiles();

        Debug.Log($"[地图] 初始化完成，共 {tileDetailsDict.Count} 个地块");
    }
    /// <summary>
    /// 查找所有必要的组件引用
    /// </summary>
    private void FindRequiredReferences()
    {
        if (currentGrid == null)
        {
            currentGrid = FindObjectOfType<Grid>();
        }
        if (digTilemap == null)
        {
            digTilemap = FindTilemapFor("Dig", "dig");
        }
        if (waterTilemap == null)
        {
            waterTilemap = FindTilemapFor("Water", "water");
        }
        if (obstacleTilemap == null)
        {
            obstacleTilemap = FindTilemapFor("Obstacle", "obstacle");
        }
        needFindReferences = false;
    }

    private static Tilemap FindTilemapFor(string tag, string namePart)
    {
        Tilemap[] all = FindObjectsOfType<Tilemap>();

        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null)
            {
                continue;
            }

            if (all[i].gameObject.name.ToLower().Contains(namePart))
            {
                return all[i];
            }
        }

        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null)
            {
                continue;
            }

            bool tagged = false;

            try
            {
                tagged = all[i].gameObject.CompareTag(tag);
            }
            catch (UnityException)
            {
                tagged = false;
            }

            if (tagged)
            {
                return all[i];
            }
        }

        return null;
    }

    private void OnEnable()
    {
        EventHandler.ExecuteActionAfterAnimation += OnExecuteActionAfterAnimation;
        EventHandler.GameDayEvent += OnGameDayEvent;
        EventHandler.RefreshCurrentMap += RefreshMap;
        EventHandler.WeatherChangedEvent += OnWeatherChangedEvent;
    }

    private void OnDisable()
    {
        EventHandler.ExecuteActionAfterAnimation -= OnExecuteActionAfterAnimation;
        EventHandler.GameDayEvent -= OnGameDayEvent;
        EventHandler.RefreshCurrentMap -= RefreshMap;
        EventHandler.WeatherChangedEvent -= OnWeatherChangedEvent;
    }
    #endregion

    #region 核心工具交互逻辑
    /// <summary>
    /// 动画播放完成后执行实际操作
    /// 这是工具交互的核心入口
    /// </summary>
    private bool TryPickupAt(Vector3 worldPosition)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPosition);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null)
            {
                continue;
            }

            FruitPickup pickup = hits[i].GetComponent<FruitPickup>();

            if (pickup == null)
            {
                pickup = hits[i].GetComponentInParent<FruitPickup>();
            }

            if (pickup != null)
            {
                pickup.Collect();
                Debug.Log("[拾取] 捡起掉落物");
                return true;
            }
        }

        FruitPickup[] all = FindObjectsOfType<FruitPickup>();
        FruitPickup nearest = null;
        float bestDistance = 1.3f;

        for (int k = 0; k < all.Length; k++)
        {
            if (all[k] == null || !all[k].gameObject.activeInHierarchy)
            {
                continue;
            }

            float distance = Vector3.Distance(all[k].transform.position, worldPosition);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                nearest = all[k];
            }
        }

        if (nearest != null)
        {
            nearest.Collect();
            Debug.Log("[拾取] 捡起附近的掉落物（范围兜底）");
            return true;
        }

        return false;
    }

    private void OnExecuteActionAfterAnimation(Vector3 mouseWorldPos, Item item)
    {
        if (needFindReferences)
            FindRequiredReferences();

        // 将鼠标坐标转换为网格坐标
        var mouseGridPos = currentGrid.WorldToCell(mouseWorldPos);

        Debug.Log("[点击] 物品=" + (item != null ? item.itemID + ":" + item.itemName : "空") + " 位置=" + mouseWorldPos);

        if (item != null && AreaUnlockManager.Instance != null && AreaUnlockManager.Instance.BlockedNameAt(mouseWorldPos) != null)
        {
            AreaUnlockPanelUI.Open();
            return;
        }

        if (item != null && item.itemType == ItemType.WaterCanTool && WaterWellManager.Instance != null)
        {
            if (WaterWellManager.Instance.IsWell(mouseWorldPos))
            {
                WaterWellManager.Instance.TryFill();
                return;
            }

            if (!WaterWellManager.Instance.HasWater)
            {
                EventHandler.CallFarmEventEvent("水井", "水壶空了，先去水井打水", true);
                return;
            }
        }

        if (item != null && item.itemID == 31 && InventoryManager.Instance != null && InventoryManager.Instance.GetItemAmountInBag(31) > 0)
        {
            TileDetails compostTile = GetTileDetailsOnMousePosition(mouseGridPos);

            if (compostTile != null && compostTile.seedItemID != -1 && CropManager.Instance != null && !CropManager.Instance.IsCropMature(compostTile))
            {
                InventoryManager.Instance.RemoveItem(31, 1);

                compostTile.growthFraction += 2f;

                int compostDays = Mathf.FloorToInt(compostTile.growthFraction);

                if (compostDays > 0)
                {
                    compostTile.growthFraction -= compostDays;
                    compostTile.growthDays += compostDays;
                }

                CropManager.Instance.RefreshCropVisual(compostTile);

                EventHandler.CallFarmEventEvent("堆肥", "施了肥，作物一口气长了两天", false);
            }
            else
            {
                EventHandler.CallFarmEventEvent("堆肥", "要对准田里还没成熟的作物用", true);
            }

            return;
        }

        if (item != null && PondManager.Instance != null && PondManager.Instance.ThrowItem(item.itemID, mouseWorldPos))
        {
            return;
        }

        if (TryPickupAt(mouseWorldPos))
        {
            return;
        }

        if (item != null && DecorationManager.Instance != null && DecorationManager.Instance.TryPlace(item.itemID, mouseWorldPos))
        {
            return;
        }

        if (item != null && item.itemType == ItemType.pickaxeTool && DecorationManager.Instance != null && DecorationManager.Instance.TryRemove(mouseWorldPos))
        {
            return;
        }

        if (item != null && item.itemType == ItemType.pickaxeTool && MineManager.Instance != null && MineManager.Instance.TryMine(mouseWorldPos))
        {
            return;
        }

        if (item != null && item.itemType == ItemType.SickleTool && GrassManager.Instance != null && GrassManager.Instance.TryCut(mouseWorldPos))
        {
            return;
        }

        if (item != null && currentGrid != null)
        {
            ApplyToolArea(item, mouseWorldPos, mouseGridPos);
        }

        if (item != null && item.itemType == ItemType.FishingRod)
        {
            if (FishingManager.Instance != null)
            {
                FishingManager.Instance.CastAt(mouseWorldPos);
            }

            return;
        }

        var currentTile = GetTileDetailsOnMousePosition(mouseGridPos);

        if (currentTile != null)
        {
            Crop currentCrop = GetCropObject(mouseWorldPos);

            switch (item.itemType)
            {
                #region 种植种子
                case ItemType.Seed:
                    // 有障碍物时禁止种植
                    if (currentTile.hasObstacle)
                    {
                        Debug.Log("该地块有障碍物，无法种植！");
                        break;
                    }

                    int remainingAmount = InventoryManager.Instance.GetItemAmountInBag(item.itemID);
                    if (remainingAmount <= 0)
                    {
                        EventHandler.CallItemSelectedEvent(null, false);
                        break;
                    }
                    // 只有已开垦且空的地块才能种植
                    if (currentTile.daysSinceDug > -1 && currentTile.seedItemID == -1)
                    {
                        EventHandler.CallPlantSeedEvent(item.itemID, currentTile);
                        InventoryManager.Instance.RemoveItem(item.itemID, 1);
                    }
                    break;
                #endregion

                #region 镐子（破坏石头）
                case ItemType.pickaxeTool:
                    ProcessPickaxeAction(currentTile, mouseGridPos);
                    break;
                #endregion

                #region 镰刀（收获成熟作物）
                case ItemType.SickleTool:
                    // 有障碍物时无法收获
                    if (currentTile.hasObstacle)
                    {
                        Debug.Log("[收割] 被障碍物判定挡住：格子 " + currentTile.girdX + "," + currentTile.girdY + " 障碍类型=" + currentTile.obstacleType);
                        break;
                    }

                    Debug.Log("[工具] 镰刀点击格子 " + currentTile.girdX + "," + currentTile.girdY + " 该格有树=" + (FruitTreeManager.Instance != null && FruitTreeManager.Instance.HasTree(currentTile.girdX, currentTile.girdY)) + " 可收割=" + (FruitTreeManager.Instance != null && FruitTreeManager.Instance.CanHarvest(currentTile.girdX, currentTile.girdY)));
                    if (FruitTreeManager.Instance != null && FruitTreeManager.Instance.TryHarvest(currentTile.girdX, currentTile.girdY))
                    {
                        break;
                    }

                    if (currentCrop != null && currentCrop.Canharvest)
                    {
                        currentCrop.ProcessToolAction(item, currentCrop.tileDetails);
                    }
                    else
                    {
                        Debug.Log("[收割] 收割失败：格子 " + currentTile.girdX + "," + currentTile.girdY + " 种子=" + currentTile.seedItemID + " 生长=" + currentTile.growthDays + " 有作物=" + (currentCrop != null) + " 可收=" + (currentCrop != null && currentCrop.Canharvest));
                    }
                    break;
                #endregion

                #region 锄头（开垦地块/清除杂草）
                case ItemType.HoeTool:
                    // 有障碍物且不是杂草时禁止开垦
                    if (currentTile.hasObstacle && currentTile.obstacleType != ObstacleType.Weed)
                    {
                        Debug.Log("该地块有障碍物，需要先清除！");
                        break;
                    }

                    // 清除杂草
                    if (currentTile.hasObstacle && currentTile.obstacleType == ObstacleType.Weed)
                    {
                        ClearObstacle(currentTile, mouseGridPos);
                    }

                    // 执行开垦
                    if (currentTile.canDig && currentTile.seedItemID == -1)
                    {
                        SetDigGround(currentTile);
                        currentTile.daysSinceDug = 0;
                        currentTile.canDig = false;
                    }
                    break;
                #endregion

                #region 斧头（砍伐树木）
                case ItemType.axeTool:
                    Debug.Log("[工具] 斧头点击格子 " + currentTile.girdX + "," + currentTile.girdY + " 该格有树=" + (FruitTreeManager.Instance != null && FruitTreeManager.Instance.HasTree(currentTile.girdX, currentTile.girdY)));
                    if (FruitTreeManager.Instance != null && FruitTreeManager.Instance.TryChop(currentTile.girdX, currentTile.girdY))
                    {
                        break;
                    }

                    ProcessAxeAction(currentTile, mouseGridPos);
                    break;
                #endregion

                #region 水壶（给地块浇水）
                case ItemType.WaterCanTool:
                    // 有障碍物时禁止浇水
                    if (currentTile.hasObstacle)
                        break;

                    if (currentTile.daysSinceDug > -1 && currentTile.daysSinceWatered == -1)
                    {
                        SetWaterGround(currentTile);
                        currentTile.daysSinceWatered = 0;
                        currentTile.isWatered = true;

                        if (WaterWellManager.Instance != null)
                        {
                            WaterWellManager.Instance.UseCharge();
                        }

                        if (currentTile.seedItemID != -1)
                        {
                            currentTile.growthFraction += Settings.wateredCropGrowthBonus;

                            int gained = Mathf.FloorToInt(currentTile.growthFraction);

                            if (gained > 0)
                            {
                                currentTile.growthFraction -= gained;
                                currentTile.growthDays += gained;

                                EventHandler.CallFarmEventEvent("浇水", "浇过水的作物马上长大了一点", false);
                            }

                            if (CropManager.Instance != null)
                            {
                                CropManager.Instance.RefreshCropVisual(currentTile);
                            }

                            if (UnityEngine.Random.value < 0.12f)
                            {
                                DropCareFind();
                            }
                        }
                    }
                    break;
                #endregion

                case ItemType.Deed:
                    if (!currentTile.isUnlocked && InventoryManager.Instance.GetItemAmountInBag(item.itemID) > 0 && TryUnlockTile(currentTile))
                    {
                        InventoryManager.Instance.RemoveItem(item.itemID, 1);
                    }
                    break;

                case ItemType.Scarecrow:
                    if (currentTile.isUnlocked && !currentTile.hasObstacle && !currentTile.hasScarecrow && InventoryManager.Instance.GetItemAmountInBag(item.itemID) > 0)
                    {
                        currentTile.hasScarecrow = true;
                        InventoryManager.Instance.RemoveItem(item.itemID, 1);
                        RefreshTileVisual(currentTile);
                    }
                    break;

                case ItemType.Greenhouse:
                    if (currentTile.isUnlocked && !currentTile.hasObstacle && !currentTile.inGreenhouse && InventoryManager.Instance.GetItemAmountInBag(item.itemID) > 0)
                    {
                        currentTile.inGreenhouse = true;
                        InventoryManager.Instance.RemoveItem(item.itemID, 1);
                        RefreshTileVisual(currentTile);
                    }
                    break;

                case ItemType.FishingRod:
                    if (FishingManager.Instance != null)
                    {
                        FishingManager.Instance.CastAt(mouseWorldPos);
                    }
                    break;

                case ItemType.Product:
                    break;
            }

            PlayActionSfx(item.itemType);
            UpdateTileDetails(currentTile);
        }
    }
    #endregion

    #region 障碍物处理核心逻辑
    /// <summary>
    /// 斧头动作 - 砍伐树木
    /// 只有树木类型的障碍物才能用斧头砍伐
    /// </summary>
    /// <param name="tile">目标地块</param>
    /// <param name="gridPos">网格坐标</param>
    private void ProcessAxeAction(TileDetails tile, Vector3Int gridPos)
    {
        // 校验：只有有障碍物且是树木类型
        if (!tile.hasObstacle || tile.obstacleType != ObstacleType.Tree)
        {
            Debug.Log("这里没有树木可以砍伐");
            return;
        }

        // 清除障碍物
        ClearObstacle(tile, gridPos);

        Debug.Log("成功砍伐树木，地块已恢复可开垦状态！");
    }

    /// <summary>
    /// 镐子动作 - 破坏石头
    /// 只有石头类型的障碍物才能用镐子破坏
    /// </summary>
    /// <param name="tile">目标地块</param>
    /// <param name="gridPos">网格坐标</param>
    private void ProcessPickaxeAction(TileDetails tile, Vector3Int gridPos)
    {
        // 校验：只有有障碍物且是石头类型
        if (!tile.hasObstacle || tile.obstacleType != ObstacleType.Rock)
        {
            Debug.Log("这里没有石头可以挖掘");
            return;
        }

        // 清除障碍物
        ClearObstacle(tile, gridPos);

        Debug.Log("成功破坏石头，地块已恢复可开垦状态！");
    }

    /// <summary>
    /// 清除指定位置的障碍物
    /// 会自动恢复地块的可开垦权限
    /// </summary>
    /// <param name="tile">目标地块详情</param>
    /// <param name="gridPos">网格坐标</param>
    private void ClearObstacle(TileDetails tile, Vector3Int gridPos)
    {
        ObstacleType clearedType = tile.obstacleType;

        // 1. 更新地块状态
        tile.hasObstacle = false;
        tile.obstacleType = ObstacleType.None;
        tile.canDig = true;           // 恢复可开垦权限

        // 2. 移除Tilemap上的障碍物显示
        if (obstacleTilemap != null)
        {
            obstacleTilemap.SetTile(gridPos, null);
        }

        // 3. 生成障碍物掉落物
        SpawnObstacleDrop(clearedType, gridPos, tile.isOreNode);
    }

    private void SpawnObstacleDrop(ObstacleType type, Vector3Int gridPos, bool oreNode)
    {
        int itemID;
        int amount;

        switch (type)
        {
            case ObstacleType.Tree:
                itemID = treeDropItemID;
                amount = treeDropAmount;
                break;

            case ObstacleType.Rock:
                if (oreNode)
                {
                    itemID = OreItemID;
                    amount = UnityEngine.Random.Range(1, 3);

                    if (UnityEngine.Random.value < 0.25f && CropManager.Instance != null)
                    {
                        CropManager.Instance.SpawnFruit(GemItemID, new Vector3(gridPos.x + 0.5f, gridPos.y + 0.5f, 0f));
                    }
                }
                else
                {
                    itemID = stoneDropItemID;
                    amount = stoneDropAmount;
                }
                break;

            case ObstacleType.Weed:
                itemID = weedDropItemID;
                amount = weedDropAmount;
                break;

            default:
                return;
        }

        if (itemID <= 0 || amount <= 0 || CropManager.Instance == null)
        {
            return;
        }

        for (int i = 0; i < amount; i++)
        {
            Vector3 spawnPos = new Vector3(gridPos.x + 0.5f, gridPos.y + 0.5f, 0);

            if (amount > 1)
            {
                float angle = (i / (float)amount) * Mathf.PI * 2f;
                spawnPos += new Vector3(Mathf.Cos(angle) * 0.25f, Mathf.Sin(angle) * 0.25f, 0);
            }

            CropManager.Instance.SpawnFruit(itemID, spawnPos);
        }
    }

    /// <summary>
    /// 刷新所有障碍物瓦片显示
    /// 游戏启动和地图刷新时调用
    /// </summary>
    public void SetLockedTile(TileBase tile)
    {
        lockedTile = tile;
        RefreshObstacleTiles();
    }

    public bool IsInFreeArea(int x, int y)
    {
        CheckFarmArea();
        CheckClearedArea();

        if (farmAreaExists && !InsideCollider(farmAreaCollider, x, y))
        {
            return false;
        }

        if (clearedAreaExists)
        {
            return InsideCollider(clearedAreaCollider, x, y);
        }

        int level = ProgressManager.Level;
        int bonusX = Mathf.Clamp(level - 2, 0, 8);
        int bonusY = Mathf.Clamp(level - 2, 0, 5);

        return Mathf.Abs(x) <= freeRadiusX + bonusX && Mathf.Abs(y) <= freeRadiusY + bonusY;
    }

    public bool IsInsideFarmArea(int x, int y)
    {
        CheckFarmArea();

        if (!farmAreaExists)
        {
            return true;
        }

        return InsideCollider(farmAreaCollider, x, y);
    }

    private static bool InsideCollider(Collider2D collider, int x, int y)
    {
        if (collider == null)
        {
            return false;
        }

        Bounds area = collider.bounds;
        float tileMinX = x;
        float tileMaxX = x + 1f;
        float tileMinY = y;
        float tileMaxY = y + 1f;

        return area.min.x < tileMaxX && area.max.x > tileMinX && area.min.y < tileMaxY && area.max.y > tileMinY;
    }

    private static void LogTileRange(string label, Collider2D collider)
    {
        if (collider == null)
        {
            return;
        }

        Bounds area = collider.bounds;
        int minX = Mathf.FloorToInt(area.min.x);
        int maxX = Mathf.CeilToInt(area.max.x) - 1;
        int minY = Mathf.FloorToInt(area.min.y);
        int maxY = Mathf.CeilToInt(area.max.y) - 1;

        Debug.Log("[农田] " + label + " 覆盖的格子 x " + minX + "~" + maxX + " y " + minY + "~" + maxY
            + "（共 " + ((maxX - minX + 1) * (maxY - minY + 1)) + " 格）");
    }

    private void CheckClearedArea()
    {
        if (clearedAreaChecked)
        {
            return;
        }

        clearedAreaChecked = true;

        Collider2D[] all = FindObjectsOfType<Collider2D>();

        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null)
            {
                continue;
            }

            string lower = all[i].gameObject.name.ToLower();
            string tag = all[i].gameObject.tag.ToLower();

            if (lower.Contains("cleared") || lower.Contains("clear area") || lower.Contains("plant area")
                || lower.Contains("plantable") || lower.Contains("已开垦") || lower.Contains("可种植") || tag.Contains("farm"))
            {
                if (farmAreaCollider != null && all[i] == farmAreaCollider)
                {
                    continue;
                }

                clearedAreaCollider = all[i];
                clearedAreaExists = true;
                break;
            }
        }

        if (clearedAreaExists)
        {
            LogTileRange("可种植区", clearedAreaCollider);

            Debug.Log("[农田] 可种植区碰撞体： " + clearedAreaCollider.gameObject.name
                + "  范围 x " + clearedAreaCollider.bounds.min.x.ToString("0.0") + "~" + clearedAreaCollider.bounds.max.x.ToString("0.0")
                + " y " + clearedAreaCollider.bounds.min.y.ToString("0.0") + "~" + clearedAreaCollider.bounds.max.y.ToString("0.0"));
        }
        else
        {
            Debug.Log("[农田] 没找到可种植区碰撞体（名字含 cleared / plant area / 可种植），圈内全判为未开荒");
        }
    }

    private void CheckFarmArea()
    {
        if (farmAreaChecked)
        {
            return;
        }

        farmAreaChecked = true;

        Collider2D[] all = FindObjectsOfType<Collider2D>();

        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null)
            {
                continue;
            }

            string lower = all[i].gameObject.name.ToLower();
            string tag = all[i].gameObject.tag.ToLower();

            if (lower.Contains("farm") || lower.Contains("plant area") || lower.Contains("种植") || tag.Contains("farm"))
            {
                farmAreaCollider = all[i];
                farmAreaExists = true;
                break;
            }
        }

        if (farmAreaExists)
        {
            LogTileRange("FarmArea", farmAreaCollider);

            Bounds areaBounds = farmAreaCollider.bounds;

            Debug.Log("[农田] FarmArea 已找到：" + farmAreaCollider.gameObject.name
                + "  中心 " + areaBounds.center.x.ToString("0.00") + " , " + areaBounds.center.y.ToString("0.00")
                + "  大小 " + areaBounds.size.x.ToString("0.00") + " x " + areaBounds.size.y.ToString("0.00")
                + "  范围 x " + areaBounds.min.x.ToString("0.0") + "~" + areaBounds.max.x.ToString("0.0")
                + " y " + areaBounds.min.y.ToString("0.0") + "~" + areaBounds.max.y.ToString("0.0"));
        }
        else
        {
            Debug.Log("[农田] 没有找到 FarmArea 碰撞体（名字里要含 farm / plant / 种植），改用默认范围");
        }
    }

    public bool TryUnlockTile(TileDetails tile)
    {
        if (tile == null || tile.isUnlocked)
        {
            return false;
        }

        tile.isUnlocked = true;
        tile.canDig = !tile.hasObstacle;
        RefreshTileVisual(tile);

        return true;
    }

    private void RefreshTileVisual(TileDetails tile)
    {
        if (obstacleTilemap == null)
        {
            return;
        }

        Vector3Int pos = new Vector3Int(tile.girdX, tile.girdY, 0);

        if (tile.hasObstacle)
        {
            obstacleTilemap.SetTile(pos, GetObstacleTile(tile.obstacleType));

            return;
        }

        if (!tile.isUnlocked && lockedTile != null)
        {
            obstacleTilemap.SetTile(pos, lockedTile);

            return;
        }

        if (tile.inGreenhouse && greenhouseTile != null)
        {
            obstacleTilemap.SetTile(pos, greenhouseTile);

            return;
        }

        if (tile.hasScarecrow && scarecrowTile != null)
        {
            obstacleTilemap.SetTile(pos, scarecrowTile);

            return;
        }

        obstacleTilemap.SetTile(pos, null);
    }

    private void RefreshObstacleTiles()
    {
        if (obstacleTilemap == null) return;

        obstacleTilemap.ClearAllTiles();

        foreach (var tilePair in tileDetailsDict)
        {
            RefreshTileVisual(tilePair.Value);
        }
    }

    /// <summary>
    /// 根据障碍物类型获取对应的瓦片
    /// </summary>
    private TileBase GetObstacleTile(ObstacleType type)
    {
        return type switch
        {
            ObstacleType.Tree => treeTile,
            ObstacleType.Rock => rockTile,
            ObstacleType.Weed => weedTile,
            _ => null
        };
    }
    #endregion

    #region 工具方法
    /// <summary>
    /// 获取指定位置的作物对象
    /// </summary>
    public Crop GetCropObject(Vector3 mouseWorldPos)
    {
        int count = Physics2D.OverlapPointNonAlloc(mouseWorldPos, overlapBuffer);
        Crop currentCrop = null;

        for (int i = 0; i < count; i++)
        {
            Crop crop = overlapBuffer[i].GetComponent<Crop>();

            if (crop != null)
            {
                currentCrop = crop;
            }
        }

        return currentCrop;
    }

    /// <summary>
    /// 初始化地块详情字典
    /// 核心修改：新增障碍物数据初始化
    /// </summary>
    public void InitTileDetailsDict(MapData_SO mapData)
    {
        foreach (TileProperty tileProperty in mapData.tileProperties)
        {
            TileDetails tileDetails = new TileDetails
            {
                girdX = tileProperty.tileCoordinate.x,
                girdY = tileProperty.tileCoordinate.y,
                // ========== 新增障碍物数据初始化 ==========
                obstacleType = tileProperty.obstacleType,
                hasObstacle = tileProperty.hasObstacle,
            };

            Vector2Int key = new Vector2Int(tileDetails.girdX, tileDetails.girdY);

            if (GetTileDetails(key) != null)
            {
                tileDetails = GetTileDetails(key);
            }

            // 根据地块类型设置初始状态
            tileDetails.isUnlocked = tileProperty.isUnlocked && IsInFreeArea(tileDetails.girdX, tileDetails.girdY);

            switch (tileProperty.gridType)
            {
                case GridType.EmptyLand:
                    // 有障碍物的地块不可开垦
                    tileDetails.canDig = tileDetails.isUnlocked && !tileDetails.hasObstacle;
                    break;
                case GridType.Tillable:
                    tileDetails.canDig = tileDetails.isUnlocked && !tileDetails.hasObstacle;
                    break;
                case GridType.TilledLand:
                    tileDetails.canDig = false;
                    tileDetails.daysSinceDug = 0;
                    break;
                case GridType.WateredLand:
                    tileDetails.canDig = false;
                    tileDetails.daysSinceDug = 0;
                    tileDetails.daysSinceWatered = 0;
                    tileDetails.isWatered = true;
                    break;
                case GridType.Obstacle:
                    // 障碍物地块：不可开垦，等待清除
                    tileDetails.canDig = false;
                    break;
            }

            if (GetTileDetails(key) != null)
            {
                tileDetailsDict[key] = tileDetails;
            }
            else
            {
                tileDetailsDict.Add(key, tileDetails);
            }
        }
    }

    /// <summary>
    /// 根据Key获取地块详情
    /// </summary>
    private TileDetails GetTileDetails(Vector2Int key)
    {
        if (tileDetailsDict.ContainsKey(key))
        {
            return tileDetailsDict[key];
        }
        return null;
    }

    /// <summary>
    /// 根据鼠标网格坐标获取地块详情
    /// </summary>
    public TileDetails GetTileDetailsOnMousePosition(Vector3Int mouseGridPos)
    {
        Vector2Int key = new Vector2Int(mouseGridPos.x, mouseGridPos.y);
        return GetTileDetails(key);
    }

    /// <summary>
    /// 每日更新逻辑
    /// </summary>
    private void EnsureFarmGrass(TileDetails tile)
    {
        int key = tile.girdX * 1000 + tile.girdY;
        GameObject existing;

        if (farmGrass.TryGetValue(key, out existing))
        {
            if (existing != null && existing.activeInHierarchy)
            {
                tile.canDig = false;
                return;
            }

            farmGrass.Remove(key);
        }

        if (grassPrefabs == null)
        {
            grassPrefabs = new GameObject[3];
            grassPrefabs[0] = Resources.Load<GameObject>("Prefabs/Grass01");
            grassPrefabs[1] = Resources.Load<GameObject>("Prefabs/Grass02");
            grassPrefabs[2] = Resources.Load<GameObject>("Prefabs/Grass03");
        }

        GameObject prefab = grassPrefabs[UnityEngine.Random.Range(0, grassPrefabs.Length)];

        if (prefab == null)
        {
            return;
        }

        GameObject grass = Instantiate(prefab, new Vector3(tile.girdX + 0.5f, tile.girdY + 0.5f, 0f), Quaternion.identity);

        SpriteRenderer[] renderers = grass.GetComponentsInChildren<SpriteRenderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingLayerName = "Ground Top";
            renderers[i].sortingOrder = 30;
        }

        farmGrass[key] = grass;
        tile.canDig = false;

        Debug.Log("[农田] 第 " + (key / 1000) + "," + (key % 1000) + " 格长出野草");
    }

    private void ApplyToolArea(Item item, Vector3 worldPosition, Vector3Int centerCell)
    {
        if (item == null || currentGrid == null)
        {
            return;
        }

        int level = ToolDurability.GetLevel(item.itemType);

        if (level < 2)
        {
            return;
        }

        bool wide = level >= 3;
        bool isHoe = item.itemType == ItemType.HoeTool;
        bool isWater = item.itemType == ItemType.WaterCanTool;
        bool isSickle = item.itemType == ItemType.SickleTool;

        if (!isHoe && !isWater && !isSickle)
        {
            return;
        }

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                {
                    continue;
                }

                bool orthogonal = Mathf.Abs(dx) + Mathf.Abs(dy) == 1;

                if (!orthogonal && !wide)
                {
                    continue;
                }

                Vector3Int cell = new Vector3Int(centerCell.x + dx, centerCell.y + dy, 0);
                TileDetails tile = GetTileDetails(new Vector2Int(cell.x, cell.y));

                if (tile == null || tile.hasObstacle)
                {
                    continue;
                }

                if (isHoe && tile.canDig && tile.seedItemID == -1)
                {
                    SetDigGround(tile);
                    tile.daysSinceDug = 0;
                    tile.canDig = false;
                }
                else if (isWater && tile.daysSinceDug > -1 && tile.daysSinceWatered == -1)
                {
                    SetWaterGround(tile);
                    tile.daysSinceWatered = 0;
                    tile.isWatered = true;

                    if (tile.seedItemID != -1)
                    {
                        tile.growthFraction += Settings.wateredCropGrowthBonus;

                        int gained = Mathf.FloorToInt(tile.growthFraction);

                        if (gained > 0)
                        {
                            tile.growthFraction -= gained;
                            tile.growthDays += gained;
                        }

                        if (CropManager.Instance != null)
                        {
                            CropManager.Instance.RefreshCropVisual(tile);
                        }
                    }
                }
                else if (isSickle)
                {
                    Vector3 point = new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
                    Collider2D[] hits = Physics2D.OverlapPointAll(point);

                    for (int h = 0; h < hits.Length; h++)
                    {
                        if (hits[h] == null)
                        {
                            continue;
                        }

                        Crop crop = hits[h].GetComponent<Crop>();

                        if (crop != null && crop.Canharvest && crop.tileDetails != null)
                        {
                            crop.ProcessToolAction(item, crop.tileDetails);
                            break;
                        }
                    }
                }
            }
        }
    }

    public void OnGameDayEvent(int day, Season season)
    {
        currentSeason = season;
        ApplyWeatherDamage(season);
        foreach (var tile in tileDetailsDict)
        {
            bool wasWatered = tile.Value.isWatered || tile.Value.daysSinceWatered > -1;

            // 浇水状态重置
            if (tile.Value.daysSinceWatered > -1)
            {
                tile.Value.daysSinceWatered = -1;
                tile.Value.isWatered = false;
                ClearWaterTile(tile.Value);
            }
            // 开垦天数增加
            if (tile.Value.daysSinceDug > -1)
            {
                tile.Value.daysSinceDug++;
            }
            // 长时间没种东西就会长野草（Grass 预制体）
            if (tile.Value.isUnlocked && tile.Value.seedItemID == -1 && tile.Value.daysSinceDug >= 3)
            {
                EnsureFarmGrass(tile.Value);
            }

            // 超过5天未种植的地块恢复原状
            if (tile.Value.daysSinceDug > 5 && tile.Value.seedItemID == -1)
            {
                tile.Value.daysSinceDug = -1;
                // 只有没有障碍物的地块才能恢复可开垦
                tile.Value.canDig = !tile.Value.hasObstacle;
                tile.Value.growthDays = -1;
                ClearDigTile(tile.Value);
            }
            // 作物生长天数增加
            if (tile.Value.seedItemID != -1)
            {
                    float baseGrowth = wasWatered ? Settings.wateredCropGrowthPerDay : Settings.dryCropGrowthPerDay;
                    float growthMultiplier = WeatherEffects.GetGrowthMultiplier(currentWeather, season);

                    if (tile.Value.inGreenhouse)
                    {
                        growthMultiplier = Mathf.Max(growthMultiplier, 1f);
                    }
                    else
                    {
                        if (growthMultiplier > 0f)
                        {
                            growthMultiplier += FarmEventEffects.growthBonus;
                        }

                        if (FarmEventEffects.growthFrozen)
                        {
                            growthMultiplier = 0f;
                        }
                    }

                    if (growthMultiplier > 0f)
                    {
                        tile.Value.growthFraction += baseGrowth * growthMultiplier;
                        int gainedDays = Mathf.FloorToInt(tile.Value.growthFraction);

                        if (gainedDays > 0)
                        {
                            tile.Value.growthFraction -= gainedDays;
                            tile.Value.growthDays += gainedDays;
                        }
                    }
                CropManager.Instance.RefreshCropVisual(tile.Value);
            }
        }
        RefreshObstacleTiles();
    }

    public int ScarecrowSaves
    {
        get { return scarecrowSaves; }
    }

    private static void DropCareFind()
    {
        if (InventoryManager.Instance == null)
        {
            return;
        }

        int[] drops = { 57, 29, 55 };
        int dropID = drops[UnityEngine.Random.Range(0, drops.Length)];

        InventoryManager.Instance.AddItem(dropID, 1);

        Item dropItem = InventoryManager.Instance.GetItem(dropID);
        string dropName = dropItem != null ? dropItem.itemName : "素材";

        EventHandler.CallFarmEventEvent("顺手", "在田边发现了 " + dropName + "x1", false);
    }

    public void GetFarmSummary(out int planted, out int watered, out int mature)
    {
        planted = 0;
        watered = 0;
        mature = 0;

        foreach (var tile in tileDetailsDict)
        {
            if (tile.Value.seedItemID == -1)
            {
                continue;
            }

            planted++;

            if (tile.Value.isWatered || tile.Value.daysSinceWatered > -1)
            {
                watered++;
            }

            if (CropManager.Instance != null && CropManager.Instance.IsCropMature(tile.Value))
            {
                mature++;
            }
        }
    }

    public Tilemap GetGroundTilemap()
    {
        GameObject groundObject = GameObject.Find("Ground Middle");

        if (groundObject == null)
        {
            return null;
        }

        return groundObject.GetComponent<Tilemap>();
    }

    public Tilemap GetDecorTilemap()
    {
        GameObject decorObject = GameObject.Find("Ground Top");

        if (decorObject == null)
        {
            return null;
        }

        return decorObject.GetComponent<Tilemap>();
    }

    public TileDetails RegisterGeneratedTile(int x, int y)
    {
        Vector2Int key = new Vector2Int(x, y);
        TileDetails tile = GetTileDetails(key);

        if (tile == null)
        {
            tile = new TileDetails
            {
                girdX = x,
                girdY = y,
                isUnlocked = true,
                canDig = true
            };

            tileDetailsDict.Add(key, tile);
        }
        else
        {
            tile.isUnlocked = true;
        }

        return tile;
    }

    public void RefreshTileVisualFor(TileDetails tile)
    {
        RefreshTileVisual(tile);
    }

    public void RegisterObstaclesFromTilemap()
    {
        if (obstacleTilemap == null)
        {
            return;
        }

        BoundsInt bounds = obstacleTilemap.cellBounds;
        int count = 0;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                TileBase tile = obstacleTilemap.GetTile(new Vector3Int(x, y, 0));

                if (tile == null)
                {
                    continue;
                }

                if (tile == lockedTile || tile == scarecrowTile || tile == greenhouseTile)
                {
                    continue;
                }

                string lower = tile.name.ToLower();

                if (lower.Contains("locked") || lower.Contains("scarecrow") || lower.Contains("greenhouse"))
                {
                    continue;
                }

                Vector2Int key = new Vector2Int(x, y);
                TileDetails detail = GetTileDetails(key);

                if (detail == null)
                {
                    detail = new TileDetails();
                    detail.girdX = x;
                    detail.girdY = y;
                    detail.isUnlocked = IsInFreeArea(x, y);
                    tileDetailsDict.Add(key, detail);
                }

                detail.hasObstacle = true;
                detail.obstacleType = ObstacleTypeFromTileName(lower);
                detail.canDig = false;
                count++;
            }
        }

        Debug.Log("[农田] 按障碍物层登记障碍 " + count + " 个");
    }

    private static ObstacleType ObstacleTypeFromTileName(string lower)
    {
        if (lower.Contains("tree") || lower.Contains("树"))
        {
            return ObstacleType.Tree;
        }

        if (lower.Contains("weed") || lower.Contains("grass") || lower.Contains("草"))
        {
            return ObstacleType.Weed;
        }

        return ObstacleType.Rock;
    }

    public void RegisterFarmTilesFromGround()
    {
        Tilemap ground = GetGroundTilemap();

        if (ground == null)
        {
            return;
        }

        BoundsInt bounds = ground.cellBounds;
        int count = 0;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            if (Mathf.Abs(x) > 30)
            {
                continue;
            }

            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                if (Mathf.Abs(y) > 30)
                {
                    continue;
                }

                if (ground.GetTile(new Vector3Int(x, y, 0)) == null)
                {
                    continue;
                }

                Vector2Int key = new Vector2Int(x, y);

                if (!IsInsideFarmArea(x, y))
                {
                    continue;
                }

                if (GetTileDetails(key) != null)
                {
                    continue;
                }

                TileDetails tile = new TileDetails();
                tile.girdX = x;
                tile.girdY = y;
                tile.isUnlocked = IsInFreeArea(x, y);
                tile.canDig = tile.isUnlocked;

                tileDetailsDict.Add(key, tile);
                count++;
            }
        }

        int unlockedCount = 0;

        foreach (KeyValuePair<Vector2Int, TileDetails> pair in tileDetailsDict)
        {
            if (pair.Value.isUnlocked)
            {
                unlockedCount++;
            }
        }

        Debug.Log("[农田] 按 Ground Middle 登记地块 " + count + " 个；当前可种植 " + unlockedCount + " 格，未开荒 " + (tileDetailsDict.Count - unlockedCount) + " 格");
    }

    public void EnsureStructureTiles()
    {
        if (scarecrowTile == null)
        {
            scarecrowTile = CreateStructureTile("structure_scarecrow");
        }

        if (greenhouseTile == null)
        {
            greenhouseTile = CreateStructureTile("structure_greenhouse");
        }
    }

    private static TileBase CreateStructureTile(string resourceName)
    {
        Texture2D texture = Resources.Load<Texture2D>("UITheme/" + resourceName);

        if (texture == null)
        {
            return null;
        }

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            texture.width);

        Tile tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.color = Color.white;

        return tile;
    }

    public bool HasScarecrow()
    {
        foreach (KeyValuePair<Vector2Int, TileDetails> pair in tileDetailsDict)
        {
            if (pair.Value.hasScarecrow)
            {
                return true;
            }
        }

        return false;
    }

    public IEnumerable<TileDetails> GetAllTileDetails()
    {
        foreach (KeyValuePair<Vector2Int, TileDetails> pair in tileDetailsDict)
        {
            yield return pair.Value;
        }
    }

    public void WaterAllTiles()
    {
        ApplyRainWater();
    }

    public int WoodItemID
    {
        get { return treeDropItemID; }
    }

    public int StoneItemID
    {
        get { return stoneDropItemID; }
    }

    public int FiberItemID
    {
        get { return weedDropItemID; }
    }

    /// <summary>
    /// 刷新整个地图显示
    /// </summary>
    private void OnWeatherChangedEvent(WeatherType weather)
    {
        currentWeather = weather;

        float chance = WeatherEffects.GetAutoWaterChance(weather);

        if (chance > 0f && UnityEngine.Random.value <= chance)
        {
            ApplyRainWater();
        }

        if (WeatherEffects.IsRainy(weather) && UnityEngine.Random.value < 0.3f)
        {
            SpawnRainFind();
        }
    }

    private void SpawnRainFind()
    {
        if (CropManager.Instance == null)
        {
            return;
        }

        Vector3 spot = new Vector3(UnityEngine.Random.Range(-6f, 6f), UnityEngine.Random.Range(-4f, 4f), 0f);

        CropManager.Instance.SpawnFruit(43, spot);

        EventHandler.CallFarmEventEvent("雨后", "雨里好像冲来了什么好东西，在田里找找", false);
    }

    private void ApplyWeatherDamage(Season season)
    {
        if (CropManager.Instance == null)
        {
            return;
        }

        scarecrowSaves = 0;

        float strikeChance = WeatherEffects.GetStrikeChance(currentWeather, season);

        if (strikeChance <= 0f || UnityEngine.Random.value >= strikeChance)
        {
            return;
        }

        List<TileDetails> targets = new List<TileDetails>();

        foreach (var tile in tileDetailsDict)
        {
            if (tile.Value.seedItemID == -1 || tile.Value.inGreenhouse)
            {
                continue;
            }

            if (tile.Value.hasScarecrow)
            {
                scarecrowSaves++;
                continue;
            }

            if (!CropManager.Instance.IsCropMature(tile.Value))
            {
                continue;
            }

            targets.Add(tile.Value);
        }

        if (targets.Count == 0)
        {
            return;
        }

        CropManager.Instance.DestroyCrop(targets[UnityEngine.Random.Range(0, targets.Count)]);
    }

    private void ApplyRainWater()
    {
        foreach (var tile in tileDetailsDict)
        {
            if (tile.Value.daysSinceDug <= -1 || tile.Value.daysSinceWatered > -1)
            {
                continue;
            }

            tile.Value.daysSinceWatered = 0;
            tile.Value.isWatered = true;
            SetWaterGround(tile.Value);
        }
    }

    private void PlayActionSfx(ItemType type)
    {
        if (AudioManager.Instance == null)
        {
            return;
        }

        switch (type)
        {
            case ItemType.HoeTool:
                AudioManager.Instance.PlaySfx(SfxType.Dig);
                break;

            case ItemType.WaterCanTool:
                AudioManager.Instance.PlaySfx(SfxType.Water);
                break;

            case ItemType.Seed:
                AudioManager.Instance.PlaySfx(SfxType.Plant);
                break;

            case ItemType.SickleTool:
                AudioManager.Instance.PlaySfx(SfxType.Harvest);
                break;

            case ItemType.axeTool:
                AudioManager.Instance.PlaySfx(SfxType.Chop);
                break;

            case ItemType.pickaxeTool:
                AudioManager.Instance.PlaySfx(SfxType.Mine);
                break;

            case ItemType.Deed:
                AudioManager.Instance.PlaySfx(SfxType.Dig);
                break;
        }
    }

    private void RefreshMap()
    {
        CropManager cropManager = CropManager.Instance;

        if (cropManager != null)
        {
            cropManager.ClearAllCrops();
        }

        RefreshGroundTiles();

        // 重新显示所有地块和作物
        foreach (var tile in tileDetailsDict)
        {
            if (tile.Value.seedItemID != -1 && cropManager != null)
            {
                cropManager.RefreshCropVisual(tile.Value);
            }
        }

        // 刷新障碍物显示
        RefreshObstacleTiles();
    }

    private void RefreshGroundTiles()
    {
        if (digTilemap != null)
        {
            digTilemap.ClearAllTiles();
        }
        if (waterTilemap != null)
        {
            waterTilemap.ClearAllTiles();
        }

        foreach (var tile in tileDetailsDict)
        {
            if (tile.Value.daysSinceDug > -1)
            {
                SetDigGround(tile.Value);
            }
            if (tile.Value.daysSinceWatered > -1)
            {
                SetWaterGround(tile.Value);
            }
        }
    }

    /// <summary>
    /// 设置开垦地块显示
    /// </summary>
    private void SetDigGround(TileDetails tile)
    {
        Vector3Int pos = new Vector3Int(tile.girdX, tile.girdY, 0);
        if (digTilemap != null && digTile != null)
            digTilemap.SetTile(pos, digTile);
    }

    /// <summary>
    /// 设置浇水地块显示
    /// </summary>
    private void SetWaterGround(TileDetails tile)
    {
        Vector3Int pos = new Vector3Int(tile.girdX, tile.girdY, 0);
        if (waterTilemap != null && waterTile != null)
            waterTilemap.SetTile(pos, waterTile);
    }

    private void ClearDigTile(TileDetails tile)
    {
        Vector3Int pos = new Vector3Int(tile.girdX, tile.girdY, 0);

        if (digTilemap != null)
        {
            digTilemap.SetTile(pos, null);
        }
    }

    private void ClearWaterTile(TileDetails tile)
    {
        Vector3Int pos = new Vector3Int(tile.girdX, tile.girdY, 0);

        if (waterTilemap != null)
        {
            waterTilemap.SetTile(pos, null);
        }
    }

    /// <summary>
    /// 更新地块详情到字典
    /// </summary>
    private void UpdateTileDetails(TileDetails tileDetails)
    {
        Vector2Int key = new Vector2Int(tileDetails.girdX, tileDetails.girdY);
        if (tileDetailsDict.ContainsKey(key))
        {
            tileDetailsDict[key] = tileDetails;
        }
    }
    #endregion


    /// <summary>
    /// 获取所有土地数据
    /// </summary>
    public List<TileDetails> GetAllTileData()
    {
        List<TileDetails> result = new List<TileDetails>();
        foreach (var pair in tileDetailsDict)
        {
            result.Add(pair.Value);
        }
        return result;
    }

    /// <summary>
    /// 恢复土地数据
    /// </summary>
    public void RestoreTileData(List<TileDetails> tiles)
    {
        foreach (var tile in tiles)
        {
            Vector2Int key = new Vector2Int(tile.girdX, tile.girdY);
            if (tileDetailsDict.ContainsKey(key))
            {
                tileDetailsDict[key] = tile;
            }
        }
    }
}