using System;
using System.Collections;                        
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;              
using Inventory;

public class SaveLoadManager : Singleton<SaveLoadManager>
{
    [Header("存档设置")]
    [Tooltip("存档文件名")]
    public string saveFileName = "farmGameSave.json";
    [Tooltip("是否启用自动保存")]
    public bool enableAutoSave = true;
    [Tooltip("自动保存间隔(秒)")]
    public float autoSaveInterval = 300f;

    //用来记录当前是新游戏还是继续游戏
    public static bool IsNewGameStart { get; private set; } = false;
    private string SaveFilePath => SaveStorage.Current.GetLocation(saveFileName);
    private float autoSaveTimer;

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {

        if (enableAutoSave)
        {
            autoSaveTimer += Time.deltaTime;
            if (autoSaveTimer >= autoSaveInterval)
            {
                AutoSave();
                autoSaveTimer = 0f;
            }
        }
    }

    private void OnEnable()
    {
        // 监听应用退出事件，退出时自动保存
        Application.quitting += OnApplicationQuit;
    }

    private void OnDisable()
    {
        Application.quitting -= OnApplicationQuit;
    }

    /// <summary>
    /// 应用退出时自动保存
    /// </summary>
    private void OnApplicationQuit()
    {
        if (enableAutoSave)
        {
            SaveGame();
        }
    }


    /// <summary>
    /// 保存游戏
    /// </summary>
    public void SaveGame()
    {
        TimeManager timeManager = FindObjectOfType<TimeManager>();

        if (GridMapMangaer.Instance == null || timeManager == null)
        {
            Debug.LogWarning("当前不在游戏场景，跳过保存");
            return;
        }

        try
        {
            SaveData saveData = new SaveData();

            if (ShopManager.Instance != null)
            {
                saveData.playerMoney = ShopManager.Instance.PlayerMoney;
                saveData.commissions = ShopManager.Instance.GetCommissions();
            }

            PlayerController playerController = FindObjectOfType<PlayerController>();

            if (playerController != null)
            {
                saveData.playerStamina = playerController.CurrentStamina;
            }

            if (InventoryManager.Instance != null &&
                InventoryManager.Instance.playerBag != null)
            {
                foreach (var item in InventoryManager.Instance.playerBag.BagList)
                {
                    if (item.itemID > 0 && item.itemAmount > 0)
                    {
                        saveData.inventoryItems.Add(
                            new SerializableInventoryItem(item.itemID, item.itemAmount)
                        );
                    }
                }
            }

            timeManager.GetTimeData(out int second, out int minute, out int hour,
                                    out int day, out int month, out int year, out Season season);

            saveData.gameSecond = second;
            saveData.gameMinute = minute;
            saveData.gameHour = hour;
            saveData.gameDay = day;
            saveData.gameMonth = month;
            saveData.gameYear = year;
            saveData.gameSeason = (int)season;
            saveData.weatherState = (int)timeManager.GetWeather();


            GridMapMangaer gridManager = FindObjectOfType<GridMapMangaer>();
            if (gridManager != null)
            {
                foreach (var tile in gridManager.GetAllTileData())
                {
                    saveData.tileDataList.Add(SerializableTileData.FromTileDetails(tile));
                }
            }

            foreach (var fruit in FindObjectsOfType<FruitPickup>())
            {
                saveData.sceneItems.Add(new SceneItem
                {
                    itemID = fruit.fruitItemID,
                    position = new SerializableVector3(fruit.transform.position)
                });
            }

            string json = JsonUtility.ToJson(saveData, true);

            SaveStorage.Current.Write(saveFileName, json);

        }
        catch (Exception e)
        {
            Debug.LogError($"保存游戏失败: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// 自动保存
    /// </summary>
    private void AutoSave()
    {
        SaveGame();
    }

    /// <summary>
    /// 加载游戏
    /// 从JSON文件读取数据并恢复游戏状态
    /// </summary>
    /// <returns>加载是否成功</returns>
    public bool LoadGame()
    {
        if (!HasSaveFile())
        {
            Debug.LogWarning("未找到存档文件，使用新游戏数据");
            return false;
        }

        try
        {
            string json = SaveStorage.Current.Read(saveFileName);
            SaveData saveData = JsonUtility.FromJson<SaveData>(json);

            if (saveData == null)
            {
                Debug.LogError("存档数据解析失败");
                return false;
            }

            //第一个检测点
            if(ShopManager.Instance == null)
            {
                Debug.Log("商店管理器未加载");
            }



            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.PlayerMoney = saveData.playerMoney;
                ShopManager.Instance.SetCommissions(saveData.commissions);
                EventHandler.CallUpdateMoneyEvent(saveData.playerMoney);
            }

            PlayerController playerController = FindObjectOfType<PlayerController>();

            if (playerController != null)
            {
                if (saveData.playerStamina > 0)
                {
                    playerController.SetStamina(saveData.playerStamina);
                }
                else
                {
                    playerController.RestoreStamina();
                }
            }

            //第二个检测点
            if (InventoryManager.Instance == null)
            {
                Debug.Log("背包管理器未加载");
            }

            if (InventoryManager.Instance != null &&
                InventoryManager.Instance.playerBag != null)
            {
                // 先清空背包
                for (int i = 0; i < InventoryManager.Instance.playerBag.BagList.Count; i++)
                {
                    InventoryManager.Instance.playerBag.BagList[i] = new InventoryItem();
                }

                // 恢复物品
                foreach (var serialItem in saveData.inventoryItems)
                {
                    InventoryManager.Instance.AddItem(
                        serialItem.itemID,
                        serialItem.itemAmount
                    );
                }
            }
            TimeManager timeManager = FindObjectOfType<TimeManager>();
            if (timeManager != null)
            {
                RestoreTimeData(timeManager, saveData);
            }

            GridMapMangaer gridManager = FindObjectOfType<GridMapMangaer>();
            if (gridManager != null)
            {
                gridManager.InitializeMap();

                if (saveData.tileDataList != null)
                {
                    bool legacySave = saveData.saveVersion < 2;
                    List<TileDetails> tiles = new List<TileDetails>();

                    foreach (var serialTile in saveData.tileDataList)
                    {
                        TileDetails tile = serialTile.ToTileDetails();

                        if (legacySave)
                        {
                            tile.isUnlocked = true;
                        }

                        tiles.Add(tile);
                    }

                    gridManager.RestoreTileData(tiles);
                }
            }

            EventHandler.CallRefreshCurrentMap();

            if (CropManager.Instance != null && saveData.sceneItems != null)
            {
                CropManager.Instance.ClearAllFruits();

                foreach (var sceneItem in saveData.sceneItems)
                {
                    if (sceneItem.position == null)
                    {
                        continue;
                    }

                    CropManager.Instance.SpawnFruit(sceneItem.itemID, sceneItem.position.ToVector3());
                }
            }

            // 强制触发金钱更新事件
            if (ShopManager.Instance != null)
            {
                EventHandler.CallUpdateMoneyEvent(ShopManager.Instance.PlayerMoney);
            }

            // 强制触发背包UI更新
            if (InventoryManager.Instance != null)
            {
                EventHandler.CallUpdaeInventoryUI(InventoryLocation.Player,
                    InventoryManager.Instance.playerBag.BagList);
            }
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"加载游戏失败: {e.Message}\n{e.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// 恢复时间系统数据
    /// </summary>
    private void RestoreTimeData(TimeManager timeManager, SaveData saveData)
    {
        timeManager.SetTimeData(
            saveData.gameSecond,
            saveData.gameMinute,
            saveData.gameHour,
            saveData.gameDay,
            saveData.gameMonth,
            saveData.gameYear,
            (Season)saveData.gameSeason
        );

        // 把季节直接告诉作物系统。
        // 存档要是夏/秋/冬，就会种不了当季种子，要等到第二天才自己恢复。
        if (CropManager.Instance != null)
        {
            CropManager.Instance.SetSeason((Season)saveData.gameSeason);
        }

        timeManager.SetWeather((WeatherType)saveData.weatherState);
    }


    /// <summary>
    /// 检查是否存在存档文件
    /// </summary>
    public bool HasSaveFile()
    {
        return SaveStorage.Exists(saveFileName);
    }

    /// <summary>
    /// 删除存档文件
    /// </summary>
    public void DeleteSave()
    {
        SaveStorage.Current.Delete(saveFileName);
    }

    /// <summary>
    /// 立即手动保存
    /// </summary>
    public void ManualSave()
    {
        SaveGame();
    }

    /// <summary>
    /// 重置自动保存计时器
    /// </summary>
    public void ResetAutoSaveTimer()
    {
        autoSaveTimer = 0f;
    }
    /// <summary>
    /// 新游戏流程入口（由主菜单调用）
    /// </summary>
    public void StartNewGame(string sceneName)
    {
        StartCoroutine(NewGameFlow(sceneName));
    }

    /// <summary>
    /// 继续游戏流程入口（由主菜单调用）
    /// </summary>
    public void StartContinueGame(string sceneName)
    {
        StartCoroutine(ContinueGameFlow(sceneName));
    }

    private IEnumerator NewGameFlow(string sceneName)
    {
        //清掉旧存档
        DeleteSave();

        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

        yield return null;

        InitGame(true);

        Debug.Log("【新游戏】启动完成");
    }

    private IEnumerator ContinueGameFlow(string sceneName)
    {
        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        yield return null;

        InitGame(false);

        Debug.Log("【继续游戏】启动完成");
    }
    /// <summary>
    /// 游戏初始化入口
    /// </summary>
    /// <param name="isNewGame">判断是新游戏还是继续游戏</param>
    public void InitGame(bool isNewGame)
    {
        IsNewGameStart = isNewGame;
        ResetAutoSaveTimer();
        if (isNewGame)
        {
            //执行新游戏初始化
            InitNewGame();
        }
        else
        {
            LoadGame();
        }
    }

    private void InitNewGame()
    {
        //初始化时间系统
        TimeManager timeManager = FindObjectOfType<TimeManager>();
        if (timeManager == null)
        {
            Debug.Log("初始化时间未完成");
        }
        if (timeManager != null)
        {
            timeManager.NewGameTime();

            // 触发时间事件
            timeManager.GetTimeData(out int sec, out int min, out int hour, out int day, out int month, out int year, out Season season);
            EventHandler.CallGameDateEvent(hour, day, month, year, season);
            EventHandler.CallGameMinuteEvent(min, hour);
            Debug.Log("初始化时间完成");
        }
        //初始化金钱
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.PlayerMoney = 1000;
            EventHandler.CallUpdateMoneyEvent(1000);
            Debug.Log("初始化金钱完成");
        }
        //初始化地图
        GridMapMangaer gridManager = FindObjectOfType<GridMapMangaer>();
        if (gridManager != null)
        {
            gridManager.InitializeMap();
            Debug.Log("初始化地图完成");
        }
        //初始化背包
        if (InventoryManager.Instance != null)
        {
            EventHandler.CallUpdaeInventoryUI(InventoryLocation.Player, InventoryManager.Instance.playerBag.BagList);
        }
    }
}