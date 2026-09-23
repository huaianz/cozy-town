using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;

public class RuntimeHud : MonoBehaviour
{
    public static string LastEventText;

    private const float EventRiseTime = 0.5f;
    private const float EventHoldTime = 6.5f;
    private const float EventFadeTime = 1.7f;

    private static RuntimeHud instance;

    private GameObject canvasObject;
    private Image rainOverlay;
    private Image panelImage;
    private Image weatherIcon;
    private TextMeshProUGUI staminaLabel;
    private TextMeshProUGUI weatherLabel;
    private TextMeshProUGUI toolLabel;
    private Item selectedItem;
    private GameObject eventRoot;
    private CanvasGroup eventGroup;
    private TextMeshProUGUI eventLabel;
    private TMP_FontAsset font;
    private bool built;
    private bool eventActive;
    private float eventElapsed;
    private float checkTimer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (instance != null)
        {
            return;
        }

        GameObject go = new GameObject("RuntimeHud");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<RuntimeHud>();
    }

    private void OnEnable()
    {
        EventHandler.StaminaChangedEvent += OnStaminaChanged;
        EventHandler.WeatherChangedEvent += OnWeatherChanged;
        EventHandler.FarmEventEvent += OnFarmEvent;
        EventHandler.ItemSelectEvent += OnItemSelected;
    }

    private void OnDisable()
    {
        EventHandler.StaminaChangedEvent -= OnStaminaChanged;
        EventHandler.WeatherChangedEvent -= OnWeatherChanged;
        EventHandler.FarmEventEvent -= OnFarmEvent;
        EventHandler.ItemSelectEvent -= OnItemSelected;
    }

    private void Update()
    {
        UpdateEventBanner();

        checkTimer += Time.unscaledDeltaTime;

        if (checkTimer < 0.5f)
        {
            return;
        }

        checkTimer = 0f;

        RefreshToolRow();

        bool inGame = FindObjectOfType<GridMapMangaer>() != null;

        if (inGame && !built)
        {
            Build();
        }

        if (canvasObject != null && canvasObject.activeSelf != inGame)
        {
            canvasObject.SetActive(inGame);
        }
    }

    private void Build()
    {
        built = true;
        font = UiFont.GetChineseFont();

        canvasObject = new GameObject("RuntimeHudCanvas");

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 700;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        DontDestroyOnLoad(canvasObject);

        GameObject overlayObject = new GameObject("WeatherOverlay");
        overlayObject.transform.SetParent(canvasObject.transform, false);

        rainOverlay = overlayObject.AddComponent<Image>();
        rainOverlay.color = new Color(0f, 0f, 0f, 0f);
        rainOverlay.raycastTarget = false;

        RectTransform overlayRect = rainOverlay.rectTransform;
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        GameObject panelObject = new GameObject("StatusPanel");
        panelObject.transform.SetParent(canvasObject.transform, false);

        panelImage = panelObject.AddComponent<Image>();
        panelImage.raycastTarget = false;

        Sprite panelSprite = UiSprites.GetSliced("panel_wood", 24f);

        if (panelSprite != null)
        {
            panelImage.sprite = panelSprite;
            panelImage.type = Image.Type.Sliced;
            panelImage.color = Color.white;
        }
        else
        {
            panelImage.color = new Color(0f, 0f, 0f, 0.42f);
        }

        RectTransform panelRect = panelImage.rectTransform;
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.sizeDelta = new Vector2(430f, 214f);
        panelRect.anchoredPosition = new Vector2(14f, -14f);

        staminaLabel = CreateRow(panelObject.transform, 0, "icon_stamina", out _);
        weatherLabel = CreateRow(panelObject.transform, 1, "icon_weather_sun", out weatherIcon);
        toolLabel = CreateRow(panelObject.transform, 2, "icon_none", out _);

        CreateEventBanner();

        EnsureLockedTileVisual();
        RefreshAll();
    }

    private TextMeshProUGUI CreateRow(Transform parent, int row, string iconName, out Image iconImage)
    {
        iconImage = null;

        Sprite iconSprite = UiSprites.Get(iconName);

        if (iconSprite != null)
        {
            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(parent, false);

            iconImage = iconObject.AddComponent<Image>();
            iconImage.sprite = iconSprite;
            iconImage.raycastTarget = false;
            iconImage.preserveAspect = true;

            RectTransform iconRect = iconImage.rectTransform;
            iconRect.anchorMin = new Vector2(0f, 1f);
            iconRect.anchorMax = new Vector2(0f, 1f);
            iconRect.pivot = new Vector2(0f, 1f);
            iconRect.sizeDelta = new Vector2(64f, 64f);
            iconRect.anchoredPosition = new Vector2(20f, -12f - row * 62f);
        }

        GameObject textObject = new GameObject("Label");
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Left;
        label.color = new Color(0.96f, 0.91f, 0.8f, 1f);
        label.fontSize = 32;
        label.raycastTarget = false;

        if (font != null)
        {
            label.font = font;
        }

        RectTransform rect = label.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(320f, 50f);
        rect.anchoredPosition = new Vector2(96f, -19f - row * 62f);

        return label;
    }

    private void CreateEventBanner()
    {
        eventRoot = new GameObject("EventBanner");
        eventRoot.transform.SetParent(canvasObject.transform, false);

        eventGroup = eventRoot.AddComponent<CanvasGroup>();
        eventGroup.alpha = 0f;
        eventGroup.blocksRaycasts = false;

        Image background = eventRoot.AddComponent<Image>();
        background.raycastTarget = false;

        Sprite panelSprite = UiSprites.GetSliced("panel_wood", 24f);

        if (panelSprite != null)
        {
            background.sprite = panelSprite;
            background.type = Image.Type.Sliced;
            background.color = new Color(1f, 1f, 1f, 0.94f);
        }
        else
        {
            background.color = new Color(0f, 0f, 0f, 0.6f);
        }

        RectTransform rect = background.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(1180f, 96f);
        rect.anchoredPosition = new Vector2(0f, -200f);

        GameObject textObject = new GameObject("Label");
        textObject.transform.SetParent(eventRoot.transform, false);

        eventLabel = textObject.AddComponent<TextMeshProUGUI>();
        eventLabel.alignment = TextAlignmentOptions.Center;
        eventLabel.fontSize = 31;
        eventLabel.color = new Color(0.98f, 0.94f, 0.84f, 1f);
        eventLabel.raycastTarget = false;

        if (font != null)
        {
            eventLabel.font = font;
        }

        RectTransform textRect = eventLabel.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(30f, 0f);
        textRect.offsetMax = new Vector2(-30f, 0f);

        eventRoot.SetActive(false);
    }

    private void OnFarmEvent(string title, string description, bool disaster)
    {
        LastEventText = "【" + title + "】" + description;

        if (eventLabel == null || eventRoot == null)
        {
            return;
        }

        eventLabel.text = LastEventText;
        eventLabel.color = disaster
            ? new Color(1f, 0.74f, 0.62f, 1f)
            : new Color(0.98f, 0.95f, 0.8f, 1f);

        eventActive = true;
        eventElapsed = 0f;

        eventRoot.SetActive(true);
        ApplyEventLayout(0f);
    }

    private void UpdateEventBanner()
    {
        if (!eventActive || eventRoot == null)
        {
            return;
        }

        eventElapsed += Time.unscaledDeltaTime;
        ApplyEventLayout(eventElapsed);

        if (eventElapsed >= EventRiseTime + EventHoldTime + EventFadeTime)
        {
            eventActive = false;
            eventRoot.SetActive(false);
        }
    }

    private void ApplyEventLayout(float elapsed)
    {
        RectTransform rect = eventRoot.GetComponent<RectTransform>();
        float y;
        float alpha;

        if (elapsed < EventRiseTime)
        {
            float k = Mathf.Clamp01(elapsed / EventRiseTime);
            y = Mathf.Lerp(-286f, -200f, k);
            alpha = k;
        }
        else if (elapsed < EventRiseTime + EventHoldTime)
        {
            y = -200f;
            alpha = 1f;
        }
        else
        {
            float k = Mathf.Clamp01((elapsed - EventRiseTime - EventHoldTime) / EventFadeTime);
            y = Mathf.Lerp(-200f, -70f, k);
            alpha = 1f - k;
        }

        rect.anchoredPosition = new Vector2(0f, y);

        if (eventGroup != null)
        {
            eventGroup.alpha = alpha;
        }
    }

    private void OnItemSelected(Item item, bool selected)
    {
        selectedItem = selected ? item : null;
        RefreshToolRow();
    }

    private void RefreshToolRow()
    {
        if (toolLabel == null)
        {
            return;
        }

        if (selectedItem == null || !ToolDurability.IsTool(selectedItem.itemType))
        {
            toolLabel.text = "未选中工具";
            return;
        }

        ItemType type = selectedItem.itemType;
        int current = ToolDurability.GetCurrent(type);
        int max = ToolDurability.GetMax(type);

        string waterInfo = "";

        if (type == ItemType.WaterCanTool && WaterWellManager.Instance != null)
        {
            waterInfo = "  水 " + WaterWellManager.Instance.Charges + "/" + WaterWellManager.Instance.MaxCharges;
        }

        toolLabel.text = selectedItem.itemName + "  " + current + "/" + max + (ToolDurability.IsDull(type) ? "  钝了" : "") + waterInfo;
    }

    private void RefreshAll()
    {
        PlayerController player = FindObjectOfType<PlayerController>();

        if (player != null)
        {
            OnStaminaChanged(player.CurrentStamina, player.MaxStamina);
        }

        RefreshWeather(FindObjectOfType<TimeManager>());
        RefreshToolRow();
    }

    private void OnStaminaChanged(int current, int max)
    {
        if (staminaLabel == null)
        {
            return;
        }

        staminaLabel.text = current + "/" + max;
    }

    private void OnWeatherChanged(WeatherType weather)
    {
        RefreshWeather(FindObjectOfType<TimeManager>());
    }

    private void RefreshWeather(TimeManager timeManager)
    {
        WeatherType weather = timeManager != null ? timeManager.GetWeather() : WeatherType.Sunny;
        Season season = timeManager != null ? timeManager.GetSeason() : Season.春天;

        if (weatherLabel != null)
        {
            weatherLabel.text = WeatherEffects.GetSeasonName(season) + " " + WeatherEffects.GetName(weather);
        }

        if (weatherIcon != null)
        {
            Sprite sprite = UiSprites.Get(GetWeatherIcon(weather));

            if (sprite != null)
            {
                weatherIcon.sprite = sprite;
            }
        }

        if (rainOverlay != null)
        {
            rainOverlay.color = WeatherEffects.GetOverlayColor(weather);
        }
    }

    private static string GetWeatherIcon(WeatherType weather)
    {
        switch (weather)
        {
            case WeatherType.LightRain:
            case WeatherType.Rainy:
            case WeatherType.HeavyRain:
                return "icon_weather_rain";

            case WeatherType.Thunderstorm:
                return "icon_weather_thunder";

            case WeatherType.Mist:
            case WeatherType.DenseFog:
                return "icon_weather_fog";

            case WeatherType.LightSnow:
            case WeatherType.HeavySnow:
                return "icon_weather_snow";

            case WeatherType.Cloudy:
            case WeatherType.Overcast:
                return "icon_weather_cloud";

            default:
                return "icon_weather_sun";
        }
    }

    private void EnsureLockedTileVisual()
    {
        GridMapMangaer gridMap = GridMapMangaer.Instance;

        if (gridMap == null || gridMap.lockedTile != null)
        {
            return;
        }

        gridMap.SetLockedTile(CreateLockedTile());
    }

    private TileBase CreateLockedTile()
    {
        Texture2D custom = Resources.Load<Texture2D>("Art/Decor/locked_tile");

        if (custom != null)
        {
            custom.filterMode = FilterMode.Point;
            custom.wrapMode = TextureWrapMode.Clamp;

            Sprite customSprite = Sprite.Create(custom, new Rect(0f, 0f, custom.width, custom.height), new Vector2(0.5f, 0.5f), custom.width);

            Tile customTile = ScriptableObject.CreateInstance<Tile>();
            customTile.sprite = customSprite;
            customTile.color = Color.white;

            return customTile;
        }

        const int size = 32;

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color soil = new Color(0.30f, 0.24f, 0.18f, 0.40f);
        Color stripe = new Color(0.17f, 0.13f, 0.10f, 0.55f);
        Color border = new Color(0.66f, 0.47f, 0.27f, 0.85f);
        Color post = new Color(0.86f, 0.68f, 0.42f, 0.95f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isBorder = x < 3 || y < 3 || x >= size - 3 || y >= size - 3;
                Color color;

                if (isBorder)
                {
                    color = border;
                }
                else if (((x + y) % 10) < 3)
                {
                    color = stripe;
                }
                else
                {
                    color = soil;
                }

                texture.SetPixel(x, y, color);
            }
        }

        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                texture.SetPixel(3 + i, 3 + j, post);
                texture.SetPixel(size - 5 + i, 3 + j, post);
                texture.SetPixel(3 + i, size - 5 + j, post);
                texture.SetPixel(size - 5 + i, size - 5 + j, post);
            }
        }

        texture.Apply();

        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);

        Tile tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.color = Color.white;

        return tile;
    }
}