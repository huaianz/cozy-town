using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class AreaManager : MonoBehaviour
{
    public enum AreaKind
    {
        Farm,
        Orchard,
        Mine,
        Pond
    }

    private const int AppleTreeSeedID = 36;
    private const bool GenerateAreas = false;

    private class Area
    {
        public AreaKind kind;
        public string name;
        public Vector2 center;
        public Vector2 size;
        public bool open;
        public bool built;
    }

    private static AreaManager instance;

    private readonly List<Area> areas = new List<Area>();
    private TileBase grassTile;
    private TileBase waterTile;
    private TileBase rockGroundTile;
    private TileBase fenceTile;
    private TileBase[] grassVariants;
    private TileBase[] decorVariants;
    private TileBase[] flowerTiles;
    private TileBase[] tileSamples;
    private TileBase[] decorSamples;
    private readonly List<Vector2Int> oreCells = new List<Vector2Int>();
    private readonly HashSet<int> pondWaterCells = new HashSet<int>();
    private bool ready;

    public static AreaManager Instance
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

        GameObject go = new GameObject("AreaManager");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<AreaManager>();
    }

    private void OnEnable()
    {
        EventHandler.RefreshCurrentMap += OnRefreshMap;
        EventHandler.GameDayEvent += OnGameDay;
    }

    private void OnDisable()
    {
        EventHandler.RefreshCurrentMap -= OnRefreshMap;
        EventHandler.GameDayEvent -= OnGameDay;
    }

    private void OnGameDay(int day, Season season)
    {
        RespawnOres();
    }

    private void RespawnOres()
    {
        GridMapMangaer map = GridMapMangaer.Instance;

        if (map == null || oreCells.Count == 0)
        {
            return;
        }

        for (int i = 0; i < oreCells.Count; i++)
        {
            Vector2Int cell = oreCells[i];
            TileDetails tile = map.RegisterGeneratedTile(cell.x, cell.y);

            if (tile.hasObstacle)
            {
                continue;
            }

            tile.isOreNode = true;
            tile.hasObstacle = true;
            tile.obstacleType = ObstacleType.Rock;
            tile.canDig = false;

            map.RefreshTileVisualFor(tile);
        }
    }

    private void OnRefreshMap()
    {
        for (int i = 0; i < areas.Count; i++)
        {
            areas[i].built = false;
        }
    }

    private void Update()
    {
        if (ready)
        {
            return;
        }

        if (FindObjectOfType<GridMapMangaer>() == null)
        {
            return;
        }

        ready = true;
        CreateAreas();
    }

    private void CreateAreas()
    {
        areas.Clear();

        areas.Add(new Area { kind = AreaKind.Farm, name = "农田", center = new Vector2(-0.5f, -0.5f), size = new Vector2(24f, 12f), open = true, built = true });
        areas.Add(new Area { kind = AreaKind.Orchard, name = "果园", center = new Vector2(60f, 0f), size = new Vector2(40f, 24f), open = IsAreaUnlocked(AreaKind.Orchard) });
        areas.Add(new Area { kind = AreaKind.Mine, name = "矿洞", center = new Vector2(-60f, 0f), size = new Vector2(20f, 12f), open = IsAreaUnlocked(AreaKind.Mine) });
        areas.Add(new Area { kind = AreaKind.Pond, name = "鱼塘", center = new Vector2(0f, -52f), size = new Vector2(22f, 14f), open = IsAreaUnlocked(AreaKind.Pond) });

        ApplyDefaultView();
    }

    private static bool IsAreaUnlocked(AreaKind kind)
    {
        return AreaUnlockManager.Instance == null || AreaUnlockManager.Instance.IsUnlocked(kind);
    }

    public bool TryGetBounds(AreaKind kind, out Rect rect)
    {
        Area area = Find(kind);

        if (area == null)
        {
            rect = new Rect();
            return false;
        }

        rect = new Rect(
            area.center.x - area.size.x * 0.5f,
            area.center.y - area.size.y * 0.5f,
            area.size.x,
            area.size.y);

        return true;
    }

    public void GoTo(AreaKind kind)
    {
        Area area = Find(kind);

        if (area == null)
        {
            return;
        }

        if (!area.open && IsAreaUnlocked(kind))
        {
            area.open = true;
        }

        if (!area.open)
        {
            if (AreaUnlockManager.Instance != null)
            {
                AreaUnlockManager.Instance.OpenPanel(kind);
            }
            else
            {
                EventHandler.CallFarmEventEvent(area.name, "这个区域还在施工中，先期待一下", false);
            }

            return;
        }

        EnsureBuilt(area);

        Rect view = GetViewRect(area);

        Camera mainCamera = Camera.main;
        CameraController controller = mainCamera != null ? mainCamera.GetComponent<CameraController>() : null;

        if (controller != null)
        {
            controller.SetArea(view.center, view.size);
        }
        else if (mainCamera != null)
        {
            Vector3 position = mainCamera.transform.position;
            position.x = area.center.x;
            position.y = area.center.y;
            mainCamera.transform.position = position;
        }

        EventHandler.CallFarmEventEvent(area.name, "已经来到" + area.name, false);
    }

    private void ApplyDefaultView()
    {
        Area farm = Find(AreaKind.Farm);

        if (farm == null)
        {
            return;
        }

        Rect view = GetViewRect(farm);

        Camera mainCamera = Camera.main;
        CameraController controller = mainCamera != null ? mainCamera.GetComponent<CameraController>() : null;

        if (controller != null)
        {
            controller.SetArea(view.center, view.size);
        }
    }

    private Rect GetViewRect(Area area)
    {
        Rect rect = new Rect(
            area.center.x - area.size.x * 0.5f,
            area.center.y - area.size.y * 0.5f,
            area.size.x,
            area.size.y);

        float margin = area.kind == AreaKind.Farm ? 4f : 1f;

        if (area.kind == AreaKind.Farm && WaterWellManager.Instance != null)
        {
            Rect well;

            if (WaterWellManager.Instance.TryGetWellBounds(out well))
            {
                well = new Rect(well.xMin - 2f, well.yMin - 2f, well.width + 4f, well.height + 4f);

                float minX = Mathf.Min(rect.xMin, well.xMin);
                float minY = Mathf.Min(rect.yMin, well.yMin);
                float maxX = Mathf.Max(rect.xMax, well.xMax);
                float maxY = Mathf.Max(rect.yMax, well.yMax);

                rect = new Rect(minX, minY, maxX - minX, maxY - minY);
            }
        }

        return new Rect(rect.xMin - margin, rect.yMin - margin, rect.width + margin * 2f, rect.height + margin * 2f);
    }

    private Area Find(AreaKind kind)
    {
        for (int i = 0; i < areas.Count; i++)
        {
            if (areas[i].kind == kind)
            {
                return areas[i];
            }
        }

        return null;
    }

    private void EnsureBuilt(Area area)
    {
        if (area.built)
        {
            return;
        }

        if (!GenerateAreas)
        {
            area.built = true;
            return;
        }

        area.built = true;

        if (area.kind == AreaKind.Orchard)
        {
            BuildOrchard(area);
        }
        else if (area.kind == AreaKind.Mine)
        {
            BuildMine(area);
        }
        else if (area.kind == AreaKind.Pond)
        {
            BuildPond(area);
        }
    }

    private static int CellKey(int x, int y)
    {
        return x * 1000 + y;
    }

    public bool IsPondWater(int x, int y)
    {
        return pondWaterCells.Contains(CellKey(x, y));
    }

    private void BuildMine(Area area)
    {
        GridMapMangaer map = GridMapMangaer.Instance;

        if (map == null)
        {
            return;
        }

        Tilemap ground = map.GetGroundTilemap();

        if (ground == null)
        {
            return;
        }

        if (grassTile == null)
        {
            grassTile = Resources.Load<TileBase>("plot/Tiles/Grass01");
        }

        oreCells.Clear();

        int halfX = Mathf.RoundToInt(area.size.x * 0.5f);
        int halfY = Mathf.RoundToInt(area.size.y * 0.5f);
        int cx = Mathf.RoundToInt(area.center.x);
        int cy = Mathf.RoundToInt(area.center.y);

        for (int x = cx - halfX; x <= cx + halfX; x++)
        {
            for (int y = cy - halfY; y <= cy + halfY; y++)
            {
                if (rockGroundTile != null)
                {
                    ground.SetTile(new Vector3Int(x, y, 0), rockGroundTile);
                }
                else if (grassTile != null)
                {
                    ground.SetTile(new Vector3Int(x, y, 0), grassTile);
                }

                map.RegisterGeneratedTile(x, y);
            }
        }

        for (int x = cx - halfX + 2; x <= cx + halfX - 2; x += 2)
        {
            for (int y = cy - halfY + 2; y <= cy + halfY - 2; y += 2)
            {
                if ((x + y) % 4 != 0)
                {
                    continue;
                }

                PlaceOreRock(map, x, y);
            }
        }
    }

    private void PlaceOreRock(GridMapMangaer map, int x, int y)
    {
        TileDetails tile = map.RegisterGeneratedTile(x, y);

        tile.isOreNode = true;
        tile.hasObstacle = true;
        tile.obstacleType = ObstacleType.Rock;
        tile.canDig = false;

        oreCells.Add(new Vector2Int(x, y));

        map.RefreshTileVisualFor(tile);
    }

    private void BuildPond(Area area)
    {
        GridMapMangaer map = GridMapMangaer.Instance;

        if (map == null)
        {
            return;
        }

        Tilemap ground = map.GetGroundTilemap();

        if (ground == null)
        {
            return;
        }

        if (grassTile == null)
        {
            grassTile = Resources.Load<TileBase>("plot/Tiles/Grass01");
        }

        if (waterTile == null)
        {
            waterTile = Resources.Load<TileBase>("plot/Rule Tiles/WaterTile");
        }

        pondWaterCells.Clear();

        int halfX = Mathf.RoundToInt(area.size.x * 0.5f);
        int halfY = Mathf.RoundToInt(area.size.y * 0.5f);
        int cx = Mathf.RoundToInt(area.center.x);
        int cy = Mathf.RoundToInt(area.center.y);

        for (int x = cx - halfX; x <= cx + halfX; x++)
        {
            for (int y = cy - halfY; y <= cy + halfY; y++)
            {
                bool water = Mathf.Abs(x - cx) <= halfX - 3 && Mathf.Abs(y - cy) <= halfY - 3;

                if (water)
                {
                    if (waterTile != null)
                    {
                        ground.SetTile(new Vector3Int(x, y, 0), waterTile);
                    }
                }
                else if (grassTile != null)
                {
                    bool shore = Mathf.Abs(x - cx) <= halfX - 1 && Mathf.Abs(y - cy) <= halfY - 1;

                    ground.SetTile(new Vector3Int(x, y, 0), shore ? PickShore() : PickGrass());
                }

                TileDetails tile = map.RegisterGeneratedTile(x, y);

                tile.isPondWater = water;
                tile.canDig = !water;

                if (water)
                {
                    pondWaterCells.Add(CellKey(x, y));
                }
            }
        }

        ground.RefreshAllTiles();
    }

    private void BuildOrchard(Area area)
    {
        GridMapMangaer map = GridMapMangaer.Instance;

        if (map == null)
        {
            return;
        }

        Tilemap ground = map.GetGroundTilemap();

        if (ground == null)
        {
            return;
        }

        if (grassTile == null)
        {
            grassTile = Resources.Load<TileBase>("plot/Tiles/Grass01");
        }

        int halfX = Mathf.RoundToInt(area.size.x * 0.5f);
        int halfY = Mathf.RoundToInt(area.size.y * 0.5f);
        int cx = Mathf.RoundToInt(area.center.x);
        int cy = Mathf.RoundToInt(area.center.y);

        Tilemap decor = map.GetDecorTilemap();

        EnsureDecoTiles();

        for (int x = cx - halfX; x <= cx + halfX; x++)
        {
            for (int y = cy - halfY; y <= cy + halfY; y++)
            {
                ground.SetTile(new Vector3Int(x, y, 0), PickGrass());

                map.RegisterGeneratedTile(x, y);

                bool border = x == cx - halfX || x == cx + halfX || y == cy - halfY || y == cy + halfY;

                if (decor == null)
                {
                    continue;
                }

                Vector3Int cell = new Vector3Int(x, y, 0);

                if (border && fenceTile != null)
                {
                    decor.SetTile(cell, fenceTile);
                }
                else if (PickDecor() != null && UnityEngine.Random.value < 0.18f)
                {
                    decor.SetTile(cell, PickDecor());
                }
            }
        }

        if (decor != null)
        {
            decor.RefreshAllTiles();
        }

        for (int x = cx - halfX + 3; x <= cx + halfX - 3; x += 4)
        {
            for (int y = cy - halfY + 3; y <= cy + halfY - 3; y += 4)
            {
                PlantAppleTree(map, x, y);
            }
        }
    }

    private void SampleFarmTiles()
    {
        if (tileSamples != null)
        {
            return;
        }

        GridMapMangaer map = GridMapMangaer.Instance;

        if (map == null)
        {
            return;
        }

        Tilemap ground = map.GetGroundTilemap();
        Tilemap decor = map.GetDecorTilemap();

        List<TileBase> groundPool = new List<TileBase>();
        List<TileBase> decorPool = new List<TileBase>();

        for (int x = -12; x <= 12; x++)
        {
            for (int y = -6; y <= 6; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);

                if (ground != null)
                {
                    TileBase groundTile2 = ground.GetTile(cell);

                    if (groundTile2 != null && !groundPool.Contains(groundTile2))
                    {
                        groundPool.Add(groundTile2);
                    }
                }

                if (decor != null)
                {
                    TileBase decorTile2 = decor.GetTile(cell);

                    if (decorTile2 != null && !decorPool.Contains(decorTile2))
                    {
                        decorPool.Add(decorTile2);
                    }
                }
            }
        }

        tileSamples = groundPool.ToArray();
        decorSamples = decorPool.ToArray();
    }

    private void EnsureDecoTiles()
    {
        SampleFarmTiles();
        if (grassTile == null)
        {
            grassTile = Resources.Load<TileBase>("plot/Tiles/Grass01");
        }

        if (rockGroundTile == null)
        {
            rockGroundTile = Resources.Load<TileBase>("plot/Tiles/Rock01");
        }

        if (waterTile == null)
        {
            waterTile = Resources.Load<TileBase>("plot/Rule Tiles/WaterTile");
        }

        if (fenceTile == null)
        {
            fenceTile = CreateTextureTile(CreateFenceTexture());
        }

        if (grassVariants == null)
        {
            List<TileBase> pool = new List<TileBase>();

            if (grassTile != null)
            {
                pool.Add(grassTile);
            }

            TileBase[] sampled = tileSamples;

            if (sampled != null)
            {
                for (int i = 0; i < sampled.Length; i++)
                {
                    if (sampled[i] != null && !pool.Contains(sampled[i]))
                    {
                        pool.Add(sampled[i]);
                    }
                }
            }

            grassVariants = pool.ToArray();
        }

        if (decorVariants == null)
        {
            decorVariants = decorSamples != null ? decorSamples : new TileBase[0];
        }

        if (flowerTiles == null)
        {
            flowerTiles = new TileBase[]
            {
                CreateTextureTile(CreateFlowerTexture(0)),
                CreateTextureTile(CreateFlowerTexture(1))
            };
        }
    }

    private TileBase PickGrass()
    {
        if (grassVariants == null || grassVariants.Length == 0)
        {
            return grassTile;
        }

        return grassVariants[Random.Range(0, grassVariants.Length)];
    }

    private TileBase PickShore()
    {
        if (grassVariants == null || grassVariants.Length == 0)
        {
            return grassTile;
        }

        return grassVariants[Random.Range(0, grassVariants.Length)];
    }

    private TileBase PickDecor()
    {
        if (flowerTiles != null && UnityEngine.Random.value < 0.4f)
        {
            return flowerTiles[UnityEngine.Random.Range(0, flowerTiles.Length)];
        }

        if (decorVariants != null && decorVariants.Length > 0)
        {
            return decorVariants[UnityEngine.Random.Range(0, decorVariants.Length)];
        }

        return null;
    }

    private static TileBase CreateTextureTile(Texture2D texture)
    {
        if (texture == null)
        {
            return null;
        }

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            texture.width);

        Tile tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.color = Color.white;

        return tile;
    }

    private static Texture2D CreateFenceTexture()
    {
        const int size = 20;

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color clear = new Color(0f, 0f, 0f, 0f);
        Color wood = new Color(0.66f, 0.47f, 0.28f, 1f);
        Color woodDark = new Color(0.45f, 0.31f, 0.18f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                texture.SetPixel(x, y, clear);
            }
        }

        for (int x = 0; x < size; x++)
        {
            for (int y = 12; y < 15; y++)
            {
                texture.SetPixel(x, y, wood);
            }

            for (int y = 5; y < 8; y++)
            {
                texture.SetPixel(x, y, woodDark);
            }
        }

        for (int y = 1; y < 19; y++)
        {
            for (int x = 8; x < 12; x++)
            {
                texture.SetPixel(x, y, woodDark);
            }

            texture.SetPixel(9, y, wood);
        }

        texture.Apply();

        return texture;
    }

    private static Texture2D CreateFlowerTexture(int variant)
    {
        const int size = 20;

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color clear = new Color(0f, 0f, 0f, 0f);
        Color petal = variant == 0 ? new Color(0.98f, 0.72f, 0.84f, 1f) : new Color(0.8f, 0.76f, 0.98f, 1f);
        Color center = variant == 0 ? new Color(0.99f, 0.9f, 0.52f, 1f) : new Color(0.99f, 0.72f, 0.62f, 1f);
        Color stem = new Color(0.36f, 0.62f, 0.32f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                texture.SetPixel(x, y, clear);
            }
        }

        for (int y = 5; y < 12; y++)
        {
            texture.SetPixel(10, y, stem);
        }

        for (int y = 11; y < 16; y++)
        {
            for (int x = 7; x < 14; x++)
            {
                texture.SetPixel(x, y, petal);
            }
        }

        texture.SetPixel(10, 13, center);
        texture.SetPixel(9, 14, center);
        texture.SetPixel(11, 12, center);

        texture.Apply();

        return texture;
    }

    private void PlantAppleTree(GridMapMangaer map, int x, int y)
    {
        CropManager cropManager = CropManager.Instance;

        if (cropManager == null)
        {
            return;
        }

        TileDetails tile = map.RegisterGeneratedTile(x, y);

        if (tile.seedItemID == AppleTreeSeedID)
        {
            cropManager.RefreshCropVisual(tile);
            return;
        }

        if (tile.seedItemID != -1)
        {
            return;
        }

        CropDetails details = cropManager.GetCropDetails(AppleTreeSeedID);

        tile.seedItemID = AppleTreeSeedID;
        tile.growthDays = details != null ? details.TotalGrowthDays : 3;
        tile.growthFraction = 0f;
        tile.daysSinceDug = 0;
        tile.daysSinceLastHarvest = 0;
        tile.canDig = false;

        cropManager.RefreshCropVisual(tile);
    }
}