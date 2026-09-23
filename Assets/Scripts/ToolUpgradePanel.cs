using System.Collections.Generic;
using Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ToolUpgradePanel : MonoBehaviour
{
    private static ToolUpgradePanel instance;

    private GameObject canvasObject;
    private TextMeshProUGUI infoLabel;
    private TMP_FontAsset font;
    private bool built;
    private ScrollRect scrollRect;
    private RectTransform contentRect;
    private bool isOpen;
    private TextMeshProUGUI statusLabel;
    private Button repairButton;
    private TextMeshProUGUI repairLabel;

    private static readonly ItemType[] Tools =
    {
        ItemType.HoeTool,
        ItemType.WaterCanTool,
        ItemType.SickleTool,
        ItemType.axeTool,
        ItemType.pickaxeTool,
        ItemType.FishingRod
    };

    public static void Toggle()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("ToolUpgradePanel");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<ToolUpgradePanel>();
        }

        instance.SetOpen(!instance.isOpen);
    }

    private void SetOpen(bool open)
    {
        isOpen = open;

        if (!built)
        {
            if (!open)
            {
                return;
            }

            Build();
        }

        canvasObject.SetActive(open);

        if (open)
        {
            Refresh();
        }
    }

    private void Build()
    {
        built = true;
        font = UiFont.GetChineseFont();

        canvasObject = new GameObject("ToolUpgradeCanvas");

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 880;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(canvasObject);

        GameObject dimObject = new GameObject("Dim");
        dimObject.transform.SetParent(canvasObject.transform, false);

        Image dim = dimObject.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.35f);

        RectTransform dimRect = dim.rectTransform;
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = Vector2.zero;
        dimRect.offsetMax = Vector2.zero;

        GameObject panelObject = new GameObject("Panel");
        panelObject.transform.SetParent(canvasObject.transform, false);

        Image panel = panelObject.AddComponent<Image>();
        Sprite panelSprite = UiSprites.GetSliced("panel_wood", 24f);

        if (panelSprite != null)
        {
            panel.sprite = panelSprite;
            panel.type = Image.Type.Sliced;
            panel.color = Color.white;
        }
        else
        {
            panel.color = new Color(0.12f, 0.09f, 0.06f, 0.96f);
        }

        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(1180f, 660f);
        panelRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI title = CreateLabel(panelObject.transform, "工坊 · 工具升级与修复", 40, TextAlignmentOptions.Center);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(40f, 0f);
        titleRect.offsetMax = new Vector2(-40f, 0f);
        titleRect.sizeDelta = new Vector2(-80f, 60f);
        titleRect.anchoredPosition = new Vector2(0f, -22f);

        statusLabel = CreateLabel(panelObject.transform, "点“修复工具”可以把所有工具修满", 26, TextAlignmentOptions.Center);
        RectTransform statusRect = statusLabel.rectTransform;
        statusRect.anchorMin = new Vector2(0f, 1f);
        statusRect.anchorMax = new Vector2(1f, 1f);
        statusRect.pivot = new Vector2(0.5f, 1f);
        statusRect.offsetMin = new Vector2(44f, 0f);
        statusRect.offsetMax = new Vector2(-44f, 0f);
        statusRect.sizeDelta = new Vector2(-88f, 40f);
        statusRect.anchoredPosition = new Vector2(0f, -90f);
        statusLabel.color = new Color(0.98f, 0.86f, 0.62f, 1f);

        GameObject scrollObject = new GameObject("Scroll");
        scrollObject.transform.SetParent(panelObject.transform, false);

        scrollRect = scrollObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 30f;

        RectTransform scrollArea = scrollObject.GetComponent<RectTransform>();
        scrollArea.anchorMin = new Vector2(0f, 0f);
        scrollArea.anchorMax = new Vector2(1f, 1f);
        scrollArea.offsetMin = new Vector2(56f, 300f);
        scrollArea.offsetMax = new Vector2(-56f, -150f);

        GameObject viewportObject = new GameObject("Viewport");
        viewportObject.transform.SetParent(scrollObject.transform, false);

        RectTransform viewportRect = viewportObject.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        Image dragImage = viewportObject.AddComponent<Image>();
        dragImage.color = new Color(0f, 0f, 0f, 0.001f);
        dragImage.raycastTarget = true;

        viewportObject.AddComponent<RectMask2D>();

        GameObject contentObject = new GameObject("Content");
        contentObject.transform.SetParent(viewportObject.transform, false);

        contentRect = contentObject.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 400f);

        infoLabel = CreateLabel(contentObject.transform, string.Empty, 28, TextAlignmentOptions.TopLeft);
        RectTransform infoRect = infoLabel.rectTransform;
        infoRect.anchorMin = new Vector2(0f, 1f);
        infoRect.anchorMax = new Vector2(1f, 1f);
        infoRect.pivot = new Vector2(0.5f, 1f);
        infoRect.sizeDelta = new Vector2(0f, 400f);
        infoRect.anchoredPosition = Vector2.zero;
        infoLabel.lineSpacing = 10f;
        infoLabel.enableWordWrapping = true;

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;

        for (int i = 0; i < Tools.Length; i++)
        {
            ItemType type = Tools[i];
            int column = i % 3;
            int row = i / 3;
            float x = -340f + column * 340f;
            float y = 196f - row * 88f;

            CreateButton(panelObject.transform, UpgradeText(type), new Vector2(x, y), () => Upgrade(type));
        }

        repairButton = CreateButton(panelObject.transform, "修复工具", new Vector2(-170f, 20f), RepairAll);
        repairLabel = repairButton != null ? repairButton.GetComponentInChildren<TextMeshProUGUI>() : null;

        CreateButton(panelObject.transform, "关闭", new Vector2(170f, 20f), Close);
    }

    private static string UpgradeText(ItemType type)
    {
        return "升级" + ToolDurability.ToolName(type);
    }

    private void RepairAll()
    {
        int repaired = 0;
        int oreCost = 0;
        int goldCost = 0;

        for (int i = 0; i < Tools.Length; i++)
        {
            ItemType type = Tools[i];

            if (!ToolDurability.NeedsRepair(type))
            {
                continue;
            }

            repaired++;
            oreCost += ToolDurability.GetRepairOreCost();
            goldCost += ToolDurability.GetRepairGoldCost(type);
        }

        if (repaired == 0)
        {
            SetStatus("所有工具都是满耐久，不用修");
            EventHandler.CallFarmEventEvent("工坊", "所有工具都是满耐久，不用修", true);
            return;
        }

        if (InventoryManager.Instance == null || InventoryManager.Instance.GetItemAmountInBag(37) < oreCost)
        {
            SetStatus("矿石不够：修复 " + repaired + " 件要给 矿石×" + oreCost + "（现在 " + (InventoryManager.Instance != null ? InventoryManager.Instance.GetItemAmountInBag(37) : 0) + "）");
            EventHandler.CallFarmEventEvent("工坊", "修复 " + repaired + " 件工具需要 矿石×" + oreCost + "，不够", true);
            return;
        }

        if (ShopManager.Instance == null || ShopManager.Instance.PlayerMoney < goldCost)
        {
            SetStatus("金币不够：修复 " + repaired + " 件要花 " + goldCost + " 金币");
            EventHandler.CallFarmEventEvent("工坊", "修复 " + repaired + " 件工具需要 " + goldCost + " 金币，不够", true);
            return;
        }

        InventoryManager.Instance.RemoveItem(37, oreCost);
        ShopManager.Instance.PlayerMoney -= goldCost;
        EventHandler.CallUpdateMoneyEvent(ShopManager.Instance.PlayerMoney);

        for (int i = 0; i < Tools.Length; i++)
        {
            ToolDurability.RepairFully(Tools[i]);
        }

        SetStatus("修好了 " + repaired + " 件工具（花了 矿石×" + oreCost + " + " + goldCost + " 金币）");
        EventHandler.CallFarmEventEvent("工坊", "花 矿石×" + oreCost + " 和 " + goldCost + " 金币，修好了 " + repaired + " 件工具", false);
        Refresh();
    }

    private void SetStatus(string text)
    {
        if (statusLabel != null)
        {
            statusLabel.text = text;
        }
    }

    private void Upgrade(ItemType type)
    {
        if (!ToolDurability.TryUpgrade(type))
        {
            EventHandler.CallFarmEventEvent("工坊", "材料不够，升级" + ToolDurability.ToolName(type) + "需要更多矿石和金币", true);
            Refresh();
            return;
        }

        Refresh();
    }

    private TextMeshProUGUI CreateLabel(Transform parent, string text, int size, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject("Label");
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.alignment = alignment;
        label.fontSize = size;
        label.color = new Color(0.97f, 0.92f, 0.82f, 1f);
        label.raycastTarget = false;

        if (font != null)
        {
            label.font = font;
        }

        return label;
    }

    private Button CreateButton(Transform parent, string text, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject("Button");
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.AddComponent<Image>();
        Sprite buttonSprite = UiSprites.GetSliced("button_wood", 16f);

        if (buttonSprite != null)
        {
            image.sprite = buttonSprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }
        else
        {
            image.color = new Color(0f, 0f, 0f, 0.45f);
        }

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(220f, 76f);
        rect.anchoredPosition = position;

        TextMeshProUGUI label = CreateLabel(buttonObject.transform, text, 28, TextAlignmentOptions.Center);
        RectTransform textRect = label.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return button;
    }

    private void CreateArrow(Transform parent, string text, Vector2 position, float direction)
    {
        GameObject buttonObject = new GameObject("Arrow" + text);
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.AddComponent<Image>();
        Sprite buttonSprite = UiSprites.GetSliced("button_wood", 16f);

        if (buttonSprite != null)
        {
            image.sprite = buttonSprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }
        else
        {
            image.color = new Color(0f, 0f, 0f, 0.45f);
        }

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => ScrollPage(direction));

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(72f, 72f);
        rect.anchoredPosition = position;

        TextMeshProUGUI label = CreateLabel(buttonObject.transform, text, 30, TextAlignmentOptions.Center);
        RectTransform textRect = label.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    private void ScrollPage(float direction)
    {
        if (scrollRect == null)
        {
            return;
        }

        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition + direction);
    }

    private void Close()
    {
        SetOpen(false);
    }

    private void Refresh()
    {
        if (infoLabel == null)
        {
            return;
        }

        string text = "金币：" + (ShopManager.Instance != null ? ShopManager.Instance.PlayerMoney : 0) + "\n\n";

        for (int i = 0; i < Tools.Length; i++)
        {
            ItemType type = Tools[i];
            int level = ToolDurability.GetLevel(type);
            int current = ToolDurability.GetCurrent(type);
            int max = ToolDurability.GetMax(type);

            text += ToolDurability.ToolName(type) + "  " + level + " 级：" + ToolDurability.GetLevelEffect(type, level);
            text += "　耐久 " + current + "/" + max;

            if (current < max)
            {
                text += "　修复需 矿石×" + ToolDurability.GetRepairOreCost() + " + " + ToolDurability.GetRepairGoldCost(type) + " 金币";
            }

            text += "\n";

            if (level >= 3)
            {
                text += "　已满级\n\n";
                continue;
            }

            text += "　→ 下一级：" + ToolDurability.GetLevelEffect(type, level + 1);
            text += "　升级需要 矿石×" + ToolDurability.GetUpgradeOreCost(type)
                + " + 金币×" + ToolDurability.GetUpgradeGoldCost(type);

            if (ToolDurability.GetUpgradeGemCost(type) > 0)
            {
                text += " + 宝石×" + ToolDurability.GetUpgradeGemCost(type);
            }

            text += ToolDurability.CanUpgrade(type) ? "　（可升级）\n\n" : "　（材料不足）\n\n";
        }

        infoLabel.text = text;

        int repairCount = 0;
        int repairOre = 0;
        int repairGold = 0;

        for (int i = 0; i < Tools.Length; i++)
        {
            if (!ToolDurability.NeedsRepair(Tools[i]))
            {
                continue;
            }

            repairCount++;
            repairOre += ToolDurability.GetRepairOreCost();
            repairGold += ToolDurability.GetRepairGoldCost(Tools[i]);
        }

        if (repairLabel != null)
        {
            repairLabel.text = repairCount == 0
                ? "工具都是满耐久"
                : "修复 " + repairCount + " 件（矿石x" + repairOre + " + " + repairGold + "金）";
        }

        if (repairButton != null)
        {
            repairButton.interactable = true;
        }

        if (statusLabel != null && string.IsNullOrEmpty(statusLabel.text))
        {
            statusLabel.text = repairCount == 0
                ? "所有工具都是满耐久，不用担心"
                : "有 " + repairCount + " 件工具需要修，点下面“修复”按钮";
        }

        if (contentRect != null)
        {
            float height = Mathf.Max(infoLabel.preferredHeight + 24f, 300f);
            contentRect.sizeDelta = new Vector2(0f, height);
            infoLabel.rectTransform.sizeDelta = new Vector2(0f, height);
        }
    }
}