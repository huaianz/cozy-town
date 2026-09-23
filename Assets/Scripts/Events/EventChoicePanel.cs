using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class EventChoicePanel : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    private static EventChoicePanel instance;

    private GameObject canvasObject;
    private TextMeshProUGUI titleLabel;
    private TextMeshProUGUI infoLabel;
    private TextMeshProUGUI acceptLabel;
    private TextMeshProUGUI declineLabel;
    private TMP_FontAsset font;
    private UnityAction onAccept;
    private UnityAction onDecline;
    private bool built;

    public static void Show(string title, string description, string acceptText, UnityAction accept, string declineText, UnityAction decline)
    {
        if (instance == null)
        {
            GameObject go = new GameObject("EventChoicePanel");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<EventChoicePanel>();
        }

        instance.Open(title, description, acceptText, accept, declineText, decline);
    }

    private void Open(string title, string description, string acceptText, UnityAction accept, string declineText, UnityAction decline)
    {
        if (!built)
        {
            Build();
        }

        onAccept = accept;
        onDecline = decline;

        titleLabel.text = title;
        infoLabel.text = description;
        acceptLabel.text = string.IsNullOrEmpty(acceptText) ? "接受" : acceptText;
        declineLabel.text = string.IsNullOrEmpty(declineText) ? "拒绝" : declineText;

        canvasObject.SetActive(true);
        IsOpen = true;

        Time.timeScale = 0f;
    }

    private void Close()
    {
        canvasObject.SetActive(false);
        IsOpen = false;
        onAccept = null;
        onDecline = null;
        Time.timeScale = 1f;
    }

    private void OnAcceptClicked()
    {
        UnityAction action = onAccept;
        Close();

        if (action != null)
        {
            action();
        }
    }

    private void OnDeclineClicked()
    {
        UnityAction action = onDecline;
        Close();

        if (action != null)
        {
            action();
        }
    }

    private void Build()
    {
        built = true;
        font = UiFont.GetChineseFont();

        canvasObject = new GameObject("EventChoiceCanvas");

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
        dim.color = new Color(0f, 0f, 0f, 0.4f);

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
        panelRect.sizeDelta = new Vector2(1000f, 440f);
        panelRect.anchoredPosition = Vector2.zero;

        titleLabel = CreateLabel(panelObject.transform, "事件", 40, TextAlignmentOptions.Center);
        RectTransform titleRect = titleLabel.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(40f, 0f);
        titleRect.offsetMax = new Vector2(-40f, 0f);
        titleRect.sizeDelta = new Vector2(-80f, 60f);
        titleRect.anchoredPosition = new Vector2(0f, -26f);

        infoLabel = CreateLabel(panelObject.transform, string.Empty, 32, TextAlignmentOptions.TopLeft);
        RectTransform infoRect = infoLabel.rectTransform;
        infoRect.anchorMin = new Vector2(0f, 0f);
        infoRect.anchorMax = new Vector2(1f, 1f);
        infoRect.offsetMin = new Vector2(56f, 150f);
        infoRect.offsetMax = new Vector2(-56f, -104f);
        infoLabel.lineSpacing = 10f;
        infoLabel.enableWordWrapping = true;

        acceptLabel = CreateButton(panelObject.transform, "　", new Vector2(-200f, 46f), OnAcceptClicked);
        declineLabel = CreateButton(panelObject.transform, "　", new Vector2(200f, 46f), OnDeclineClicked);
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

    private TextMeshProUGUI CreateButton(Transform parent, string text, Vector2 position, UnityAction action)
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
        rect.sizeDelta = new Vector2(340f, 84f);
        rect.anchoredPosition = position;

        TextMeshProUGUI label = CreateLabel(buttonObject.transform, text, 30, TextAlignmentOptions.Center);
        RectTransform textRect = label.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10f, 0f);
        textRect.offsetMax = new Vector2(-10f, 0f);

        return label;
    }
}