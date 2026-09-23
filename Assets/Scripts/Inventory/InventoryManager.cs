using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;

//作为一个库函数，防止互相乱调用的藕合情况
namespace Inventory
{

    public class InventoryManager : Singleton<InventoryManager>
    {
        [Header("物品数据")]
        public ItemData_SO itemData;
        [Header("背包数据")]
        public InventoryBag_SO playerBag;

        private void Start()
        {
            EventHandler.CallUpdaeInventoryUI(InventoryLocation.Player, playerBag.BagList);
        }

        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);
        }
        /// <summary>
        /// 根据ID返回物品信息
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        public Item GetItem(int ID)
        {
            if (itemData == null)
            {
                Debug.LogError("物品数据未初始化");
                return null;
            }

            if(itemData.Items==null)
            {
                Debug.LogError("物品列表为空");
                return null;
            }

            for (int i = 0; i < itemData.Items.Count; i++)
            {
                if (itemData.Items[i].itemID == ID)
                {
                    return itemData.Items[i];
                }
            }

            return null;

        }

        /// <summary>
        /// 通过物品的ID获取背包里已有物品的位置
        /// </summary>
        /// <param name="ID">物品ID</param>
        /// <returns></returns>
        public int GetItemIndexInBag(int ID)
        {
            for (int i = 0; i < playerBag.BagList.Count; i++)
            {
                if (playerBag.BagList[i].itemID == ID)
                    return i;
            }
            return -1;
        }


        /// <summary>
        /// 背包添加物品
        /// </summary>
        /// <param name="ID">物品ID</param>
        /// <param name="index">当前索引</param>
        /// <param name="amount">添加数量</param>
        public int AddItem(int ID,int amount)
        {
            if (amount <= 0)
                return 0;
            
            Item item=GetItem(ID);
            AlmanacManager.Discover(ID);
            if (item == null)
            {
                return 0;
            }

            int maxStack=item.maxStack;
            //剩余要添加的数量
            int remainingAmount=amount;

            //先把有相应物品的格子堆满
            for(int i=0;i<playerBag.BagList.Count;i++)
            {
                if (remainingAmount <= 0) break;
                InventoryItem slot=playerBag.BagList[i];
                if(slot.itemID == ID && slot.itemAmount < maxStack)
                {
                    int canAdd=maxStack-slot.itemAmount;
                    int addThisTime=Mathf.Min(canAdd, remainingAmount);

                    playerBag.BagList[i] = new InventoryItem(ID, slot.itemAmount + addThisTime);
                    remainingAmount-=addThisTime;   

                }
            }

            //接下来把剩下的放到空格子里
            while (remainingAmount > 0)
            {
                int emptySlotIndex = -1;
                for(int i=0;i<playerBag.BagList.Count; i++)
                {
                    if (playerBag.BagList[i].itemID == 0)
                    {
                        emptySlotIndex = i;
                        break;
                    }
                }
                if (emptySlotIndex == -1)
                {
                    //这就说明背包满了
                    break;
                }

                int addThisTime=Mathf.Min(maxStack, remainingAmount);
                playerBag.BagList[emptySlotIndex]=new InventoryItem(ID, addThisTime);
                remainingAmount-=addThisTime;
            }

            EventHandler.CallUpdaeInventoryUI(InventoryLocation.Player, playerBag.BagList);

            return amount - remainingAmount;

        
        }

        /// <summary>
        /// 背包内交换物品
        /// </summary>
        /// <param name="frontIndex">选择交换的物品索引</param>
        /// <param name="targetIndex">被交换的物品索引</param>
        public void SwapItem(int frontIndex, int targetIndex)
        {
            InventoryItem currentItem = playerBag.BagList[frontIndex];
            InventoryItem targetItem = playerBag.BagList[targetIndex];

            if (targetItem.itemID != 0)
            {
                playerBag.BagList[frontIndex] = targetItem;
                playerBag.BagList[targetIndex] = currentItem;
            }
            else
            {
                playerBag.BagList[targetIndex] = currentItem;
                playerBag.BagList[frontIndex] = new InventoryItem();
            }

            EventHandler.CallUpdaeInventoryUI(InventoryLocation.Player, playerBag.BagList);
        }

        /// <summary>
        /// 移除背包里的物品（支持同种物品分散在多个格子的情况）
        /// </summary>
        public void RemoveItem(int ID, int removeAmount)
        {
            if (removeAmount <= 0)
                return;

            // 先算总量，不够就一个都不删
            if (GetItemAmountInBag(ID) < removeAmount)
            {
                Debug.LogWarning($"背包里的物品 {ID} 数量不足，需要 {removeAmount}，实际 {GetItemAmountInBag(ID)}");
                return;
            }

            int remaining = removeAmount;

            for (int i = 0; i < playerBag.BagList.Count && remaining > 0; i++)
            {
                InventoryItem slot = playerBag.BagList[i];
                if (slot.itemID != ID)
                    continue;

                if (slot.itemAmount <= remaining)
                {
                    // 这一格全部拿走
                    remaining -= slot.itemAmount;
                    playerBag.BagList[i] = new InventoryItem();
                }
                else
                {
                    // 这一格只用拿走一部分
                    playerBag.BagList[i] = new InventoryItem(ID, slot.itemAmount - remaining);
                    remaining = 0;
                }
            }

            EventHandler.CallUpdaeInventoryUI(InventoryLocation.Player, playerBag.BagList);
        }

        /// <summary>
        /// 获取背包中指定物品的剩余数量
        /// </summary>
        public int GetItemAmountInBag(int itemID)
        {
            int total = 0;
            foreach (var slot in playerBag.BagList)
            {
                if (slot.itemID == itemID)
                {
                    total += slot.itemAmount;
                }
            }
            return total;
        }
    }


}
