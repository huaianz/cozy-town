using System.Collections.Generic;
using Inventory;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AreaUnlockManager : MonoBehaviour
{
    private const string KeyPrefix = "CozyTown_Area_";
    private const string KeyDaysPlayed = "CozyTown_DaysPlayed";
    private const string OverlayLayer = "Ground Top";
    private const int OverlayOrder = 700;

    public class AreaInfo
    {
        public AreaManager.AreaKind kind;
        public string name;
        public int cost;
        public int unlockDay;
        public int requires = -1;
        public bool hasRect;
        public Rect rect;
        public GameObject root;
    }

    private static AreaUnlockManager instance;
    private readonly List<AreaInfo> areas = new List<AreaInfo>();
    private Sprite tileSprite;
    private float nextResolveTime;

    public static AreaUnlockManager Instance
    {
        get { return instance; }
    }

    public IList<AreaInfo> Areas
    {
        get { return areas; }
    }

    public int DaysPlayed
    {
        get { return Mathf.Max(1, PlayerPrefs.GetInt(KeyDaysPlayed, 1)); }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (instance != null)
        {
            return;
        }

        GameObject go = new GameObject("AreaUnlockManager");
        DontDestroyOnLoad(go);
        go.AddComponent<AreaUnlockManager>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        BuildAreas();

        tileSprite = MakeTiledSprite("Art/Decor/locked_tile");

        EventHandler.GameDayEvent += OnGameDay;
        SceneManager.sceneLoaded += OnSceneLoaded;

        Refresh();
    }

    private void OnDestroy()
    {
        EventHandler.GameDayEvent -= OnGameDay;
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (instance == this)
        {
            instance = null;
        }
    }

    private void BuildAreas()
    {
        areas.Clear();
        areas.Add(NewArea(AreaManager.AreaKind.Orchard, "果园", 300, 3, -1));
        areas.Add(NewArea(AreaManager.AreaKind.Mine, "矿洞", 800, 8, 0));
        areas.Add(NewArea(AreaManager.AreaKind.Pond, "鱼塘", 1500, 15, 1));
    }

    private static AreaInfo NewArea(AreaManager.AreaKind kind, string name, int cost, int unlockDay, int requires)
    {
        AreaInfo area = new AreaInfo();
        area.kind = kind;
        area.name = name;
        area.cost = cost;
        area.unlockDay = unlockDay;
        area.requires = requires;
        return area;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Refresh();
    }

    private void OnGameDay(int day, Season season)
    {
        PlayerPrefs.SetInt(KeyDaysPlayed, DaysPlayed + 1);
        PlayerPrefs.Save();

        Refresh();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextResolveTime)
        {
            return;
        }

        nextResolveTime = Time.unscaledTime + 0.5f;

        if (AreaManager.Instance == null)
        {
            return;
        }

        for (int i = 0; i < areas.Count; i++)
        {
            if (!areas[i].hasRect && !IsUnlocked(areas[i].kind))
            {
                Refresh();
                return;
            }
        }
    }

    public static void ResetProgress()
    {
        PlayerPrefs.DeleteKey(KeyPrefix + AreaManager.AreaKind.Orchard);
        PlayerPrefs.DeleteKey(KeyPrefix + AreaManager.AreaKind.Mine);
        PlayerPrefs.DeleteKey(KeyPrefix + AreaManager.AreaKind.Pond);
        PlayerPrefs.SetInt(KeyDaysPlayed, 1);
        PlayerPrefs.Save();

        if (instance != null)
        {
            instance.Refresh();
        }

        AreaUnlockPanelUI.RefreshIfOpen();
    }

    public bool IsUnlocked(AreaManager.AreaKind kind)
    {
        if (kind == AreaManager.AreaKind.Farm)
        {
            return true;
        }

        return PlayerPrefs.GetInt(KeyPrefix + kind, 0) == 1;
    }

    public string BlockReason(AreaInfo area)
    {
        if (IsUnlocked(area.kind))
        {
            return null;
        }

        if (area.requires >= 0 && area.requires < areas.Count)
        {
            AreaInfo previous = areas[area.requires];

            if (!IsUnlocked(previous.kind))
            {
                return "要先开荒" + previous.name;
            }
        }

        if (DaysPlayed < area.unlockDay)
        {
            return "第 " + area.unlockDay + " 天起可开荒";
        }

        int money = ShopManager.Instance != null ? ShopManager.Instance.PlayerMoney : 0;

        if (money < area.cost)
        {
            return "需要 " + area.cost + " 金币，还差 " + (area.cost - money) + " 金币";
        }

        return null;
    }

    public bool Unlock(AreaInfo area)
    {
        if (BlockReason(area) != null)
        {
            EventHandler.CallFarmEventEvent("开荒", area.name + "：" + BlockReason(area), true);
            return false;
        }

        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.PlayerMoney -= area.cost;
            EventHandler.CallUpdateMoneyEvent(ShopManager.Instance.PlayerMoney);
        }

        PlayerPrefs.SetInt(KeyPrefix + area.kind, 1);
        PlayerPrefs.Save();

        ClearOverlay(area);

        EventHandler.CallFarmEventEvent("开荒", area.name + "开荒完成，可以过去了", false);
        AreaUnlockPanelUI.RefreshIfOpen();

        return true;
    }

    public void OpenPanel(AreaManager.AreaKind kind)
    {
        AreaUnlockPanelUI.Open();
    }

    public string BlockedNameAt(Vector3 worldPosition)
    {
        for (int i = 0; i < areas.Count; i++)
        {
            AreaInfo area = areas[i];

            if (!area.hasRect || IsUnlocked(area.kind))
            {
                continue;
            }

            if (area.rect.Contains(new Vector2(worldPosition.x, worldPosition.y)))
            {
                return area.name;
            }
        }

        return null;
    }

    public void Refresh()
    {
        for (int i = 0; i < areas.Count; i++)
        {
            AreaInfo area = areas[i];
            area.hasRect = ResolveRect(area);

            if (IsUnlocked(area.kind))
            {
                ClearOverlay(area);
            }
            else
            {
                BuildOverlay(area);
            }
        }
    }

    private bool ResolveRect(AreaInfo area)
    {
        if (area.kind == AreaManager.AreaKind.Orchard)
        {
            Rect orchard;

            if (TryFruitTreeBounds(out orchard))
            {
                area.rect = orchard;
                return true;
            }
        }

        AreaManager manager = AreaManager.Instance;

        if (manager != null)
        {
            Rect rect;

            if (manager.TryGetBounds(area.kind, out rect) && rect.width > 0.1f)
            {
                area.rect = rect;
                return true;
            }
        }

        return false;
    }

    private static bool TryFruitTreeBounds(out Rect rect)
    {
        rect = new Rect();

        GameObject[] trees;

        try
        {
            trees = GameObject.FindGameObjectsWithTag("fruit tree");
        }
        catch (UnityException)
        {
            return false;
        }

        if (trees == null || trees.Length == 0)
        {
            return false;
        }

        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;

        for (int i = 0; i < trees.Length; i++)
        {
            Vector3 position = trees[i].transform.position;

            if (position.x < minX) minX = position.x;
            if (position.x > maxX) maxX = position.x;
            if (position.y < minY) minY = position.y;
            if (position.y > maxY) maxY = position.y;
        }

        rect = new Rect(minX - 3f, minY - 3f, (maxX - minX) + 6f, (maxY - minY) + 6f);
        return true;
    }

    private void BuildOverlay(AreaInfo area)
    {
        ClearOverlay(area);

        if (!area.hasRect || tileSprite == null)
        {
            return;
        }

        GameObject root = new GameObject("Locked_" + area.kind);
        area.root = root;

        GameObject tile = new GameObject("Tile");
        tile.transform.SetParent(root.transform, false);
        tile.transform.position = new Vector3(area.rect.center.x, area.rect.center.y, 0f);

        SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
        renderer.sprite = tileSprite;
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.size = new Vector2(area.rect.width, area.rect.height);
        renderer.sortingLayerName = OverlayLayer;
        renderer.sortingOrder = OverlayOrder;
        renderer.color = new Color(1f, 1f, 1f, 0.78f);
    }

    private void ClearOverlay(AreaInfo area)
    {
        if (area.root != null)
        {
            Destroy(area.root);
            area.root = null;
        }
    }

    private static Sprite MakeTiledSprite(string path)
    {
        Texture2D texture = Resources.Load<Texture2D>(path);

        if (texture == null)
        {
            return null;
        }

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        int border = Mathf.Max(1, Mathf.Min(texture.width, texture.height) / 4);

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            16f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(border, border, border, border));
    }
}