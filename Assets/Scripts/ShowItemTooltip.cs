using Inventory;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

//表示挂载这个代码必须要有SlotUI
[RequireComponent(typeof(SlotUI))]
public class ShowItemTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private SlotUI slotUI;
    private InventoryUI inventoryUI
    {
        get
        {
            return GetComponentInParent<InventoryUI>();
        }
    }

    private void Awake()
    {
        slotUI = GetComponent<SlotUI>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(slotUI.item != null)
        {
            inventoryUI.itemTooltip.gameObject.SetActive(true);
            inventoryUI.itemTooltip.SetupTooltip(slotUI.item,slotUI.slotType);

            //先把提示语的中心点设置在底部中心，这样防止鼠标遮挡提示语
            inventoryUI.itemTooltip.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0);
            //然后再将提示语显示在物品格子上方上移60像素点位置
            inventoryUI.itemTooltip.transform.position = transform.position + Vector3.up * 60;
        }
        else
        {
            inventoryUI.itemTooltip.gameObject.SetActive(false);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        inventoryUI.itemTooltip.gameObject.SetActive(false);
    }
}
