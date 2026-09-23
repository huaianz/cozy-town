using Inventory;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class FruitPickup : MonoBehaviour
{
    [Header("果实交互")]
    public int fruitItemID;
    public bool playPickupAnimation = true;
    public float pickupAnimationDuration = 0.2f;

    //是否使用碰撞体检测点击
    public bool useColliderDetection = true;

    //果实是否正在被拾取
    private bool isBeingPickUp=false;
    private SpriteRenderer spriteRenderer;
    private static readonly Dictionary<GameObject, Stack<FruitPickup>> pools = new Dictionary<GameObject, Stack<FruitPickup>>();
    private GameObject sourcePrefab;
    private Color originalColor = Color.white;

    private void Awake()
    {
        spriteRenderer=GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }


    /// <summary>
    /// 初始化果实数据
    /// </summary>
    /// <param name="itemID">果实对应的物品ID</param>
    public void Initialize(int itemID)
    {
        fruitItemID = itemID;
        UpdateFruitVisual();
    }

    /// <summary>
    /// 更新果实外观
    /// </summary>
    private void UpdateFruitVisual()
    {
        if (InventoryManager.Instance != null)
        {
            Item item = InventoryManager.Instance.GetItem(fruitItemID);
            if (item != null&&spriteRenderer!=null)
            {
                if(item.itemOnWorldSprite!=null)
                {
                    spriteRenderer.sprite = item.itemOnWorldSprite;
                }
                else if(item.itemIcon!=null) 
                {
                    spriteRenderer.sprite= item.itemIcon;
                }
            }
        }
    }

    public static FruitPickup Spawn(GameObject prefab, int itemID, Vector3 position)
    {
        if (prefab == null)
        {
            return null;
        }

        FruitPickup instance = TakeFromPool(prefab);

        if (instance == null)
        {
            GameObject fruitObject = Instantiate(prefab, position, Quaternion.identity);
            instance = fruitObject.GetComponent<FruitPickup>();

            if (instance == null)
            {
                Destroy(fruitObject);
                return null;
            }
        }

        instance.sourcePrefab = prefab;
        instance.transform.position = position;
        instance.transform.localScale = Vector3.one;
        instance.isBeingPickUp = false;

        if (instance.spriteRenderer != null)
        {
            instance.spriteRenderer.color = instance.originalColor;
        }

        instance.gameObject.SetActive(true);
        instance.Initialize(itemID);

        return instance;
    }

    private static FruitPickup TakeFromPool(GameObject prefab)
    {
        Stack<FruitPickup> stack;

        if (!pools.TryGetValue(prefab, out stack))
        {
            return null;
        }

        while (stack.Count > 0)
        {
            FruitPickup candidate = stack.Pop();

            if (candidate != null)
            {
                return candidate;
            }
        }

        return null;
    }

    public void ReleaseToPool()
    {
        if (sourcePrefab == null)
        {
            Destroy(gameObject);
            return;
        }

        Stack<FruitPickup> stack;

        if (!pools.TryGetValue(sourcePrefab, out stack))
        {
            stack = new Stack<FruitPickup>();
            pools[sourcePrefab] = stack;
        }

        gameObject.SetActive(false);
        stack.Push(this);
    }

    //鼠标点击检测
    private void OnMouseDown()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if(useColliderDetection)
        {
            PickupFruit();
        }
    }

    /// <summary>
    /// 捡起果实到背包
    /// </summary>
    public void Collect()
    {
        PickupFruit();
    }

    private void PickupFruit()
    {
        if (isBeingPickUp)
        {
            return;
        }

        if (InventoryManager.Instance.AddItem(fruitItemID, 1) <= 0)
        {
            Debug.LogWarning("背包已满，果实留在地上");
            return;
        }

        isBeingPickUp= true;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(SfxType.Pickup);
        }

        if (playPickupAnimation)
        {
            StartCoroutine(PickupAnimationCoroutine());
        }
        else
        {
            ReleaseToPool();
        }
    }


    private IEnumerator PickupAnimationCoroutine()
    {
        // 记录初始缩放
        Vector3 originalScale = transform.localScale;
        // 记录初始位置
        Vector3 originalPosition = transform.position;
        float elapsedTime = 0f;
        while (elapsedTime < pickupAnimationDuration)
        {
            // 计算动画进度（0-1）
            float progress = elapsedTime / pickupAnimationDuration;

            // 缩放效果：先放大再缩小
            float scaleFactor = 1f + Mathf.Sin(progress * Mathf.PI) * 0.3f;
            transform.localScale = originalScale * scaleFactor;

            // 向上浮动效果
            transform.position = originalPosition + new Vector3(0, progress * 0.5f, 0);

            // 渐隐效果
            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = 1f - progress;
                spriteRenderer.color = color;
            }

            // 累加时间
            elapsedTime += Time.deltaTime;
            // 等待下一帧
            yield return null;
        }

        ReleaseToPool();
    }


}
