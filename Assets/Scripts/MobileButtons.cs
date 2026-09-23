using Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MobileButtons : MonoBehaviour
{
    private const float ButtonWidth = 152f;
    private const float ButtonHeight = 74f;
    private const float ButtonSpacing = 10f;
    private const float Margin = 16f;

    private static MobileButtons instance;

    private GameObject canvasObject;
    private TMP_FontAsset font;
    private bool built;

    private readonly System.Collections.Generic.List<GameObject> dockButtons = new System.Collections.Generic.List<GameObject>();
    private GameObject arrowObject;
    private TextMeshProUGUI arrowLabel;
    private bool dockCollapsed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (instance != null)
        {
            return;
        }

        GameObject go = new GameObject("MobileButtons");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<MobileButtons>();
    }

    private void Update()
    {
        if (built)
        {
            return;
        }

        if (!Application.isMobilePlatform && !PointerInput.HasUsedTouch)
        {
            return;
        }

        if (FindObjectOfType<GridMapMangaer>() == null)
        {
            return;
        }

        Build();
    }

    private void Build()
    {
        built = true;

        canvasObject = new GameObject("MobileButtonsCanvas");

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 800;

        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(canvasObject);

        font = UiFont.GetChineseFont();

        CreateButton("Ë¯¾õ", 0, OnSleepClicked);
        CreateButton("Î¯ÍÐ", 1, OnCommissionClicked);
        CreateButton("Åëâ¿", 2, OnCookingClicked);
        CreateButton("¹¤·»", 3, OnWorkshopClicked);
        CreateButton("Í¼¼ø", 4, OnAlmanacClicked);
        CreateButton("µØÍ¼", 5, OnMapClicked);
        CreateButton("ÉèÖÃ", 6, OnSettingsClicked);
        CreateButton("µØÆõ", 7, OnDeedClicked);
        CreateButton("¿ª»Ä", 8, OnAreaUnlockClicked);

        CreateArrowButton();
    }

    private void CreateButton(string text, int index, UnityAction action)
    {
        GameObject buttonObject = new GameObject(text + "Button");
        buttonObject.transform.SetParent(canvasObject.transform, false);
        dockButtons.Add(buttonObject);

        Image image = buttonObject.AddComponent<Image>();

        Sprite buttonSprite = UiSprites.GetSliced("button_wood", 16f);
        Sprite pressedSprite = UiSprites.GetSliced("button_wood_pressed", 16f);

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

        if (pressedSprite != null)
        {
            button.transition = Selectable.Transition.SpriteSwap;
            SpriteState state = new SpriteState();
            state.pressedSprite = pressedSprite;
            state.highlightedSprite = buttonSprite;
            button.spriteState = state;
        }

        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);
        int column = index / 4;
        int row = index % 4;

        rect.anchoredPosition = new Vector2(-Margin - column * (ButtonWidth + ButtonSpacing), Margin + row * (ButtonHeight + ButtonSpacing));

        GameObject textObject = new GameObject("Label");
        textObject.transform.SetParent(buttonObject.transform, false);

        TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 30;
        label.color = Color.white;
        label.raycastTarget = false;

        if (font != null)
        {
            label.font = font;
        }

        RectTransform textRect = label.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    private void CreateArrowButton()
    {
        GameObject buttonObject = new GameObject("DockArrow");
        buttonObject.transform.SetParent(canvasObject.transform, false);

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
        button.onClick.AddListener(ToggleDock);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.sizeDelta = new Vector2(56f, 132f);
        rect.anchoredPosition = ExpandedArrowPosition();

        GameObject textObject = new GameObject("Label");
        textObject.transform.SetParent(buttonObject.transform, false);

        arrowLabel = textObject.AddComponent<TextMeshProUGUI>();
        arrowLabel.text = ">";
        arrowLabel.alignment = TextAlignmentOptions.Center;
        arrowLabel.fontSize = 40;
        arrowLabel.color = Color.white;
        arrowLabel.raycastTarget = false;

        if (font != null)
        {
            arrowLabel.font = font;
        }

        RectTransform textRect = arrowLabel.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        arrowObject = buttonObject;
    }

    private static Vector2 ExpandedArrowPosition()
    {
        float x = Margin + 2f * (ButtonWidth + ButtonSpacing) + ButtonWidth + 8f;
        float y = Margin + (ButtonHeight + ButtonSpacing) * 0.3f;

        return new Vector2(-x, y);
    }

    private static Vector2 CollapsedArrowPosition()
    {
        float y = Margin + (ButtonHeight + ButtonSpacing) * 0.3f;

        return new Vector2(-Margin, y);
    }

    private void ToggleDock()
    {
        dockCollapsed = !dockCollapsed;

        for (int i = 0; i < dockButtons.Count; i++)
        {
            dockButtons[i].SetActive(!dockCollapsed);
        }

        if (arrowLabel != null)
        {
            arrowLabel.text = dockCollapsed ? "<" : ">";
        }

        if (arrowObject != null)
        {
            RectTransform rect = arrowObject.GetComponent<RectTransform>();
            rect.anchoredPosition = dockCollapsed ? CollapsedArrowPosition() : ExpandedArrowPosition();
        }
    }

    private void OnSleepClicked()
    {
        TimeManager timeManager = FindObjectOfType<TimeManager>();

        if (timeManager != null)
        {
            timeManager.SleepToNextDay();
        }
    }

    private void OnCommissionClicked()
    {
        CommissionPanel.Toggle();
    }

    private void OnSettingsClicked()
    {
        SettingsUI settings = FindObjectOfType<SettingsUI>();

        if (settings != null)
        {
            settings.ToggleSettings();
        }
    }
    private void OnDeedClicked()
    {
        DeedPanelUI.Toggle();
    }

    private void OnAreaUnlockClicked()
    {
        AreaUnlockPanelUI.Toggle();
    }

    private void OnDebugClicked()
    {
        DebugOverlay.Toggle();
    }

    private void OnCookingClicked()
    {
        CookingPanel.Toggle();
    }

    private void OnWorkshopClicked()
    {
        ToolUpgradePanel.Toggle();
    }

    private void OnAlmanacClicked()
    {
        AlmanacPanel.Toggle();
    }

    private void OnMapClicked()
    {
        AreaPanel.Toggle();
    }
}