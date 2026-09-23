using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RainEffect : MonoBehaviour
{
    private const int DropCount = 90;
    private const float MinSpeed = 720f;
    private const float MaxSpeed = 1500f;
    private const float MinLength = 18f;
    private const float MaxLength = 46f;

    private static RainEffect instance;

    private GameObject canvasObject;
    private RectTransform canvasRect;
    private Sprite dropSprite;
    private readonly List<RectTransform> drops = new List<RectTransform>();
    private readonly List<float> speeds = new List<float>();
    private readonly List<float> drifts = new List<float>();
    private bool raining;
    private float checkTimer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (instance != null)
        {
            return;
        }

        GameObject go = new GameObject("RainEffect");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<RainEffect>();
    }

    private void OnEnable()
    {
        EventHandler.WeatherChangedEvent += OnWeatherChanged;
    }

    private void OnDisable()
    {
        EventHandler.WeatherChangedEvent -= OnWeatherChanged;
    }

    private void OnWeatherChanged(WeatherType weather)
    {
        raining = WeatherEffects.IsRainy(weather);
    }

    private void Update()
    {
        checkTimer += Time.unscaledDeltaTime;

        if (checkTimer >= 0.5f)
        {
            checkTimer = 0f;

            bool inGame = FindObjectOfType<GridMapMangaer>() != null;

            TimeManager timeManager = FindObjectOfType<TimeManager>();

            if (timeManager != null)
            {
                WeatherType current = timeManager.GetWeather();
                raining = WeatherEffects.IsRainy(current);
            }

            if (inGame && drops.Count == 0)
            {
                Build();
            }

            bool visible = inGame && raining;

            if (canvasObject != null && canvasObject.activeSelf != visible)
            {
                canvasObject.SetActive(visible);
            }
        }

        if (!raining || canvasObject == null || !canvasObject.activeSelf)
        {
            return;
        }

        MoveDrops();
    }

    private void Build()
    {
        canvasObject = new GameObject("RainCanvas");

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 600;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        DontDestroyOnLoad(canvasObject);

        canvasRect = canvasObject.GetComponent<RectTransform>();
        dropSprite = CreateDropSprite();

        for (int i = 0; i < DropCount; i++)
        {
            CreateDrop(true);
        }
    }

    private void CreateDrop(bool spread)
    {
        GameObject dropObject = new GameObject("Drop");
        dropObject.transform.SetParent(canvasObject.transform, false);

        Image image = dropObject.AddComponent<Image>();
        image.sprite = dropSprite;
        image.raycastTarget = false;
        image.color = new Color(1f, 1f, 1f, Random.Range(0.35f, 0.75f));

        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 1f);

        float width = Random.Range(4f, 7f);
        float height = Random.Range(MinLength, MaxLength);
        rect.sizeDelta = new Vector2(width, height);

        float halfWidth = 1000f;
        float halfHeight = 600f;

        float y = spread ? Random.Range(-halfHeight, halfHeight) : Random.Range(halfHeight, halfHeight * 1.6f);

        rect.anchoredPosition = new Vector2(Random.Range(-halfWidth, halfWidth), y);

        drops.Add(rect);
        speeds.Add(Random.Range(MinSpeed, MaxSpeed));
        drifts.Add(Random.Range(-90f, -20f));
    }

    private void MoveDrops()
    {
        float halfWidth = 1000f;
        float halfHeight = 600f;

        if (canvasRect != null)
        {
            halfWidth = canvasRect.rect.width * 0.5f;
            halfHeight = canvasRect.rect.height * 0.5f;
        }

        float delta = Time.unscaledDeltaTime;

        for (int i = 0; i < drops.Count; i++)
        {
            RectTransform drop = drops[i];

            if (drop == null)
            {
                continue;
            }

            Vector2 position = drop.anchoredPosition;
            position.y -= speeds[i] * delta;
            position.x += drifts[i] * delta;

            if (position.y < -halfHeight - 80f || position.x < -halfWidth - 80f)
            {
                position.y = halfHeight + Random.Range(40f, halfHeight * 0.6f);
                position.x = Random.Range(-halfWidth, halfWidth * 1.25f);

                drop.sizeDelta = new Vector2(Random.Range(4f, 7f), Random.Range(MinLength, MaxLength));
                speeds[i] = Random.Range(MinSpeed, MaxSpeed);
            }

            drop.anchoredPosition = position;
        }
    }

    private Sprite CreateDropSprite()
    {
        const int width = 6;
        const int height = 24;

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < height; y++)
        {
            float t = y / (float)(height - 1);
            float alpha = Mathf.Lerp(0.08f, 0.75f, t);

            for (int x = 0; x < width; x++)
            {
                bool core = x >= 2 && x <= 3;
                float a = core ? alpha : alpha * 0.3f;

                if (core && y >= height - 5)
                {
                    a = 1f;
                }

                texture.SetPixel(x, y, new Color(0.80f, 0.90f, 1f, a));
            }
        }

        texture.Apply();

        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 1f), 100f, 0, SpriteMeshType.FullRect);
    }
}