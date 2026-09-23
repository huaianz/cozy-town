using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DebugOverlay : MonoBehaviour
{
    private static DebugOverlay instance;

    private GameObject canvasObject;
    private TextMeshProUGUI infoLabel;
    private TextMeshProUGUI pointerLabel;
    private TMP_FontAsset font;
    private bool built;
    private bool isOpen;
    private float refreshTimer;

    public static void Toggle()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("DebugOverlay");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<DebugOverlay>();
        }

        instance.SetOpen(!instance.isOpen);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Y))
        {
            NextWeather();
        }

        if (Input.GetKeyDown(KeyCode.U))
        {
            NextSeason();
        }

        if (!isOpen)
        {
            return;
        }

        UpdatePointerLabel();

        refreshTimer += Time.unscaledDeltaTime;

        if (refreshTimer >= 0.4f)
        {
            refreshTimer = 0f;
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

        canvasObject = new GameObject("DebugCanvas");

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(canvasObject);

        GameObject panelObject = new GameObject("DebugPanel");
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
            panel.color = new Color(0f, 0f, 0f, 0.7f);
        }

        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0f, 0.5f);
        panelRect.anchorMax = new Vector2(0f, 0.5f);
        panelRect.pivot = new Vector2(0f, 0.5f);
        panelRect.sizeDelta = new Vector2(560f, 320f);
        panelRect.anchoredPosition = new Vector2(24f, 0f);

        infoLabel = CreateLabel(panelObject.transform, 130f, 34);
        infoLabel.text = string.Empty;

        CreateButton(panelObject.transform, "天气 +1", -170f, NextWeather);
        CreateButton(panelObject.transform, "季节 +1", -90f, NextSeason);
        CreateButton(panelObject.transform, "关闭", -10f, Close);
    }

    private TextMeshProUGUI CreateLabel(Transform parent, float y, int size)
    {
        GameObject textObject = new GameObject("Info");
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Left;
        label.color = new Color(0.95f, 0.89f, 0.79f, 1f);
        label.fontSize = size;
        label.raycastTarget = false;

        if (font != null)
        {
            label.font = font;
        }

        RectTransform rect = label.rectTransform;
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.offsetMin = new Vector2(30f, 0f);
        rect.offsetMax = new Vector2(-30f, 0f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(-60f, 52f);

        return label;
    }

    private void CreateButton(Transform parent, string text, float y, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject("DebugButton");
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
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(220f, 64f);
        rect.anchoredPosition = new Vector2(30f, y);

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

    private void Close()
    {
        SetOpen(false);
    }

    private void NextWeather()
    {
        TimeManager timeManager = FindObjectOfType<TimeManager>();

        if (timeManager == null)
        {
            return;
        }

        WeatherType weather = timeManager.RollWeather(timeManager.GetSeason());
        timeManager.SetWeather(weather);
        Refresh();
    }

    private void NextSeason()
    {
        TimeManager timeManager = FindObjectOfType<TimeManager>();

        if (timeManager == null)
        {
            return;
        }

        int next = ((int)timeManager.GetSeason() + 1) % 4;
        timeManager.SetSeason((Season)next);
        Refresh();
    }

    private void UpdatePointerLabel()
    {
        if (pointerLabel == null)
        {
            return;
        }

        Camera mainCamera = Camera.main;

        if (mainCamera == null)
        {
            pointerLabel.text = string.Empty;
            return;
        }

        Vector2 screenPoint = PointerInput.Position;
        Vector3 worldPoint = mainCamera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, 0f));
        Grid grid = FindObjectOfType<Grid>();

        if (grid == null)
        {
            pointerLabel.text = "指针 世界 " + worldPoint.x.ToString("0.00") + " , " + worldPoint.y.ToString("0.00");
            return;
        }

        Vector3Int cell = grid.WorldToCell(worldPoint);

        pointerLabel.text = "指针 世界 " + worldPoint.x.ToString("0.00") + " , " + worldPoint.y.ToString("0.00")
            + "　格子 " + cell.x + " , " + cell.y;
    }

    private void Refresh()
    {
        if (infoLabel == null)
        {
            return;
        }

        TimeManager timeManager = FindObjectOfType<TimeManager>();

        if (timeManager == null)
        {
            infoLabel.text = string.Empty;
            return;
        }

        infoLabel.text = "季节：" + WeatherEffects.GetSeasonName(timeManager.GetSeason())
            + "　天气：" + WeatherEffects.GetName(timeManager.GetWeather());
    }
}