using System.Collections.Generic;
using Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AreaUnlockPanelUI : MonoBehaviour
{
    private static AreaUnlockPanelUI instance;

    private GameObject canvasObject;
    private TMP_FontAsset font;
    private TextMeshProUGUI moneyLabel;
    private bool built;
    private bool isOpen;

    private readonly List<TextMeshProUGUI> nameLabels = new List<TextMeshProUGUI>();
    private readonly List<TextMeshProUGUI> statusLabels = new List<TextMeshProUGUI>();
    private readonly List<TextMeshProUGUI> buttonLabels = new List<TextMeshProUGUI>();
    private readonly List<Button> buttons = new List<Button>();

    public static void Toggle()
    {
        Ensure();
        instance.SetOpen(!instance.isOpen);
    }

    public static void Open()
    {
        Ensure();
        instance.SetOpen(true);
    }

    public static void RefreshIfOpen()
    {
        if (instance != null && instance.isOpen)
        {
            instance.Refresh();
        }
    }

    private static void Ensure()
    {
        if (instance != null)
        {
            return;
        }

        GameObject go = new GameObject("AreaUnlockPanelUI");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<AreaUnlockPanelUI>();
    }

    private void OnEnable()
    {
        EventHandler.UpdateMoneyEvent += OnMoneyChanged;
    }

    private void OnDisable()
    {
        EventHandler.UpdateMoneyEvent -= OnMoneyChanged;
    }

    private void OnMoneyChanged(int money)
    {
        if (isOpen)
        {
            Refresh();
        }
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

        canvasObject = new GameObject("AreaUnlockCanvas");

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 872;

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
        panelRect.sizeDelta = new Vector2(1240f, 860f);
        panelRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI title = CreateLabel(panelObject.transform, "开荒", 46, TextAlignmentOptions.Center);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(40f, 0f);
        titleRect.offsetMax = new Vector2(-40f, 0f);
        titleRect.sizeDelta = new Vector2(-80f, 64f);
        titleRect.anchoredPosition = new Vector2(0f, -22f);

        moneyLabel = CreateLabel(panelObject.transform, "", 28, TextAlignmentOptions.Center);
        RectTransform moneyRect = moneyLabel.rectTransform;
        moneyRect.anchorMin = new Vector2(0.5f, 0f);
        moneyRect.anchorMax = new Vector2(0.5f, 0f);
        moneyRect.pivot = new Vector2(0.5f, 0f);
        moneyRect.sizeDelta = new Vector2(900f, 44f);
        moneyRect.anchoredPosition = new Vector2(0f, 118f);

        AreaUnlockManager manager = AreaUnlockManager.Instance;
        int count = manager != null ? manager.Areas.Count : 0;

        for (int i = 0; i < count; i++)
        {
            CreateRow(panelObject.transform, manager.Areas[i], i);
        }

        CreateCloseButton(panelObject.transform);
    }

    private void CreateRow(Transform parent, AreaUnlockManager.AreaInfo area, int index)
    {
        GameObject rowObject = new GameObject("Row_" + area.kind);
        rowObject.transform.SetParent(parent, false);

        Image rowImage = rowObject.AddComponent<Image>();
        rowImage.color = new Color(0.16f, 0.12f, 0.08f, 0.55f);

        RectTransform rowRect = rowImage.rectTransform;
        rowRect.anchorMin = new Vector2(0.5f, 0.5f);
        rowRect.anchorMax = new Vector2(0.5f, 0.5f);
        rowRect.pivot = new Vector2(0.5f, 0.5f);
        rowRect.sizeDelta = new Vector2(1120f, 190f);
        rowRect.anchoredPosition = new Vector2(0f, 190f - index * 205f);

        TextMeshProUGUI name = CreateLabel(rowObject.transform, area.name, 40, TextAlignmentOptions.Left);
        RectTransform nameRect = name.rectTransform;
        nameRect.anchorMin = new Vector2(0f, 0.5f);
        nameRect.anchorMax = new Vector2(0f, 0.5f);
        nameRect.pivot = new Vector2(0f, 0.5f);
        nameRect.sizeDelta = new Vector2(300f, 60f);
        nameRect.anchoredPosition = new Vector2(46f, 0f);

        TextMeshProUGUI status = CreateLabel(rowObject.transform, "", 26, TextAlignmentOptions.Left);
        RectTransform statusRect = status.rectTransform;
        statusRect.anchorMin = new Vector2(0f, 0.5f);
        statusRect.anchorMax = new Vector2(0f, 0.5f);
        statusRect.pivot = new Vector2(0f, 0.5f);
        statusRect.sizeDelta = new Vector2(560f, 150f);
        statusRect.anchoredPosition = new Vector2(360f, 0f);

        status.enableWordWrapping = true;
        status.lineSpacing = 6f;

        GameObject buttonObject = new GameObject("Unlock");
        buttonObject.transform.SetParent(rowObject.transform, false);

        Image buttonImage = buttonObject.AddComponent<Image>();
        Sprite buttonSprite = UiSprites.GetSliced("button_wood", 16f);

        if (buttonSprite != null)
        {
            buttonImage.sprite = buttonSprite;
            buttonImage.type = Image.Type.Sliced;
            buttonImage.color = Color.white;
        }
        else
        {
            buttonImage.color = new Color(0f, 0f, 0f, 0.45f);
        }

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;

        RectTransform buttonRect = buttonImage.rectTransform;
        buttonRect.anchorMin = new Vector2(1f, 0.5f);
        buttonRect.anchorMax = new Vector2(1f, 0.5f);
        buttonRect.pivot = new Vector2(1f, 0.5f);
        buttonRect.sizeDelta = new Vector2(230f, 84f);
        buttonRect.anchoredPosition = new Vector2(-46f, 0f);

        TextMeshProUGUI buttonLabel = CreateLabel(buttonObject.transform, "开荒", 30, TextAlignmentOptions.Center);
        RectTransform buttonLabelRect = buttonLabel.rectTransform;
        buttonLabelRect.anchorMin = Vector2.zero;
        buttonLabelRect.anchorMax = Vector2.one;
        buttonLabelRect.offsetMin = Vector2.zero;
        buttonLabelRect.offsetMax = Vector2.zero;

        int captured = index;
        button.onClick.AddListener(() => OnUnlockClicked(captured));

        nameLabels.Add(name);
        statusLabels.Add(status);
        buttonLabels.Add(buttonLabel);
        buttons.Add(button);
    }

    private void CreateCloseButton(Transform parent)
    {
        GameObject buttonObject = new GameObject("Close");
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.AddComponent<Image>();
        Sprite sprite = UiSprites.GetSliced("button_wood", 16f);

        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }
        else
        {
            image.color = new Color(0f, 0f, 0f, 0.45f);
        }

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => SetOpen(false));

        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(260f, 78f);
        rect.anchoredPosition = new Vector2(0f, 26f);

        TextMeshProUGUI label = CreateLabel(buttonObject.transform, "关闭", 30, TextAlignmentOptions.Center);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
    }

    private void OnUnlockClicked(int index)
    {
        AreaUnlockManager manager = AreaUnlockManager.Instance;

        if (manager == null || index < 0 || index >= manager.Areas.Count)
        {
            return;
        }

        manager.Unlock(manager.Areas[index]);
        Refresh();
    }

    private void Refresh()
    {
        AreaUnlockManager manager = AreaUnlockManager.Instance;

        if (manager == null)
        {
            return;
        }

        for (int i = 0; i < buttons.Count && i < manager.Areas.Count; i++)
        {
            AreaUnlockManager.AreaInfo area = manager.Areas[i];
            bool unlocked = manager.IsUnlocked(area.kind);
            string reason = manager.BlockReason(area);

            if (i < nameLabels.Count)
            {
                nameLabels[i].text = area.name;
            }

            statusLabels[i].text = unlocked
                ? "已经开荒，随时可以过去"
                : "开荒费 " + area.cost + " 金币\n" + (reason != null ? reason : "现在就可以开荒");

            buttons[i].interactable = !unlocked && reason == null;
            buttonLabels[i].text = unlocked ? "已开荒" : (reason == null ? "开荒" : "未达成");
        }

        if (moneyLabel != null)
        {
            int money = ShopManager.Instance != null ? ShopManager.Instance.PlayerMoney : 0;
            moneyLabel.text = "当前金币：" + money;
        }
    }

    private TextMeshProUGUI CreateLabel(Transform parent, string content, int size, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject("Label");
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
        label.text = content;
        label.alignment = alignment;
        label.fontSize = size;
        label.color = new Color(0.98f, 0.94f, 0.84f, 1f);
        label.raycastTarget = false;

        if (font != null)
        {
            label.font = font;
        }

        return label;
    }
}