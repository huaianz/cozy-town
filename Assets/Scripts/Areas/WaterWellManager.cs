using UnityEngine;

public class WaterWellManager : MonoBehaviour
{
    private const string KeyCharges = "CozyTown_WaterCharges";
    private const float WellSearchRadius = 0.6f;

    private static WaterWellManager instance;

    public static WaterWellManager Instance
    {
        get { return instance; }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (instance != null)
        {
            return;
        }

        GameObject go = new GameObject("WaterWellManager");
        DontDestroyOnLoad(go);
        go.AddComponent<WaterWellManager>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    public int MaxCharges
    {
        get { return 2 + Mathf.Max(0, ToolDurability.GetLevel(ItemType.WaterCanTool) - 1); }
    }

    public int Charges
    {
        get { return Mathf.Clamp(PlayerPrefs.GetInt(KeyCharges, MaxCharges), 0, MaxCharges); }
    }

    public bool HasWater
    {
        get { return Charges > 0; }
    }

    public static void ResetCharges()
    {
        PlayerPrefs.DeleteKey(KeyCharges);
        PlayerPrefs.Save();
    }

    public bool IsWell(Vector3 worldPosition)
    {
        if (HitsWell(Physics2D.OverlapPointAll(worldPosition, Physics2D.AllLayers)))
        {
            return true;
        }

        return HitsWell(Physics2D.OverlapCircleAll(worldPosition, WellSearchRadius, Physics2D.AllLayers));
    }

    public Vector3 WellCenter()
    {
        Collider2D[] all = FindObjectsOfType<Collider2D>();

        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null || !NameIsWell(all[i].gameObject.name))
            {
                continue;
            }

            return all[i].bounds.center;
        }

        return Vector3.zero;
    }

    public bool TryGetWellBounds(out Rect rect)
    {
        rect = new Rect();

        bool found = false;
        float minX = 0f;
        float minY = 0f;
        float maxX = 0f;
        float maxY = 0f;

        Collider2D[] colliders = FindObjectsOfType<Collider2D>();

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] == null || !NameIsWell(colliders[i].gameObject.name))
            {
                continue;
            }

            Include(ref found, ref minX, ref minY, ref maxX, ref maxY, colliders[i].bounds);

            SpriteRenderer[] children = colliders[i].GetComponentsInChildren<SpriteRenderer>(true);

            for (int c = 0; c < children.Length; c++)
            {
                if (children[c] != null)
                {
                    Include(ref found, ref minX, ref minY, ref maxX, ref maxY, children[c].bounds);
                }
            }

            SpriteRenderer parent = colliders[i].GetComponentInParent<SpriteRenderer>();

            if (parent != null)
            {
                Include(ref found, ref minX, ref minY, ref maxX, ref maxY, parent.bounds);
            }
        }

        if (!found)
        {
            return false;
        }

        rect = new Rect(minX, minY, maxX - minX, maxY - minY);
        return true;
    }

    private static void Include(ref bool found, ref float minX, ref float minY, ref float maxX, ref float maxY, Bounds bounds)
    {
        if (bounds.size.x <= 0.01f && bounds.size.y <= 0.01f)
        {
            return;
        }

        if (!found)
        {
            found = true;
            minX = bounds.min.x;
            minY = bounds.min.y;
            maxX = bounds.max.x;
            maxY = bounds.max.y;
            return;
        }

        minX = Mathf.Min(minX, bounds.min.x);
        minY = Mathf.Min(minY, bounds.min.y);
        maxX = Mathf.Max(maxX, bounds.max.x);
        maxY = Mathf.Max(maxY, bounds.max.y);
    }

    public bool TryFill()
    {
        int max = MaxCharges;

        if (Charges >= max)
        {
            EventHandler.CallFarmEventEvent("水井", "水壶是满的（可以浇 " + max + " 次）", true);
            return true;
        }

        PlayerPrefs.SetInt(KeyCharges, max);
        PlayerPrefs.Save();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(SfxType.Water);
        }

        EventHandler.CallFarmEventEvent("水井", "打满水了，可以浇 " + max + " 次地", false);

        return true;
    }

    public void UseCharge()
    {
        int charges = Charges;

        if (charges <= 0)
        {
            return;
        }

        PlayerPrefs.SetInt(KeyCharges, charges - 1);
        PlayerPrefs.Save();
    }

    private static bool HitsWell(Collider2D[] hits)
    {
        if (hits == null)
        {
            return false;
        }

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null)
            {
                continue;
            }

            Transform check = hits[i].transform;

            for (int depth = 0; depth < 4 && check != null; depth++)
            {
                if (NameIsWell(check.gameObject.name))
                {
                    return true;
                }

                check = check.parent;
            }
        }

        return false;
    }

    private static bool NameIsWell(string name)
    {
        string lower = name.ToLower();

        return lower.Contains("well") || lower.Contains("井");
    }
}