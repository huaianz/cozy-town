using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 光标管理器 - 新增障碍物有效性校验
/// 功能：管理光标显示、工具有效性检查、鼠标交互
/// 【重要】实现工具-障碍物严格匹配校验
/// </summary>
public class CursorManager : MonoBehaviour
{
    [Header("光标精灵")]
    public Sprite normal;    
    public Sprite tool;       
    public Sprite seed;        
    public Sprite commodity;   

    [Header("光标状态颜色")]
    [Tooltip("操作有效时显示的颜色")]
    public Color validColor = Color.white;
    [Tooltip("操作无效时显示的颜色")]
    public Color invalidColor = Color.red;

    private Sprite currentSprite;           
    private Image cursorImage;             
    private RectTransform cursorCanvas;    


    private Camera mainCamera;             
    private Grid currentGrid;               
    private Vector3 mouseWorldPos;          
    private Vector3Int mouseGridPos;        

  
    private bool cursorEnable;              
    private bool cursorPositionValid;       
    private Item currentItem;              
    private bool cursorCacheValid;
    private Vector3Int cachedGridPos;
    private Item cachedItem;
    private static readonly Collider2D[] fruitOverlapBuffer = new Collider2D[16];


    private GridMapMangaer gridMapMangaer;
    private PlayerController playerController;  

    private void OnEnable()
    {
        EventHandler.ItemSelectEvent += OnItemSelectEvent;
        EventHandler.GameDayEvent += OnGameDayEvent;
    }

    private void OnDisable()
    {
        EventHandler.ItemSelectEvent -= OnItemSelectEvent;
        EventHandler.GameDayEvent -= OnGameDayEvent;

        Cursor.visible = true;
    }

    private void Start()
    {
        cursorCanvas = GameObject.FindGameObjectWithTag("CursorCanvas").GetComponent<RectTransform>();
        cursorImage = cursorCanvas.GetChild(0).GetComponent<Image>();
        currentSprite = normal;
        SetCursorImage(normal);

        mainCamera = Camera.main;
        currentGrid = FindObjectOfType<Grid>();
        gridMapMangaer = FindObjectOfType<GridMapMangaer>();
        playerController = FindObjectOfType<PlayerController>();

        Cursor.visible = false;
    }

    private void Update()
    {
        if (cursorCanvas == null) return;

        cursorImage.transform.position = PointerInput.Position;

        if (PointerInput.HasUsedTouch && cursorImage.enabled)
        {
            cursorImage.enabled = false;
        }

        if (!InteractWithUI() && cursorEnable)
        {
            SetCursorImage(currentSprite);
            CheckCursorValid();  // 校验当前操作是否有效
            CheckPlayerInput();  // 检测鼠标点击
        }
        else
        {
            SetCursorImage(normal);
        }
    }



    /// <summary>
    /// 检测鼠标点击，有效位置才触发事件
    /// </summary>
    private void CheckPlayerInput()
    {
        if (PointerInput.TappedThisFrame && AreaUnlockManager.Instance != null && AreaUnlockManager.Instance.BlockedNameAt(mouseWorldPos) != null)
        {
            AreaUnlockPanelUI.Open();
            cursorCacheValid = false;
            return;
        }

        if (PointerInput.TappedThisFrame && cursorPositionValid)
        {
            if (IsPointerOverFruit())
            {
                return;
            }

            EventHandler.CallMouseClickedEvent(mouseWorldPos, currentItem);
            cursorCacheValid = false;
        }
    }

    private bool IsPointerOverFruit()
    {
        int count = Physics2D.OverlapPointNonAlloc(mouseWorldPos, fruitOverlapBuffer);

        for (int i = 0; i < count; i++)
        {
            if (fruitOverlapBuffer[i].GetComponent<FruitPickup>() != null)
            {
                return true;
            }
        }

        return false;
    }
    


    /// <summary>
    /// 校验光标当前位置操作是否有效
    /// </summary>
    private void CheckCursorValid()
    {
        // 坐标转换
        Vector2 pointer = PointerInput.Position;
        mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(pointer.x, pointer.y, -mainCamera.transform.position.z));
        mouseGridPos = currentGrid.WorldToCell(mouseWorldPos);

        if (cursorCacheValid && mouseGridPos == cachedGridPos && cachedItem == currentItem)
        {
            return;
        }

        cachedGridPos = mouseGridPos;
        cachedItem = currentItem;
        cursorCacheValid = true;

        cursorPositionValid = false;

        if (AreaUnlockManager.Instance != null && AreaUnlockManager.Instance.BlockedNameAt(mouseWorldPos) != null)
        {
            cursorImage.color = invalidColor;
            return;
        }

        if (gridMapMangaer == null || currentItem == null)
            return;

        if (playerController != null && !playerController.HasStaminaFor(currentItem.itemType))
        {
            cursorPositionValid = false;
            cursorImage.color = invalidColor;

            return;
        }

        TileDetails tileDetails = gridMapMangaer.GetTileDetailsOnMousePosition(mouseGridPos);

        int pondItemID = currentItem.itemID;

        if (pondItemID == 51 || pondItemID == 52 || pondItemID == 53 || pondItemID == 54 || pondItemID == 55 || pondItemID == 57 || (pondItemID >= 61 && pondItemID <= 64))
        {
            cursorPositionValid = true;
            cursorImage.color = validColor;
            return;
        }

        switch (currentItem.itemType)
        {
            case ItemType.HoeTool:
               
                if (tileDetails != null &&
                    tileDetails.seedItemID == -1 &&
                    (tileDetails.canDig ||
                     (tileDetails.hasObstacle && tileDetails.obstacleType == ObstacleType.Weed)))
                {
                    cursorPositionValid = true;
                }
                break;

            case ItemType.Seed:
                bool seasonValid = CropManager.Instance.IsSeedSeasonAvailable(currentItem.itemID)
                    || (tileDetails != null && tileDetails.inGreenhouse);
     
                if (tileDetails != null &&
                    tileDetails.daysSinceDug > -1 &&
                    tileDetails.seedItemID == -1 &&
                    !tileDetails.hasObstacle &&  // 有障碍物不能播种
                    seasonValid)
                {
                    cursorPositionValid = true;
                }
                else
                {
                    cursorPositionValid = false;
                }
                break;
   
            case ItemType.WaterCanTool:

                if (WaterWellManager.Instance != null && WaterWellManager.Instance.IsWell(mouseWorldPos))
                {
                    cursorPositionValid = true;
                    break;
                }

                if (WaterWellManager.Instance != null && !WaterWellManager.Instance.HasWater)
                {
                    cursorPositionValid = true;
                    cursorImage.color = invalidColor;
                    return;
                }

                if (tileDetails != null &&
                    tileDetails.daysSinceDug > -1 &&
                    tileDetails.daysSinceWatered == -1 &&
                    tileDetails.seedItemID != -1 &&
                    !tileDetails.hasObstacle)  // 有障碍物不能浇水
                {
                    cursorPositionValid = true;
                }
                break;



            case ItemType.axeTool:
                if (tileDetails != null && FruitTreeManager.Instance != null && FruitTreeManager.Instance.HasTree(tileDetails.girdX, tileDetails.girdY))
                {
                    cursorPositionValid = true;
                    break;
                }
          
                if (tileDetails != null &&
                    tileDetails.hasObstacle &&
                    tileDetails.obstacleType == ObstacleType.Tree)
                {
                    cursorPositionValid = true;
                }
                else
                {
                    cursorPositionValid = false;
                }
                break;


            case ItemType.pickaxeTool:
                if (MineManager.Instance != null && MineManager.Instance.HasVeinAt(mouseWorldPos))
                {
                    cursorPositionValid = true;
                    break;
                }
            
                if (tileDetails != null &&
                    tileDetails.hasObstacle &&
                    tileDetails.obstacleType == ObstacleType.Rock)
                {
                    cursorPositionValid = true;
                }
                else
                {
                    cursorPositionValid = false;
                }
                break;
            

            case ItemType.SickleTool:
                if (GrassManager.Instance != null && GrassManager.Instance.HasGrassAt(mouseWorldPos))
                {
                    cursorPositionValid = true;
                    break;
                }
                if (tileDetails != null && FruitTreeManager.Instance != null && FruitTreeManager.Instance.HasTree(tileDetails.girdX, tileDetails.girdY))
                {
                    cursorPositionValid = true;
                    break;
                }
  
                if (tileDetails != null &&
                    tileDetails.seedItemID != -1 &&
                    !tileDetails.hasObstacle)  
                {
                    Crop crop = gridMapMangaer.GetCropObject(mouseWorldPos);
                    if (crop != null && crop.Canharvest)
                    {
                        cursorPositionValid = true;
                    }
                }
                break;

            case ItemType.Deed:
                if (tileDetails != null && !tileDetails.isUnlocked && Inventory.InventoryManager.Instance.GetItemAmountInBag(currentItem.itemID) > 0)
                {
                    cursorPositionValid = true;
                }
                break;

            case ItemType.Scarecrow:
                if (tileDetails != null && tileDetails.isUnlocked && !tileDetails.hasObstacle && !tileDetails.hasScarecrow && Inventory.InventoryManager.Instance.GetItemAmountInBag(currentItem.itemID) > 0)
                {
                    cursorPositionValid = true;
                }
                break;

            case ItemType.Greenhouse:
                if (tileDetails != null && tileDetails.isUnlocked && !tileDetails.hasObstacle && !tileDetails.inGreenhouse && Inventory.InventoryManager.Instance.GetItemAmountInBag(currentItem.itemID) > 0)
                {
                    cursorPositionValid = true;
                }
                break;

            case ItemType.FishingRod:
                cursorPositionValid = true;
                break;

            case ItemType.Product:
                if (currentItem.itemID == 31 && tileDetails != null && tileDetails.seedItemID != -1 && CropManager.Instance != null && !CropManager.Instance.IsCropMature(tileDetails))
                {
                    cursorPositionValid = true;
                    break;
                }

                cursorPositionValid = false;
                break;

            default:
                cursorPositionValid = false;
                break;
        }

        // 根据有效性设置光标颜色
        cursorImage.color = cursorPositionValid ? validColor : invalidColor;
    }


    /// <summary>
    /// 设置光标图像
    /// </summary>
    private void SetCursorImage(Sprite currentSprite)
    {
        cursorImage.sprite = currentSprite;
    }

    /// <summary>
    /// 检测是否与UI交互
    /// </summary>
    private bool InteractWithUI()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// 物品选择事件处理
    /// </summary>
    private void OnGameDayEvent(int day, Season season)
    {
        cursorCacheValid = false;
    }

    private void OnItemSelectEvent(Item item, bool isSelect)
    {
        cursorCacheValid = false;

        if (!isSelect)
        {
            currentItem = null;
            cursorEnable = false;
            currentSprite = normal;
        }
        else
        {
            currentItem = item;
            cursorEnable = true;
            currentSprite = item.itemType switch
            {
                ItemType.Seed => seed,
                ItemType.WaterCanTool => tool,
                ItemType.SickleTool => tool,
                ItemType.HoeTool => tool,
                ItemType.axeTool => tool,
                ItemType.pickaxeTool => tool,
                ItemType.Deed => commodity,
                ItemType.Product => commodity,
                _ => normal
            };
        }
    }
}