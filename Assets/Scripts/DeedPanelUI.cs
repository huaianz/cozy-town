using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeedPanelUI : MonoBehaviour
{
    private static DeedPanelUI instance;

    private GameObject canvasObject;
    private TextMeshProUGUI infoLabel;
    private TextMeshProUGUI titleLabel;
    private Image progressFill;
    private Button payButton;
    private TMP_FontAsset font;
    private bool built;
    private bool isOpen;

    public static void Toggle()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("DeedPanelUI");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<DeedPanelUI>();
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

        canvasObject = new GameObject("DeedCanvas");

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 865;

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
        panelRect.sizeDelta = new Vector2(1140f, 700f);
        panelRect.anchoredPosition = Vector2.zero;

        titleLabel = CreateLabel(panelObject.transform, "地契", 42, TextAlignmentOptions.Center);
        RectTransform titleRect = titleLabel.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(40f, 0f);
        titleRect.offsetMax = new Vector2(-40f, 0f);
        titleRect.sizeDelta = new Vector2(-80f, 60f);
        titleRect.anchoredPosition = new Vector2(0f, -24f);

        GameObject barBack = new GameObject("ProgressBack");
        barBack.transform.SetParent(panelObject.transform, false);

        Image backImage = barBack.AddComponent<Image>();
        backImage.color = new Color(0.18f, 0.14f, 0.1f, 0.9f);

        RectTransform backRect = backImage.rectTransform;
        backRect.anchorMin = new Vector2(0.5f, 1f);
        backRect.anchorMax = new Vector2(0.5f, 1f);
        backRect.pivot = new Vector2(0.5f, 1f);
        backRect.sizeDelta = new Vector2(900f, 44f);
        backRect.anchoredPosition = new Vector2(0f, -100f);

        GameObject barFill = new GameObject("ProgressFill");
        barFill.transform.SetParent(barBack.transform, false);

        progressFill = barFill.AddComponent<Image>();
        progressFill.color = new Color(0.85f, 0.68f, 0.32f, 1f);
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = 0;
        progressFill.fillAmount = 0f;

        RectTransform fillRect = progressFill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(4f, 4f);
        fillRect.offsetMax = new Vector2(-4f, -4f);

        infoLabel = CreateLabel(panelObject.transform, string.Empty, 30, TextAlignmentOptions.TopLeft);
        RectTransform infoRect = infoLabel.rectTransform;
        infoRect.anchorMin = new Vector2(0f, 0f);
        infoRect.anchorMax = new Vector2(1f, 1f);
        infoRect.offsetMin = new Vector2(70f, 160f);
        infoRect.offsetMax = new Vector2(-70f, -160f);
        infoLabel.lineSpacing = 12f;
        infoLabel.enableWordWrapping = true;

        payButton = CreateButton(panelObject.transform, "立即缴纳", new Vector2(-180f, 46f), PayNow);
        CreateButton(panelObject.transform, "关闭", new Vector2(180f, 46f), Close);
    }

    private void PayNow()
    {
        if (DebtManager.Pay())
        {
            Refresh();
            return;
        }

        EventHandler.CallFarmEventEvent("赎地", "金币不够，暂时交不上", true);
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
        rect.sizeDelta = new Vector2(300f, 84f);
        rect.anchoredPosition = position;

        TextMeshProUGUI label = CreateLabel(buttonObject.transform, text, 30, TextAlignmentOptions.Center);
        RectTransform textRect = label.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return button;
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

        int phase = DebtManager.Phase;

        if (phase == 0)
        {
            infoLabel.text = "主线尚未开始。\n点「开始游戏」后，老镇长会在这里交代赎地的约定。";
            progressFill.fillAmount = 0f;

            if (payButton != null)
            {
                payButton.interactable = false;
            }

            return;
        }

        if (phase >= 4)
        {
            titleLabel.text = "地契 · 已归你所有";
            infoLabel.text = "三期费用已全部缴清，土地正式归你所有。\n\n"
                + "第一封 · 土地：种下的东西或许会被风雨打倒，但土地从不会辜负认真对待它的人。\n\n"
                + "第二封 · 邻里：一个人守不住一座小镇，是大家互相帮衬，才有了田舍。\n\n"
                + "第三封 · 回家：我从没指望你还清什么，我只是想给你留一个，随时能回来的地方。";
            progressFill.fillAmount = 1f;

            if (payButton != null)
            {
                payButton.interactable = false;
            }

            return;
        }

        int amount = DebtManager.CurrentAmount;
        int paid = DebtManager.Paid;
        int deadline = DebtManager.CurrentDeadline;
        int remaining = DebtManager.RemainingDays();

        titleLabel.text = "地契 · 第 " + phase + " 期 · " + DebtManager.CurrentTitle;

        infoLabel.text = "本期主题：" + DebtManager.CurrentTitle + "\n"
            + "应还金额：" + amount + " 金币\n"
            + "已缴金额：" + paid + " 金币\n"
            + "还差：" + (amount - paid) + " 金币\n"
            + "截止日：第 " + deadline + " 天（距今天还有 " + remaining + " 天）\n\n"
            + (remaining <= 0
                ? "已经到期，镇长说先宽限几天，凑齐了随时来交。"
                : "在截止日前缴清，就能拿到外婆的回忆信。");

        progressFill.fillAmount = amount > 0 ? Mathf.Clamp01(paid / (float)amount) : 1f;

        if (payButton != null)
        {
            payButton.interactable = DebtManager.CanPay();
        }
    }
}