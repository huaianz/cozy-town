using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using Inventory;
public class ShopUI : MonoBehaviour
{
    [Header("商店UI")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Transform goodsContent;
    [SerializeField] private GameObject shopBuyItemPrefab;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private ItemTooltip itemTooltip;

    [Header("分类按钮")]
    [SerializeField] private Button allBtn;
    [SerializeField] private Button seedBtn;
    [SerializeField] private Button toolBtn;
    [SerializeField] private Color selectedColor = Color.yellow;
    [SerializeField] private Color normalColor = Color.white;

    [Header("购买区和回收区按钮")]
    [SerializeField] private Button buyTabBtn;
    [SerializeField] private Button sellTabBtn;

    private ShopTabType currentTab = ShopTabType.Buy;

    private bool shopOpened = false;                    // 商店当前是否打开
    private ShopCategory currentCategory = ShopCategory.All;
    // 所有生成过的商品项,作为一个对象池
    private readonly List<shopBuyItem> _itemPool = new List<shopBuyItem>(); // 所有生成过的商品项
    private readonly List<shopBuyItem> _activeItems = new List<shopBuyItem>(); // 当前显示的商品项



    private void Start()
    {
        allBtn.onClick.AddListener(() => SwitchCategory(ShopCategory.All));
        seedBtn.onClick.AddListener(() => SwitchCategory(ShopCategory.Seed));
        toolBtn.onClick.AddListener(() => SwitchCategory(ShopCategory.Tool));

        buyTabBtn.onClick.AddListener(() => SwitchTab(ShopTabType.Buy));
        sellTabBtn.onClick.AddListener(() => SwitchTab(ShopTabType.Sell));

        //商店是否打开（看面板默认是否激活）
        shopOpened = shopPanel.activeInHierarchy;

        UpdateTabState();
        //更新按钮选中状态
        UpdateButtonState();
    }

    private void OnEnable()
    {
        EventHandler.UpdateMoneyEvent += OnMoneyUpdate;
    }

    private void OnDisable()
    {
        EventHandler.UpdateMoneyEvent -= OnMoneyUpdate;
    }

    private void OnMoneyUpdate(int money)
    {
        moneyText.text=money.ToString();
    }

    /// <summary>
    /// 打开/关闭商店
    /// </summary>
    public void ToggleShop()
    {
        shopOpened = !shopOpened;

        shopPanel.SetActive(shopOpened);

        if(shopOpened )
        {
            RefreshShop();
        }
        else
        {
            //关闭商店的时候顺便把提示语关闭
            itemTooltip?.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 切换商品分类
    /// </summary>
    /// <param name="category"></param>
    public void SwitchCategory(ShopCategory category)
    {
        if (currentCategory == category) return;
        currentCategory = category;
        UpdateButtonState();
        RefreshShop();
    }

    /// <summary>
    /// 更新按钮状态
    /// </summary>
    private void UpdateButtonState()
    {
        allBtn.image.color = currentCategory == ShopCategory.All ? selectedColor : normalColor;
        seedBtn.image.color = currentCategory == ShopCategory.Seed ? selectedColor : normalColor;
        toolBtn.image.color = currentCategory == ShopCategory.Tool ? selectedColor : normalColor;
    }

    /// <summary>
    /// 刷新商店UI
    /// </summary>
    private void RefreshShop()
    {
        //隐藏当前显示的商品
        foreach (var item in _activeItems)
        {
            item.gameObject.SetActive(false);
        }
        _activeItems.Clear();

        if (currentTab == ShopTabType.Buy)
        {
            //拿到当前显示的商品列表
            List<ShopGoods> goodsList = ShopManager.Instance.GetShopGoodsByCategory(currentCategory);

            foreach (var goods in goodsList)
            {
                //从池子里那一个隐藏的商品项
                shopBuyItem item = GetPooledItem();


                Item itemData = InventoryManager.Instance.GetItem(goods.itemId);

                item.SetupBuyItem(goods, itemData, itemTooltip);

                item.gameObject.SetActive(true);
                _activeItems.Add(item);
            }
        }
        else
        {
            List<InventoryItem> bagItems = InventoryManager.Instance.playerBag.BagList;
            foreach (var bagItem in bagItems)
            {
                if (bagItem.itemID == 0) continue; // 跳过空格子

                Item itemData = InventoryManager.Instance.GetItem(bagItem.itemID);
                if (itemData == null) continue; // 跳过不能卖的物品
                if (ShopManager.IsTool(itemData.itemType) || itemData.itemType == ItemType.Deed) continue;

                shopBuyItem item = GetPooledItem();

                item.SetupSellItem(itemData, itemTooltip);
                item.gameObject.SetActive(true);
                _activeItems.Add(item);
            }
        }
    }

    private shopBuyItem GetPooledItem()
    {
        foreach(var item in _itemPool)
        {
            //未被激活
            if (!item.gameObject.activeSelf)
            {
                return item;
            }
        }

        GameObject newItem = Instantiate(shopBuyItemPrefab, goodsContent);
        shopBuyItem buyItem = newItem.GetComponent<shopBuyItem>();
        _itemPool.Add(buyItem);
        return buyItem;
    }


    /// <summary>
    /// 切换购买/回收Tab
    /// </summary>
    public void SwitchTab(ShopTabType tab)
    {
        if (currentTab == tab) return;
        currentTab = tab;
        UpdateTabState();
        RefreshShop(); // 切换Tab立刻刷新内容
    }

    /// <summary>
    /// 更新购买区/回收区选中状态
    /// </summary>
    private void UpdateTabState()
    {
        buyTabBtn.image.color = currentTab == ShopTabType.Buy ? selectedColor : normalColor;
        sellTabBtn.image.color = currentTab == ShopTabType.Sell ? selectedColor : normalColor;

        // 回收区隐藏分类按钮（种子/工具/全部），购买区显示
        allBtn.gameObject.SetActive(currentTab == ShopTabType.Buy);
        seedBtn.gameObject.SetActive(currentTab == ShopTabType.Buy);
        toolBtn.gameObject.SetActive(currentTab == ShopTabType.Buy);
    }
}
