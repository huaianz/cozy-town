using Inventory;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class shopBuyItem : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("商店UI")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Button buyButton;

    [Header("长按设置")]
    [SerializeField] private float longPressDelay = 0.5f;//长按触发时间
    [SerializeField] private float longPressInterval = 0.5f;//连续购买间隔


    private ShopGoods goods;
    private Item item;
    private ItemTooltip tooltip;
    private ShopTabType currentTab;
    //存长按协程，用来停止
    private Coroutine longPressCoroutine;
    // 长按是否已经真正开始连续购买（用来拦住松手时那次多余的点击）
    private bool longPressTriggered;

    /// <summary>
    /// 初始化商品
    /// </summary>
    /// <param name="goods">商品</param>
    /// <param name="item">物品数据</param>
    /// <param name="tooltip">提示语</param>
    public void SetupBuyItem(ShopGoods goods,Item item,ItemTooltip tooltip)
    {
        this.goods = goods;
        this.item = item;
        this.tooltip = tooltip;
        currentTab = ShopTabType.Buy;

        UpdateUI();

        //停止之前的长按协程，防止复用时还在执行之前的协程
        if(longPressCoroutine!= null)
        {
            StopCoroutine(longPressCoroutine);
            longPressCoroutine = null;
        }

    }

    /// <summary>
    /// 初始化出售商品
    /// </summary>
    /// <param name="itemData"></param>
    /// <param name="tooltip"></param>
    public void SetupSellItem(Item itemData, ItemTooltip tooltip)
    {
        item = itemData;
        this.tooltip = tooltip;
        currentTab = ShopTabType.Sell;

        UpdateUI();

        //停止之前的长按协程，防止复用时还在执行之前的协程
        if (longPressCoroutine != null)
        {
            StopCoroutine(longPressCoroutine);
            longPressCoroutine = null;
        }
    }


    /// <summary>
    /// 更新商店UI显示
    /// </summary>

    private void UpdateUI()
    {
        if(item != null)
        {
            itemIcon.sprite = item.itemIcon;
            itemIcon.enabled = true;
            itemName.text = item.itemName;

            //不同模式显示不同价格
            if(currentTab==ShopTabType.Buy)
            {
                //价格使用的是商店售价，不是物品原价，商店可以单独定价
                priceText.text = goods.buyPrice.ToString();
            }
            else
            {
                //回收机设定为原价*出售折扣
                int sellPrice = (int)(item.itemPrice * item.sellPercentage);
                priceText.text = sellPrice.ToString();
            }
        }
        
        UpdateButtonState();
    }

    
    /// <summary>
    /// 更新购买按钮状态
    /// </summary>
    private void UpdateButtonState()
    {
        if(currentTab== ShopTabType.Buy)
        {
            buyButton.interactable = ShopManager.Instance.CanBuyItem(goods.itemId);
        }
        else
        {
            buyButton.interactable = InventoryManager.Instance.GetItemIndexInBag(item.itemID) != -1;
        }

    }


    /// <summary>
    /// 点击购买单个
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (longPressTriggered || longPressCoroutine != null)
        {
            longPressTriggered = false;
            return;
        }

        if (currentTab == ShopTabType.Buy)
        {
            BuySingle();
        }
        else
        {
            SellSingle();
        }
    }

    /// <summary>
    /// 按下开始检测长按
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerDown(PointerEventData eventData)
    {
        // 每次按下都先认为"长按还没触发"
        longPressTriggered = false;

        if (currentTab == ShopTabType.Buy)
        {
            longPressCoroutine = StartCoroutine(LongPressCoroutine());
        }
    }

    /// <summary>
    /// 悬停显示详情
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltip != null && item != null)
        {
            tooltip.SetupTooltip(item, SlotType.Shop);
            tooltip.gameObject.SetActive(true);
        }
    }

    /// <summary>
    ///  抬起结束长按
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerUp(PointerEventData eventData)
    {
        if (longPressCoroutine != null)
        {
            StopCoroutine(longPressCoroutine);
            longPressCoroutine = null;
        }
    }

    /// <summary>
    /// 离开关闭提示语
    /// </summary>
    /// <param name="eventData"></param>
    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        tooltip?.gameObject.SetActive(false);
    }

    /// <summary>
    /// 长按连续购买协程
    /// </summary>
    /// <returns></returns>
    private IEnumerator LongPressCoroutine()
    {
        yield return new WaitForSeconds(longPressDelay);
        longPressTriggered = true;
        while (true)
        {
            if(currentTab == ShopTabType.Buy && ShopManager.Instance != null)
            {
                if (ShopManager.Instance.BuyItem(goods.itemId, 1))
                {
                    UpdateButtonState();
                    yield return new WaitForSeconds(longPressInterval);
                }
                else
                {
                    UpdateButtonState();
                    break;
                }
            }
            else
            {
                break;
            }
        }
    }

    /// <summary>
    /// 购买单个
    /// </summary>
    private void BuySingle()
    {
        if (ShopManager.Instance.BuyItem(goods.itemId, 1))
        {
            UpdateButtonState();
        }
    }

    /// <summary>
    /// 单个回收
    /// </summary>
    private void SellSingle()
    {
        if(currentTab==ShopTabType.Sell && ShopManager.Instance != null)
        {
            if(ShopManager.Instance.SellItem(item.itemID, 1))
            {
                EventHandler.CallRefreshShopUIEvent();
                UpdateButtonState();
            }
        }
    }
    //防止切换分类的时候，按住的商品被隐藏了，协程还在后台一直买
    private void OnDisable()
    {
        if (longPressCoroutine != null)
        {
            StopCoroutine(longPressCoroutine);
            longPressCoroutine = null;
        }

        longPressTriggered = false;
    }

}
