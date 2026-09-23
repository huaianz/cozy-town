using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeatherParticles : MonoBehaviour
{
    private const int AirCount = 44;
    private const int FogCount = 8;

    private static WeatherParticles instance;

    private GameObject canvasObject;
    private RectTransform canvasRect;
    private Image flashImage;

    private readonly List<Image> airImages = new List<Image>();
    private readonly List<RectTransform> airRects = new List<RectTransform>();
    private readonly List<float> airX = new List<float>();
    private readonly List<float> airSpeed = new List<float>();
    private readonly List<float> airSway = new List<float>();
    private readonly List<float> airFrequency = new List<float>();
    private readonly List<float> airPhase = new List<float>();
    private readonly List<float> airSpin = new List<float>();

    private readonly List<Image> fogImages = new List<Image>();
    private readonly List<RectTransform> fogRects = new List<RectTransform>();
    private readonly List<float> fogSpeed = new List<float>();
    private readonly List<float> fogBaseAlpha = new List<float>();

    private Sprite[] petalSprites;
    private Sprite[] leafSprites;
    private Sprite[] snowSprites;
    private Sprite fogSprite;

    private int airMode = -1;
    private bool lastRaining;
    private bool raining;
    private bool thunder;
    private float fogTarget;
    private float fogCurrent;
    private float alphaScale = 1f;
    private float flashTimer;
    private float nextFlash = 8f;
    private float checkTimer;
    private float lifeTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (instance != null)
        {
            return;
        }

        GameObject go = new GameObject("WeatherParticles");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<WeatherParticles>();
    }

    private void Update()
    {
        float delta = Time.unscaledDeltaTime;

        checkTimer += delta;

        if (checkTimer >= 0.5f)
        {
            checkTimer = 0f;
            Poll();
        }

        if (canvasObject == null)
        {
            return;
        }

        lifeTime += delta;
        fogCurrent = Mathf.MoveTowards(fogCurrent, fogTarget, delta * 0.7f);

        bool visible = airMode > 0 || fogCurrent > 0.01f || flashTimer > 0f;

        if (canvasObject.activeSelf != visible)
        {
            canvasObject.SetActive(visible);
        }

        if (!visible)
        {
            return;
        }

        MoveAir(delta);
        MoveFog(delta);
        UpdateFlash(delta);
    }

    private void Poll()
    {
        if (canvasObject == null)
        {
            if (FindObjectOfType<GridMapMangaer>() == null)
            {
                return;
            }

            Build();
        }

        TimeManager timeManager = FindObjectOfType<TimeManager>();

        if (timeManager == null)
        {
            return;
        }

        WeatherType weather = timeManager.GetWeather();
        Season season = timeManager.GetSeason();

        raining = WeatherEffects.IsRainy(weather);
        thunder = weather == WeatherType.Thunderstorm;

        int mode = 0;

        if (WeatherEffects.IsSnowy(weather))
        {
            mode = 3;
        }
        else if (season == Season.´ºÌì)
        {
            mode = 1;
        }
        else if (season == Season.ÇïÌì)
        {
            mode = 2;
        }

        SetAirMode(mode);

        fogTarget = WeatherEffects.IsFoggy(weather)
            ? (weather == WeatherType.DenseFog ? 1f : 0.55f)
            : 0f;
    }

    private void Build()
    {
        canvasObject = new GameObject("WeatherParticleCanvas");

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 580;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        DontDestroyOnLoad(canvasObject);

        canvasRect = canvasObject.GetComponent<RectTransform>();

        petalSprites = new Sprite[] { UiSprites.Get("particle_petal_1"), UiSprites.Get("particle_petal_2") };
        leafSprites = new Sprite[] { UiSprites.Get("particle_leaf_1"), UiSprites.Get("particle_leaf_2") };
        snowSprites = new Sprite[] { UiSprites.Get("particle_snow_1"), UiSprites.Get("particle_snow_2") };
        fogSprite = UiSprites.Get("particle_fog_band");

        for (int i = 0; i < FogCount; i++)
        {
            CreateFog(i);
        }

        for (int i = 0; i < AirCount; i++)
        {
            CreateAir();
        }

        GameObject flashObject = new GameObject("LightningFlash");
        flashObject.transform.SetParent(canvasObject.transform, false);

        flashImage = flashObject.AddComponent<Image>();
        flashImage.color = new Color(1f, 1f, 1f, 0f);
        flashImage.raycastTarget = false;

        RectTransform flashRect = flashImage.rectTransform;
        flashRect.anchorMin = Vector2.zero;
        flashRect.anchorMax = Vector2.one;
        flashRect.offsetMin = Vector2.zero;
        flashRect.offsetMax = Vector2.zero;

        SetAirMode(0);
    }

    private void CreateAir()
    {
        GameObject particleObject = new GameObject("Particle");
        particleObject.transform.SetParent(canvasObject.transform, false);

        Image image = particleObject.AddComponent<Image>();
        image.raycastTarget = false;
        image.color = new Color(1f, 1f, 1f, 0f);

        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(18f, 18f);
        rect.anchoredPosition = new Vector2(Random.Range(-1000f, 1000f), Random.Range(-600f, 600f));

        airImages.Add(image);
        airRects.Add(rect);
        airX.Add(rect.anchoredPosition.x);
        airSpeed.Add(Random.Range(60f, 140f));
        airSway.Add(Random.Range(18f, 46f));
        airFrequency.Add(Random.Range(0.6f, 1.6f));
        airPhase.Add(Random.Range(0f, 6.28f));
        airSpin.Add(Random.Range(-70f, 70f));
    }

    private void CreateFog(int index)
    {
        GameObject bandObject = new GameObject("FogBand");
        bandObject.transform.SetParent(canvasObject.transform, false);

        Image image = bandObject.AddComponent<Image>();
        image.sprite = fogSprite;
        image.raycastTarget = false;
        image.color = new Color(1f, 1f, 1f, 0f);

        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        float height = Random.Range(90f, 260f);
        rect.sizeDelta = new Vector2(1600f, height);
        rect.anchoredPosition = new Vector2(
            Random.Range(-900f, 900f),
            -540f + index * (1080f / FogCount) + Random.Range(-30f, 30f));

        fogImages.Add(image);
        fogRects.Add(rect);
        fogSpeed.Add(Random.Range(-24f, 24f));
        fogBaseAlpha.Add(Random.Range(0.10f, 0.22f));
    }

    private void SetAirMode(int mode)
    {
        if (airMode == mode && lastRaining == raining)
        {
            return;
        }

        airMode = mode;
        lastRaining = raining;
        alphaScale = raining ? 0.55f : 1f;

        for (int i = 0; i < airImages.Count; i++)
        {
            if (mode <= 0)
            {
                airImages[i].color = new Color(1f, 1f, 1f, 0f);
                continue;
            }

            Sprite sprite = PickSprite(mode, i);

            if (sprite != null)
            {
                airImages[i].sprite = sprite;
            }

            float size = mode == 3 ? Random.Range(16f, 24f) : Random.Range(18f, 28f);
            airRects[i].sizeDelta = new Vector2(size, size);
            airSpeed[i] = mode == 3 ? Random.Range(40f, 95f) : Random.Range(55f, 125f);
            airSway[i] = Random.Range(16f, 44f);
            airSpin[i] = mode == 3 ? Random.Range(-40f, 40f) : Random.Range(-70f, 70f);

            float alpha = mode == 3 ? Random.Range(0.55f, 0.9f) : Random.Range(0.7f, 0.95f);
            airImages[i].color = new Color(1f, 1f, 1f, alpha * alphaScale);
        }
    }

    private Sprite PickSprite(int mode, int index)
    {
        Sprite[] pool = mode == 1 ? petalSprites : (mode == 2 ? leafSprites : snowSprites);

        if (pool == null || pool.Length == 0)
        {
            return null;
        }

        Sprite sprite = pool[index % pool.Length];

        if (sprite == null)
        {
            sprite = pool[Random.Range(0, pool.Length)];
        }

        return sprite;
    }

    private void MoveAir(float delta)
    {
        if (airMode <= 0)
        {
            return;
        }

        float halfWidth = 1000f;
        float halfHeight = 600f;

        if (canvasRect != null)
        {
            halfWidth = canvasRect.rect.width * 0.5f + 60f;
            halfHeight = canvasRect.rect.height * 0.5f + 60f;
        }

        float speedScale = raining ? 1.35f : 1f;

        for (int i = 0; i < airRects.Count; i++)
        {
            RectTransform rect = airRects[i];

            if (rect == null)
            {
                continue;
            }

            Vector2 position = rect.anchoredPosition;
            position.y -= airSpeed[i] * speedScale * delta;

            if (position.y < -halfHeight)
            {
                position.y = halfHeight + Random.Range(10f, 140f);
                airX[i] = Random.Range(-halfWidth, halfWidth);
            }

            position.x = airX[i] + Mathf.Sin(lifeTime * airFrequency[i] + airPhase[i]) * airSway[i];

            rect.anchoredPosition = position;
            rect.localRotation = Quaternion.Euler(0f, 0f, lifeTime * airSpin[i] + airPhase[i] * 57.3f);
        }
    }

    private void MoveFog(float delta)
    {
        if (fogRects.Count == 0 || fogCurrent <= 0.01f)
        {
            return;
        }

        float halfWidth = 1000f;

        if (canvasRect != null)
        {
            halfWidth = canvasRect.rect.width * 0.5f;
        }

        for (int i = 0; i < fogRects.Count; i++)
        {
            RectTransform rect = fogRects[i];

            if (rect == null)
            {
                continue;
            }

            Vector2 position = rect.anchoredPosition;
            position.x += fogSpeed[i] * delta;

            if (position.x > halfWidth + 900f)
            {
                position.x = -halfWidth - 900f;
            }
            else if (position.x < -halfWidth - 900f)
            {
                position.x = halfWidth + 900f;
            }

            rect.anchoredPosition = position;

            Color color = fogImages[i].color;
            color.a = fogBaseAlpha[i] * fogCurrent;
            fogImages[i].color = color;
        }
    }

    private void UpdateFlash(float delta)
    {
        if (flashImage == null)
        {
            return;
        }

        if (thunder)
        {
            nextFlash -= delta;

            if (nextFlash <= 0f)
            {
                flashTimer = 0.36f;
                nextFlash = Random.Range(6f, 16f);
            }
        }
        else if (flashTimer > 0f)
        {
            flashTimer = 0f;
        }

        float alpha = 0f;

        if (flashTimer > 0f)
        {
            flashTimer -= delta;
            float t = Mathf.Clamp01(flashTimer / 0.36f);
            alpha = Mathf.Sin(t * Mathf.PI) * 0.40f;
        }

        flashImage.color = new Color(1f, 1f, 1f, alpha);
    }
}