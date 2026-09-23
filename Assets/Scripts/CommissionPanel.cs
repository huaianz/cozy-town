using Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CommissionPanel : MonoBehaviour
{
    private static CommissionPanel instance;

    private GameObject canvasObject;
    private TextMeshProUGUI infoLabel;
    private TMP_FontAsset font;
    private bool built;
    private bool isOpen;

    public static void Toggle()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("CommissionPanel");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<CommissionPanel>();
        }

        instance.SetOpen(!instance.isOpen);
    }

    private void OnEnable()
    {
        EventHandler.CommissionChangedEvent += OnCommissionChanged;
    }

    private void OnDisable()
    {
        EventHandler.CommissionChangedEvent -= OnCommissionChanged;
    }

    private void OnCommissionChanged()
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

        canvasObject = new GameObject("CommissionCanvas");

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 850;

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
            panel.color = new Color(0.1f, 0.08f, 0.06f, 0.95f);
        }

        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(1040f, 660f);
        panelRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI title = CreateLabel(panelObject.transform, "今日委托与事件", 40, TextAlignmentOptions.Center);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(40f, 0f);
        titleRect.offsetMax = new Vector2(-40f, 0f);
        titleRect.sizeDelta = new Vector2(-80f, 60f);
        titleRect.anchoredPosition = new Vector2(0f, -24f);

        infoLabel = CreateLabel(panelObject.transform, string.Empty, 32, TextAlignmentOptions.TopLeft);
        RectTransform infoRect = infoLabel.rectTransform;
        infoRect.anchorMin = new Vector2(0f, 0f);
        infoRect.anchorMax = new Vector2(1f, 1f);
        infoRect.offsetMin = new Vector2(48f, 130f);
        infoRect.offsetMax = new Vector2(-48f, -100f);
        infoLabel.lineSpacing = 12f;
        infoLabel.enableWordWrapping = true;

        CreateButton(panelObject.transform, "交付", new Vector2(-150f, 46f), Deliver);
        CreateButton(panelObject.transform, "关闭", new Vector2(150f, 46f), Close);
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
        rect.sizeDelta = new Vector2(240f, 76f);
        rect.anchoredPosition = position;

        TextMeshProUGUI label = CreateLabel(buttonObject.transform, text, 32, TextAlignmentOptions.Center);
        RectTransform textRect = label.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    private void OpenAlmanac()
    {
        SetOpen(false);
        AlmanacPanel.Toggle();
    }

    private void OpenCooking()
    {
        SetOpen(false);
        CookingPanel.Toggle();
    }

    private void Close()
    {
        SetOpen(false);
    }

    private void Deliver()
    {
        if (ShopManager.Instance == null)
        {
            return;
        }

        ShopManager.Instance.TryDeliverCommissions();
        Refresh();
    }

    private void Refresh()
    {
        if (infoLabel == null)
        {
            return;
        }

        string text = string.Empty;

        if (ShopManager.Instance != null)
        {
            text += "金币：" + ShopManager.Instance.PlayerMoney + "\n";
        }

        text += "委托：\n";

        if (ShopManager.Instance == null)
        {
            text += "委托还没准备好\n";
        }
        else
        {
            System.Collections.Generic.List<CommissionData> commissions = ShopManager.Instance.GetCommissions();

            if (commissions == null || commissions.Count == 0)
            {
                text += "今天没有委托\n";
            }
            else
            {
                for (int i = 0; i < commissions.Count; i++)
                {
                    CommissionData commission = commissions[i];

                    Item item = InventoryManager.Instance != null ? InventoryManager.Instance.GetItem(commission.itemID) : null;
                    string itemName = item != null ? item.itemName : commission.itemID.ToString();

                    text += (i + 1) + ". " + itemName + " ×" + commission.amount + "　报酬 " + commission.reward + " 金币";

                    if (!commission.delivered)
                    {
                        text += commission.daysLeft <= 1 ? "　【今天最后一天】" : "　还剩 " + commission.daysLeft + " 天";
                    }

                    if (commission.delivered)
                    {
                        text += "（已交付）";
                    }
                    else if (InventoryManager.Instance != null && InventoryManager.Instance.GetItemAmountInBag(commission.itemID) >= commission.amount)
                    {
                        text += "（可交付）";
                    }
                    else
                    {
                        text += "（背包不足）";
                    }

                    text += "\n";
                }
            }
        }

        text += "\n今日事件：\n";

        if (string.IsNullOrEmpty(RuntimeHud.LastEventText))
        {
            text += "今天还没有事情发生";
        }
        else
        {
            text += RuntimeHud.LastEventText;
        }

        infoLabel.text = text;
    }
}