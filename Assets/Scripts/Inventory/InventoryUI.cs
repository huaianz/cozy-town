using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Inventory;
public class InventoryUI : MonoBehaviour
{
    //提示语
    public ItemTooltip itemTooltip;

    [Header("拖拽时的图片")]
    public Image dragItem;

    [Header("玩家背包UI")]
    [SerializeField] private GameObject bagUI;
    private bool bagOpened;
    [SerializeField]private SlotUI[] playerSlots;

    private void Start()
    {
        //游戏开始时给背包格子排序，并且根据它是否显示来判断是否打开
        for(int i = 0; i < playerSlots.Length; i++)
        {
            playerSlots[i].slotIndex = i;
        }
        bagOpened = bagUI.activeInHierarchy;
        //开始的时候隐藏拖拽图片
        if (dragItem != null)
        {
            dragItem.enabled = false;
        }
    }

    private void OnEnable()
    {
        EventHandler.UpdateInventoryUI += OnUpdateInventoryUI;
    }

    private void OnDisable()
    {
        EventHandler.UpdateInventoryUI -= OnUpdateInventoryUI;
    }


    /// <summary>
    /// 更新背包UI
    /// </summary>
    /// <param name="location">背包类型（物品栏/背包）</param>
    /// <param name="list">背包列表</param>
    private void OnUpdateInventoryUI(InventoryLocation location,List<InventoryItem> list)
    {
        switch(location)
        {
            case InventoryLocation.Player:
                for(int i = 0; i < playerSlots.Length; i++)
                {
                    if (list[i].itemAmount > 0)
                    {
                        var item = InventoryManager.Instance.GetItem(list[i].itemID);
                        playerSlots[i].UpdateSlot(item,list[i].itemAmount);
                    }
                    else
                    {
                        playerSlots[i].UpdateEmptySlot();
                    }
                }
                break;
            case InventoryLocation.Box:

                break;
        }
    }
    /// <summary>
    /// 打开关闭背包
    /// </summary>
    public void OpenBagUI()
    {
        bagOpened = !bagOpened;
        bagUI.SetActive(bagOpened);
    }
    
    /// <summary>
    /// 更新格子高亮显示
    /// </summary>
    /// <param name="index">索引</param>
    public void UpdateSlotHightlight(int index)
    {
        foreach (var slot in playerSlots)
        {
            if (slot.isSelected && slot.slotIndex == index)
            {
                slot.slotHightlight.gameObject.SetActive(true);
            }
            else
            {
                slot.isSelected = false;
                slot.slotHightlight.gameObject.SetActive(false);
            }
        }
    }
}
