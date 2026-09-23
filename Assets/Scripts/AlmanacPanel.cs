using System.Collections.Generic;
using Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AlmanacPanel : MonoBehaviour
{
    private const float RowHeight = 44f;
    private const float HeaderHeight = 48f;

    private static readonly string[] GroupNames =
    {
        "种子", "收获作物", "鱼塘", "素材", "料理", "工具与设施", "装饰"
    };

    public static int GroupOf(int id)
    {
        if (id == 40 || id == 41 || id == 42 || id == 56) return 2;
        if (id >= 51 && id <= 55) return 2;
        if (id >= 44 && id <= 46) return 4;
        if (id >= 58 && id <= 60) return 4;
        if (id >= 61 && id <= 64) return 6;
        if (id == 21 || id == 22 || id == 23 || id == 24 || id == 25 || id == 26 || id == 33 || id == 34 || id == 39) return 5;
        if (id == 27 || id == 28 || id == 29 || id == 30 || id == 31 || id == 32 || id == 37 || id == 38 || id == 57) return 3;
        if (id == 1 || id == 3 || id == 5 || id == 7 || id == 9 || id == 11 || id == 13 || id == 15 || id == 17 || id == 19 || id == 36 || id == 47 || id == 49) return 0;

        return 1;
    }

    private static AlmanacPanel instance;

    private GameObject canvasObject;
    private RectTransform contentRect;
    private TextMeshProUGUI countLabel;
    private TMP_FontAsset font;
    private bool built;
    private bool isOpen;

    public static void Toggle()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("AlmanacPanel");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<AlmanacPanel>();
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

        canvasObject = new GameObject("AlmanacCanvas");

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 870;

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
        panelRect.sizeDelta = new Vector2(1200f, 720f);
        panelRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI title = CreateLabel(panelObject.transform, "图鉴", 40, TextAlignmentOptions.Center);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(40f, 0f);
        titleRect.offsetMax = new Vector2(-40f, 0f);
        titleRect.sizeDelta = new Vector2(-80f, 60f);
        titleRect.anchoredPosition = new Vector2(0f, -20f);

        countLabel = CreateLabel(panelObject.transform, string.Empty, 28, TextAlignmentOptions.Center);
        RectTransform countRect = countLabel.rectTransform;
        countRect.anchorMin = new Vector2(0f, 1f);
        countRect.anchorMax = new Vector2(1f, 1f);
        countRect.pivot = new Vector2(0.5f, 1f);
        countRect.offsetMin = new Vector2(40f, 0f);
        countRect.offsetMax = new Vector2(-40f, 0f);
        countRect.sizeDelta = new Vector2(-80f, 40f);
        countRect.anchoredPosition = new Vector2(0f, -78f);

        TextMeshProUGUI hint = CreateLabel(panelObject.transform, "获得过一次就会永久记录，卖掉或用掉也不会消失", 22, TextAlignmentOptions.Center);
        RectTransform hintRect = hint.rectTransform;
        hintRect.anchorMin = new Vector2(0f, 1f);
        hintRect.anchorMax = new Vector2(1f, 1f);
        hintRect.pivot = new Vector2(0.5f, 1f);
        hintRect.offsetMin = new Vector2(40f, 0f);
        hintRect.offsetMax = new Vector2(-40f, 0f);
        hintRect.sizeDelta = new Vector2(-80f, 32f);
        hintRect.anchoredPosition = new Vector2(0f, -114f);
        hint.color = new Color(0.78f, 0.72f, 0.62f, 1f);

        GameObject scrollObject = new GameObject("Scroll");
        scrollObject.transform.SetParent(panelObject.transform, false);

        RectTransform scrollRect = scrollObject.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0f, 0f);
        scrollRect.anchorMax = new Vector2(1f, 1f);
        scrollRect.offsetMin = new Vector2(48f, 150f);
        scrollRect.offsetMax = new Vector2(-48f, -156f);

        ScrollRect scroll = scrollObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;

        GameObject viewportObject = new GameObject("Viewport");
        viewportObject.transform.SetParent(scrollObject.transform, false);

        RectTransform viewportRect = viewportObject.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        viewportObject.AddComponent<RectMask2D>();

        GameObject dragArea = new GameObject("DragArea");
        dragArea.transform.SetParent(viewportObject.transform, false);

        Image dragImage = dragArea.AddComponent<Image>();
        dragImage.color = new Color(0f, 0f, 0f, 0.001f);
        dragImage.raycastTarget = true;

        RectTransform dragRect = dragImage.rectTransform;
        dragRect.anchorMin = Vector2.zero;
        dragRect.anchorMax = Vector2.one;
        dragRect.offsetMin = Vector2.zero;
        dragRect.offsetMax = Vector2.zero;

        GameObject contentObject = new GameObject("Content");
        contentObject.transform.SetParent(viewportObject.transform, false);

        contentRect = contentObject.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);

        scroll.viewport = viewportRect;
        scroll.content = contentRect;

        CreateButton(panelObject.transform, "关闭", new Vector2(0f, 46f), Close);
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

    private void CreateButton(Transform parent, string text, Vector2 position, UnityEngine.Events.UnityAction action)
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
        rect.sizeDelta = new Vector2(260f, 80f);
        rect.anchoredPosition = position;

        TextMeshProUGUI label = CreateLabel(buttonObject.transform, text, 32, TextAlignmentOptions.Center);
        RectTransform textRect = label.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    private void Close()
    {
        SetOpen(false);
    }

    private void Refresh()
    {
        if (contentRect == null)
        {
            return;
        }

        for (int i = contentRect.childCount - 1; i >= 0; i--)
        {
            Destroy(contentRect.GetChild(i).gameObject);
        }

        AlmanacManager.SyncBag();

        List<Item> items = null;

        if (InventoryManager.Instance != null && InventoryManager.Instance.itemData != null)
        {
            items = InventoryManager.Instance.itemData.Items;
        }

        if (items == null)
        {
            return;
        }

        int groupCount = GroupNames.Length;
        int[] knownCount = new int[groupCount];
        int[] totalCount = new int[groupCount];
        int discovered = 0;
        int all = 0;

        for (int i = 0; i < items.Count; i++)
        {
            Item item = items[i];

            if (item == null || item.itemID <= 0)
            {
                continue;
            }

            int group = GroupOf(item.itemID);

            if (group < 0 || group >= groupCount)
            {
                continue;
            }

            totalCount[group]++;
            all++;

            if (AlmanacManager.IsDiscovered(item.itemID))
            {
                knownCount[group]++;
                discovered++;
            }
        }

        float y = 0f;
        int row = 0;

        for (int group = 0; group < groupCount; group++)
        {
            if (totalCount[group] == 0)
            {
                continue;
            }

            CreateHeader(GroupNames[group] + "   " + knownCount[group] + " / " + totalCount[group], y, row);
            y += HeaderHeight;
            row++;

            for (int i = 0; i < items.Count; i++)
            {
                Item item = items[i];

                if (item == null || item.itemID <= 0 || GroupOf(item.itemID) != group)
                {
                    continue;
                }

                CreateRow(item, AlmanacManager.IsDiscovered(item.itemID), y, row);
                y += RowHeight;
                row++;
            }
        }

        contentRect.sizeDelta = new Vector2(0f, y + 8f);

        if (countLabel != null)
        {
            countLabel.text = "已收集 " + discovered + " / " + all + "（拖动可滚动）";
        }
    }

    private void CreateHeader(string text, float y, int index)
    {
        GameObject headerObject = new GameObject("Header_" + index);
        headerObject.transform.SetParent(contentRect, false);

        Image bar = headerObject.AddComponent<Image>();
        bar.color = new Color(0.34f, 0.24f, 0.14f, 0.92f);
        bar.raycastTarget = false;

        RectTransform headerRect = bar.rectTransform;
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.sizeDelta = new Vector2(0f, HeaderHeight);
        headerRect.anchoredPosition = new Vector2(0f, -y);

        TextMeshProUGUI label = CreateLabel(headerObject.transform, text, 30, TextAlignmentOptions.Left);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(14f, 0f);
        labelRect.offsetMax = new Vector2(-10f, 0f);
        label.color = new Color(1f, 0.92f, 0.72f, 1f);
    }

    private void CreateRow(Item item, bool known, float y, int index)
    {
        GameObject rowObject = new GameObject("Row_" + index + "_" + item.itemID);
        rowObject.transform.SetParent(contentRect, false);

        RectTransform rowRect = rowObject.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.sizeDelta = new Vector2(0f, RowHeight);
        rowRect.anchoredPosition = new Vector2(0f, -y);

        Sprite icon = known ? (item.itemIcon != null ? item.itemIcon : item.itemOnWorldSprite) : null;

        if (icon != null)
        {
            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(rowObject.transform, false);

            Image iconImage = iconObject.AddComponent<Image>();
            iconImage.sprite = icon;
            iconImage.raycastTarget = false;
            iconImage.preserveAspect = true;

            RectTransform iconRect = iconImage.rectTransform;
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.sizeDelta = new Vector2(36f, 36f);
            iconRect.anchoredPosition = new Vector2(8f, 0f);
        }

        TextMeshProUGUI label = CreateLabel(rowObject.transform, known ? item.itemName : "？ ？？？", 28, TextAlignmentOptions.Left);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = new Vector2(56f, 0f);
        labelRect.offsetMax = new Vector2(-8f, 0f);

        label.color = known ? new Color(0.97f, 0.92f, 0.82f, 1f) : new Color(0.6f, 0.56f, 0.5f, 1f);
    }
}