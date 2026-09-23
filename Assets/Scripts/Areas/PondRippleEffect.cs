using System.Collections.Generic;
using UnityEngine;

public class PondRippleEffect : MonoBehaviour
{
    private const int PoolSize = 32;
    private const float LifeTime = 0.85f;
    private const string SortingLayerName = "Ground Top";
    private const int SortingOrder = 480;

    private static PondRippleEffect instance;

    private readonly List<SpriteRenderer> ripples = new List<SpriteRenderer>();
    private readonly List<float> ages = new List<float>();

    private Bounds pondBounds;
    private bool pondFound;
    private bool raining;
    private float intensity = 1f;
    private float spawnTimer;
    private float checkTimer;
    private Sprite ringSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (instance != null)
        {
            return;
        }

        GameObject go = new GameObject("PondRippleEffect");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<PondRippleEffect>();
    }

    private void Update()
    {
        float delta = Time.deltaTime;

        checkTimer += delta;

        if (checkTimer >= 0.5f)
        {
            checkTimer = 0f;
            Poll();
        }

        if (!raining || !pondFound)
        {
            return;
        }

        spawnTimer -= delta;

        if (spawnTimer <= 0f)
        {
            SpawnRipple();
            spawnTimer = Mathf.Lerp(0.95f, 0.25f, Mathf.Clamp01(intensity / 1.3f));
        }

        AnimateRipples(delta);
    }

    private void Poll()
    {
        if (ringSprite == null)
        {
            ringSprite = CreateRingSprite();
        }

        if (ripples.Count == 0)
        {
            BuildPool();
        }

        if (!pondFound)
        {
            FindPond();
        }

        TimeManager timeManager = FindObjectOfType<TimeManager>();

        if (timeManager != null)
        {
            WeatherType weather = timeManager.GetWeather();
            raining = WeatherEffects.IsRainy(weather);
            intensity = WeatherEffects.GetRainIntensity(weather);
        }
        else
        {
            raining = false;
        }
    }

    private void FindPond()
    {
        Collider2D[] colliders = FindObjectsOfType<Collider2D>();

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D collider = colliders[i];

            if (collider == null)
            {
                continue;
            }

            string name = collider.gameObject.name.ToLower();
            string tag = collider.gameObject.tag.ToLower();

            if (name.Contains("pond") || name.Contains("water") || name.Contains("Óã") || name.Contains("ÌÁ") || tag.Contains("water"))
            {
                pondBounds = collider.bounds;
                pondFound = true;

                Debug.Log("[ÓãÌÁ] ÓêµÎ²¨ÎÆ·¶Î§£º" + pondBounds.center + " ´óÐ¡ " + pondBounds.size);

                return;
            }
        }
    }

    private void BuildPool()
    {
        for (int i = 0; i < PoolSize; i++)
        {
            GameObject rippleObject = new GameObject("Ripple");
            rippleObject.transform.SetParent(transform, false);

            SpriteRenderer renderer = rippleObject.AddComponent<SpriteRenderer>();
            renderer.sprite = ringSprite;
            renderer.sortingLayerName = SortingLayerName;
            renderer.sortingOrder = SortingOrder;
            renderer.enabled = false;

            ripples.Add(renderer);
            ages.Add(LifeTime);
        }
    }

    private void SpawnRipple()
    {
        if (ripples.Count == 0 || ringSprite == null)
        {
            return;
        }

        int index = -1;

        for (int i = 0; i < ripples.Count; i++)
        {
            if (ages[i] >= LifeTime)
            {
                index = i;
                break;
            }
        }

        if (index < 0)
        {
            return;
        }

        float x = Random.Range(pondBounds.min.x + 0.5f, pondBounds.max.x - 0.5f);
        float y = Random.Range(pondBounds.min.y + 0.5f, pondBounds.max.y - 0.5f);

        SpriteRenderer ripple = ripples[index];
        ripple.transform.position = new Vector3(x, y, 0f);
        ripple.transform.localScale = new Vector3(0.25f, 0.25f, 1f);
        ripple.color = new Color(1f, 1f, 1f, 0.65f);
        ripple.enabled = true;

        ages[index] = 0f;
    }

    private void AnimateRipples(float delta)
    {
        for (int i = 0; i < ripples.Count; i++)
        {
            if (ages[i] >= LifeTime)
            {
                continue;
            }

            ages[i] += delta;

            float t = Mathf.Clamp01(ages[i] / LifeTime);
            float scale = Mathf.Lerp(0.25f, 1.5f, t);
            float alpha = (1f - t) * 0.65f;

            SpriteRenderer ripple = ripples[i];
            ripple.transform.localScale = new Vector3(scale, scale * 0.55f, 1f);

            Color color = ripple.color;
            color.a = alpha;
            ripple.color = color;

            if (t >= 1f)
            {
                ripple.enabled = false;
            }
        }
    }

    private static Sprite CreateRingSprite()
    {
        const int size = 32;

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color clear = new Color(0f, 0f, 0f, 0f);
        float center = (size - 1) * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                float band = Mathf.Abs(distance - 12f);

                if (band <= 1.6f)
                {
                    float alpha = Mathf.Lerp(1f, 0.35f, band / 1.6f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else
                {
                    texture.SetPixel(x, y, clear);
                }
            }
        }

        texture.Apply();

        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }
}