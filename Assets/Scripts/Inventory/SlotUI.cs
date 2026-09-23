using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;

namespace Inventory
{


    public class SlotUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("组件获取")]
        [SerializeField] private Image slotImage;
        [SerializeField] private TextMeshProUGUI amountText;
        public Image slotHightlight;
        [SerializeField] private Button button;
        [Header("格子类型")]
        public SlotType slotType;
        public bool isSelected;
        public int slotIndex;

        //物品信息
        public Item item;
        public int itemAmount;

        private InventoryUI inventoryUI
        {
            get { return GetComponentInParent<InventoryUI>(); }
        }

        private void Start()
        {
            isSelected = false;

            if (item == null)
            {
                UpdateEmptySlot();
            }
        }

        /// <summary>
        /// 将slot更新为空
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void UpdateEmptySlot()
        {
            if (isSelected)
            {
                isSelected = false;
                inventoryUI.UpdateSlotHightlight(-1);
                //通知取消选择物品
                EventHandler.CallItemSelectedEvent(null, false);

            }
            item = null;
            slotImage.enabled = false;
            amountText.text=string.Empty;
            button.interactable = false;
        }

        /// <summary>
        /// 更新背包格子
        /// </summary>
        /// <param name="item"></param>
        /// <param name="amount"></param>
        public void UpdateSlot(Item item,int amount )
        {
            this.item = item;
            slotImage.sprite = item.itemIcon;
            itemAmount = amount;
            amountText.text = amount.ToString();
            slotImage.enabled = true;
            button.interactable=true;
        }

        /// <summary>
        /// 开始拖拽时触发
        /// </summary>
        /// <param name="eventData"></param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (itemAmount <= 0 || inventoryUI?.dragItem == null)
            {
                return;
            }

            inventoryUI.dragItem.enabled = true;
            inventoryUI.dragItem.sprite = slotImage.sprite;
            inventoryUI.dragItem.SetNativeSize();

            Vector2 offset = new Vector2(
                inventoryUI.dragItem.rectTransform.sizeDelta.x/6,
                inventoryUI.dragItem.rectTransform.sizeDelta.y / 8
            );
            inventoryUI.dragItem.transform.position = (Vector2)Input.mousePosition - offset;

            // 选中高亮
            isSelected = true;
            inventoryUI.UpdateSlotHightlight(slotIndex);
        }

        /// <summary>
        /// 拖拽过程中触发
        /// </summary>
        /// <param name="eventData"></param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void OnDrag(PointerEventData eventData)
        {
            if (inventoryUI?.dragItem == null || !inventoryUI.dragItem.enabled)
            {
                return;
            }

            // 保持图片跟随鼠标
            Vector2 offset = new Vector2(
                inventoryUI.dragItem.rectTransform.sizeDelta.x/6,
                inventoryUI.dragItem.rectTransform.sizeDelta.y/8 
            );
            inventoryUI.dragItem.transform.position = (Vector2)Input.mousePosition - offset;
        }

        /// <summary>
        /// 拖拽松开时触发
        /// </summary>
        /// <param name="eventData"></param>
        public void OnEndDrag(PointerEventData eventData)
        {
            if (inventoryUI?.dragItem != null)
            {
                inventoryUI.dragItem.enabled = false;
            }

            // 空值保护：拖拽到UI外直接结束
            if (eventData.pointerCurrentRaycast.gameObject == null)
            {
                inventoryUI?.UpdateSlotHightlight(-1);
                return;
            }

            SlotUI targetSlot = eventData.pointerCurrentRaycast.gameObject.GetComponent<SlotUI>();
            if (targetSlot == null)
            {
                inventoryUI?.UpdateSlotHightlight(-1);
                return;
            }

            // 只有背包格子之间才交换
            if (slotType == SlotType.Bag && targetSlot.slotType == SlotType.Bag)
            {
                if (slotIndex != targetSlot.slotIndex && InventoryManager.Instance != null)
                {
                    InventoryManager.Instance.SwapItem(slotIndex, targetSlot.slotIndex);
                }
            }

            // 关闭高亮
            inventoryUI?.UpdateSlotHightlight(-1);
        }

        /// <summary>
        /// 点击的时候触发
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (item == null) return;
            //让选择状态和高亮状态改变
            isSelected =!isSelected;
            inventoryUI.UpdateSlotHightlight(slotIndex);

            if (slotType == SlotType.Bag)
            {
                EventHandler.CallItemSelectedEvent(item, isSelected);
            }
        }
    }

}