using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class ItemTooltip : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI typeText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private GameObject bottomPart;

    public void SetupTooltip(Item item, SlotType slotType)
    {
        nameText.text=item.itemName;
        typeText.text = GetItemType(item.itemType);
        descriptionText.text = item.itemDescription;

        if (item.itemType == ItemType.Seed || item.itemType == ItemType.Product)
        {
            bottomPart.SetActive(true);

            var price = item.itemPrice;
            if (slotType == SlotType.Bag)
            {
                price = (int)(price * item.sellPercentage);
            }

            valueText.text=price.ToString();
        }
        else
        {
            bottomPart.SetActive(false);
        }

        LayoutRebuilder.MarkLayoutForRebuild(GetComponent<RectTransform>());

    }


    private string GetItemType(ItemType itemType)
    {
        return itemType switch
        {
            ItemType.SickleTool => "工具",// 镰刀收获
            ItemType.axeTool => "工具",// 斧头
            ItemType.HoeTool => "工具",// 锄头耕地
            ItemType.WaterCanTool => "工具",// 水壶浇水
            ItemType.pickaxeTool => "工具", // 稿子
            ItemType.Seed => "种子",
            ItemType.Product => "商品",
            ItemType.Deed => "道具",
            _ => "无"
        };
    }
}
