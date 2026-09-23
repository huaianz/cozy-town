using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Inventory
{
    public class ShopManager : Singleton<ShopManager>
    {
        [Header("商店数据")]
        public ShopData_SO shopdData;

        [Header("玩家初始金币")]
        [SerializeField] private int playerMoney = 1000;

        [Header("每日委托")]
        [SerializeField] private int dailyCommissionCount = 2;
        [SerializeField] private int minCommissionAmount = 2;
        [SerializeField] private int maxCommissionAmount = 5;
        [SerializeField] private float commissionRewardMultiplier = 1.2f;
        [SerializeField] private KeyCode deliverKey = KeyCode.E;

        private readonly List<CommissionData> commissions = new List<CommissionData>();
        private const int MaxActiveCommissions = 4;
        private int lastExpiredCount;

        public int LastExpiredCommissions
        {
            get { return lastExpiredCount; }
        }

        public int PlayerMoney
        {
            get => playerMoney;
            set
            {
                playerMoney = value;
                //更新金钱UI
            }
        }

        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            GenerateCommissions();

            //if (SaveLoadManager.Instance != null && !SaveLoadManager.Instance.HasSaveFile())
            //{
            //    // 新游戏初始化默认金钱
            //    PlayerMoney = 1000;
            //    EventHandler.CallUpdateMoneyEvent(playerMoney);
            //    Debug.Log("开始新游戏");
            //}

        }

        /// <summary>
        /// 按照分类获取商品列表
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        private void OnEnable()
        {
            EventHandler.GameDayEvent += OnGameDayEvent;
        }

        private void OnDisable()
        {
            EventHandler.GameDayEvent -= OnGameDayEvent;
        }

        private void OnGameDayEvent(int day, Season season)
        {
            AdvanceCommissions();
        }

        private void Update()
        {
            if (Input.GetKeyDown(deliverKey))
            {
                TryDeliverCommissions();
            }
        }

        public List<CommissionData> GetCommissions()
        {
            return commissions;
        }

        public void SetCommissions(List<CommissionData> data)
        {
            if (data == null || data.Count == 0)
            {
                return;
            }

            commissions.Clear();

            for (int i = 0; i < data.Count; i++)
            {
                CommissionData commission = data[i];

                if (commission.daysLeft <= 0)
                {
                    commission.daysLeft = 3;
                }

                commissions.Add(commission);
            }

            EventHandler.CallCommissionChangedEvent();
        }

        public bool HasFulfillableCommission()
        {
            if (InventoryManager.Instance == null)
            {
                return false;
            }

            for (int i = 0; i < commissions.Count; i++)
            {
                CommissionData commission = commissions[i];

                if (commission.delivered)
                {
                    continue;
                }

                if (InventoryManager.Instance.GetItemAmountInBag(commission.itemID) >= commission.amount)
                {
                    return true;
                }
            }

            return false;
        }

        private void GenerateCommissions()
        {
            commissions.Clear();
            lastExpiredCount = 0;

            for (int i = 0; i < dailyCommissionCount; i++)
            {
                CommissionData commission = RollCommission();

                if (commission == null)
                {
                    break;
                }

                commissions.Add(commission);
            }

            EventHandler.CallCommissionChangedEvent();
        }

        private void AdvanceCommissions()
        {
            lastExpiredCount = 0;

            for (int i = commissions.Count - 1; i >= 0; i--)
            {
                CommissionData commission = commissions[i];

                if (commission.delivered)
                {
                    commissions.RemoveAt(i);
                    continue;
                }

                commission.daysLeft--;

                if (commission.daysLeft <= 0)
                {
                    commissions.RemoveAt(i);
                    lastExpiredCount++;
                }
            }

            int missing = dailyCommissionCount - commissions.Count;

            for (int i = 0; i < missing; i++)
            {
                CommissionData commission = RollCommission();

                if (commission == null)
                {
                    break;
                }

                commissions.Add(commission);
            }

            if (commissions.Count > MaxActiveCommissions)
            {
                commissions.RemoveRange(MaxActiveCommissions, commissions.Count - MaxActiveCommissions);
            }

            if (lastExpiredCount > 0)
            {
                EventHandler.CallFarmEventEvent("委托", lastExpiredCount + " 个委托过期了，今天来了新委托", true);
            }

            EventHandler.CallCommissionChangedEvent();
        }

        private CommissionData RollCommission()
        {
            if (InventoryManager.Instance == null || InventoryManager.Instance.itemData == null || CropManager.Instance == null)
            {
                return null;
            }

            Season season = CropManager.Instance.GetCurrentSeason();
            List<Item> candidates = new List<Item>();

            foreach (var item in InventoryManager.Instance.itemData.Items)
            {
                if (item.itemType != ItemType.Product || item.itemPrice <= 0)
                {
                    continue;
                }

                if (!CropManager.Instance.CanObtainProduct(item.itemID, season))
                {
                    continue;
                }

                bool already = false;

                for (int i = 0; i < commissions.Count; i++)
                {
                    if (commissions[i].itemID == item.itemID)
                    {
                        already = true;
                        break;
                    }
                }

                if (!already)
                {
                    candidates.Add(item);
                }
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            Item picked = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            int amount = UnityEngine.Random.Range(minCommissionAmount, maxCommissionAmount + 1);

            CommissionData commission = new CommissionData();
            commission.itemID = picked.itemID;
            commission.amount = amount;
            commission.reward = Mathf.RoundToInt(picked.itemPrice * amount * commissionRewardMultiplier);
            commission.delivered = false;
            commission.daysLeft = UnityEngine.Random.Range(1, 4);

            Debug.Log("[委托] " + picked.itemName + "×" + amount + "，报酬 " + commission.reward + "，" + commission.daysLeft + " 天内");

            return commission;
        }

        public void TryDeliverCommissions()
        {
            if (InventoryManager.Instance == null)
            {
                return;
            }

            bool deliveredAny = false;

            for (int i = 0; i < commissions.Count; i++)
            {
                CommissionData commission = commissions[i];

                if (commission.delivered)
                {
                    continue;
                }

                if (InventoryManager.Instance.GetItemAmountInBag(commission.itemID) < commission.amount)
                {
                    continue;
                }

                InventoryManager.Instance.RemoveItem(commission.itemID, commission.amount);
                commission.delivered = true;
                playerMoney += commission.reward;
                ProgressManager.RegisterEvent("交付");
                deliveredAny = true;
            }

            if (!deliveredAny)
            {
                Debug.LogWarning("当前没有可以交付的委托");
                return;
            }

            EventHandler.CallUpdateMoneyEvent(PlayerMoney);
            EventHandler.CallCommissionChangedEvent();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySfx(SfxType.Deliver);
            }
        }

        public List<ShopGoods> GetShopGoodsByCategory(ShopCategory category)
        {
            if(shopdData == null)
            {
                return new List<ShopGoods>();
            }

            return category == ShopCategory.All ? shopdData.shopGoodsList.FindAll(g => g.isOnSale) : shopdData.shopGoodsList.FindAll(g => g.isOnSale && g.cate == category);
        }
        /// <summary>
        /// 购买商品
        /// </summary>
        /// <param name="itemID">物品ID</param>
        /// <param name="amount">购买数量</param>
        /// <returns></returns>
        public bool BuyItem(int itemID,int amount = 1)
        {

            ShopGoods goods = FindGoods(itemID);
            if (!goods.isOnSale)
            {
                return false;
            }

            Item item = InventoryManager.Instance.GetItem(itemID);

            if (item == null)
            {
                return false;
            }

            int totalPrice=goods.buyPrice*amount;
            if (PlayerMoney < totalPrice)
            {
                return false;
            }


            if (!CanAddItemToBag(itemID, amount))
            {
                return false;
            }

            playerMoney-=totalPrice;
            InventoryManager.Instance.AddItem(itemID, amount);
            EventHandler.CallUpdateMoneyEvent(PlayerMoney);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySfx(SfxType.Buy);
            }

            return true;

        }

        /// <summary>
        /// 检查背包能不能放心指定数量的物品
        /// </summary>
        /// <param name="itemID"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        private bool CanAddItemToBag(int itemID, int amount)
        {
            Item item =InventoryManager.Instance.GetItem(itemID);
            int maxStack=item.maxStack;
            int remaining = amount;

            //先检查已有的物品的剩余空间能否装下购买的产品数
            foreach(var slot in InventoryManager.Instance.playerBag.BagList)
            {
                if(slot.itemID == itemID)
                {
                    int freeSpace=maxStack-slot.itemAmount;
                    remaining -= freeSpace;
                    if (remaining <= 0)
                        return true;
                }
            }

            //向上取整，看剩余多少格子
            int needSlots = Mathf.CeilToInt((float)remaining / maxStack);

            int emptySlots = 0;
            foreach(var slot in InventoryManager.Instance.playerBag.BagList)
            {
                if(slot.itemID==0)
                    emptySlots++;
            }

            return emptySlots >= needSlots;
        }


        /// <summary>
        /// 出售背包里的物品给商店
        /// </summary>
        /// <param name="itemID">物品ID</param>
        /// <param name="amount">出售的数量</param>
        /// <returns></returns>
    private int CurrentDay
    {
        get
        {
            TimeManager timeManager = FindObjectOfType<TimeManager>();

            if (timeManager == null)
            {
                return 0;
            }

            int second;
            int minute;
            int hour;
            int day;
            int month;
            int year;
            Season season;

            timeManager.GetTimeData(out second, out minute, out hour, out day, out month, out year, out season);

            return day;
        }
    }

    public bool SellItem(int itemID,int amount = 1)
        {
            Item item=InventoryManager.Instance.GetItem(itemID);
            if (item == null || IsTool(item.itemType) || item.itemType == ItemType.Deed)
            {
                return false;
            }

            int itemIndex = InventoryManager.Instance.GetItemIndexInBag(itemID);
            if (itemIndex == -1)
            {
                return false;
            }

            int currentAmount = InventoryManager.Instance.playerBag.BagList[itemIndex].itemAmount;

            if (currentAmount < amount)
            {
                return false;
            }

            int sellPrice = (int)(item.itemPrice * item.sellPercentage);
            int totalMoney = Mathf.RoundToInt(sellPrice * amount * MarketDay.GetPriceMultiplier(CurrentDay) * ProgressManager.GetSellMultiplier());

            playerMoney += totalMoney;
            InventoryManager.Instance.RemoveItem(itemID,amount);
            EventHandler.CallUpdateMoneyEvent(PlayerMoney);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySfx(SfxType.Sell);
            }

            return true;

        }



        private ShopGoods FindGoods(int itemID)
        {
            if (shopdData == null || shopdData.shopGoodsList == null)
            {
                return default(ShopGoods);
            }

            for (int i = 0; i < shopdData.shopGoodsList.Count; i++)
            {
                if (shopdData.shopGoodsList[i].itemId == itemID)
                {
                    return shopdData.shopGoodsList[i];
                }
            }

            return default(ShopGoods);
        }

        public static bool IsTool(ItemType type)
        {
            return type == ItemType.axeTool
                || type == ItemType.HoeTool
                || type == ItemType.WaterCanTool
                || type == ItemType.SickleTool
                || type == ItemType.pickaxeTool;
        }

        /// <summary>
        /// 检查是否可以购买
        /// </summary>
        /// <param name="itemID">物品ID</param>
        /// <param name="amount">购买数量</param>
        /// <returns></returns>
        public bool CanBuyItem(int itemID,int amount = 1)
        {
            ShopGoods good = FindGoods(itemID);
            return good.isOnSale&&playerMoney>=good.buyPrice*amount;
        }
    }
}
