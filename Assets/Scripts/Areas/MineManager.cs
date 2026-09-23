using System.Collections.Generic;
using Inventory;
using UnityEngine;

public class MineManager : MonoBehaviour
{
    private const int StoneItemID = 28;
    private const int OreItemID = 37;
    private const int GemItemID = 38;
    private const int VeinCount = 6;
    private const int HitsNeeded = 2;
    private const int RespawnDays = 3;
    private const string SortingLayerName = "Ground Top";
    private const int SortingOrder = 40;

    private class Vein
    {
        public GameObject root;
        public SpriteRenderer renderer;
        public Sprite sprite;
        public int oreItemID;
        public Vector3 position;
        public int hits;
        public int respawnDays;
        public bool mined;
    }

    private static MineManager instance;

    private readonly List<Vein> veins = new List<Vein>();
    private Bounds areaBounds;
    private bool areaFound;
    private bool spawned;
    private float retryTimer;
    private Sprite veinSprite;
    private readonly List<Sprite> veinSprites = new List<Sprite>();
    private Sprite minedSprite;

    public int ActiveVeins
    {
        get { return veins.Count; }
    }

    public static MineManager Instance
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

        GameObject go = new GameObject("MineManager");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<MineManager>();
    }

    private void OnEnable()
    {
        EventHandler.GameDayEvent += OnGameDay;
    }

    private void OnDisable()
    {
        EventHandler.GameDayEvent -= OnGameDay;
    }

    private void OnGameDay(int day, Season season)
    {
        for (int i = 0; i < veins.Count; i++)
        {
            Vein vein = veins[i];

            if (!vein.mined)
            {
                continue;
            }

            vein.respawnDays--;

            if (vein.respawnDays <= 0)
            {
                vein.mined = false;
                vein.hits = 0;

                Vector3 newPosition = RandomPoint();
                vein.position = newPosition;

                if (vein.root != null)
                {
                    vein.root.transform.position = newPosition;
                    vein.root.SetActive(true);
                }

                if (vein.renderer != null)
                {
                    vein.renderer.sprite = vein.sprite != null ? vein.sprite : veinSprite;
                    vein.renderer.color = Color.white;
                }
            }
        }
    }

    private void Update()
    {
        if (spawned)
        {
            return;
        }

        retryTimer += Time.deltaTime;

        if (retryTimer < 0.5f)
        {
            return;
        }

        retryTimer = 0f;

        if (!FindArea())
        {
            return;
        }

        SpawnVeins();
    }

    private bool FindArea()
    {
        if (areaFound)
        {
            return true;
        }

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

            if (name.Contains("mine") || name.Contains("矿") || tag.Contains("mine"))
            {
                areaBounds = collider.bounds;
                areaFound = true;

                Debug.Log("[矿洞] 找到矿区范围：" + collider.gameObject.name + " 中心 " + areaBounds.center + " 大小 " + areaBounds.size);

                return true;
            }
        }

        return false;
    }

    private void SpawnVeins()
    {
        spawned = true;

        EnsureSprites();

        GameObject parent = new GameObject("OreVeins");
        DontDestroyOnLoad(parent);

        for (int i = 0; i < VeinCount; i++)
        {
            Vector3 position = RandomPoint();

            Vein vein = new Vein();
            vein.position = position;
            vein.hits = 0;
            vein.mined = false;
            vein.respawnDays = 0;
            vein.oreItemID = RollOreType();

            GameObject obj = new GameObject("OreVein");
            obj.transform.SetParent(parent.transform, false);
            obj.transform.position = position;

            SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
            vein.sprite = veinSprites.Count > 0 ? veinSprites[Random.Range(0, veinSprites.Count)] : veinSprite;
            renderer.sprite = vein.sprite;
            renderer.sortingLayerName = SortingLayerName;
            renderer.sortingOrder = SortingOrder;

            BoxCollider2D box = obj.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1.2f, 1.2f);

            vein.root = obj;
            vein.renderer = renderer;

            veins.Add(vein);
        }

        Debug.Log("[矿洞] 生成矿脉 " + veins.Count + " 处");
    }

    private Vector3 RandomPoint()
    {
        for (int attempt = 0; attempt < 30; attempt++)
        {
            float x = Random.Range(areaBounds.min.x + 1f, areaBounds.max.x - 1f);
            float y = Random.Range(areaBounds.min.y + 1f, areaBounds.max.y - 1f);
            Vector3 candidate = new Vector3(x, y, 0f);
            bool tooClose = false;

            for (int i = 0; i < veins.Count; i++)
            {
                if (Vector3.Distance(veins[i].position, candidate) < 2f)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
            {
                return candidate;
            }
        }

        return new Vector3(areaBounds.center.x, areaBounds.center.y, 0f);
    }

    public bool HasVeinAt(Vector3 worldPosition)
    {
        return FindVein(worldPosition) != null;
    }

    private Vein FindVein(Vector3 worldPosition)
    {
        Vein best = null;
        float bestDistance = 1.6f;

        for (int i = 0; i < veins.Count; i++)
        {
            Vein vein = veins[i];

            if (vein == null || vein.mined || vein.root == null)
            {
                continue;
            }

            float distance = Vector3.Distance(vein.position, worldPosition);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = vein;
            }
        }

        return best;
    }

    public bool TryMine(Vector3 worldPosition)
    {
        Vein vein = FindVein(worldPosition);

        if (vein == null)
        {
            return false;
        }

        vein.hits++;

        if (vein.hits < HitsNeeded)
        {
            EventHandler.CallFarmEventEvent("挖矿", "敲了一下，矿脉还需要 " + (HitsNeeded - vein.hits) + " 下", false);
            return true;
        }

        vein.mined = true;
        vein.respawnDays = RespawnDays;

        if (vein.renderer != null && vein.sprite != null)
        {
            vein.renderer.sprite = vein.sprite;
            vein.renderer.color = new Color(0.45f, 0.45f, 0.5f, 0.7f);
        }

        int pickaxeLevel = ToolDurability.GetLevel(ItemType.pickaxeTool);
        int oreAmount = Random.Range(1, 3) + (pickaxeLevel - 1);
        Vector3 dropPosition = vein.position + new Vector3(0f, -0.6f, 0f);

        for (int i = 0; i < oreAmount; i++)
        {
            SpawnDrop(vein.oreItemID, dropPosition + new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(-0.4f, 0.4f), 0f));
        }

        if (vein.oreItemID == OreItemID && Random.value < 0.08f)
        {
            SpawnDrop(GemItemID, dropPosition + new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(-0.3f, 0.3f), 0f));
        }

        Item mined = InventoryManager.Instance != null ? InventoryManager.Instance.GetItem(vein.oreItemID) : null;
        string minedName = mined != null ? mined.itemName : "矿";

        EventHandler.CallFarmEventEvent("挖矿", "采到 " + oreAmount + " 份" + minedName + "，矿脉 " + RespawnDays + " 天后恢复", false);

        return true;
    }

    private static int RollOreType()
    {
        float roll = Random.value;

        if (roll < 0.35f)
        {
            return StoneItemID;
        }

        if (roll < 0.92f)
        {
            return OreItemID;
        }

        return GemItemID;
    }

    private static void SpawnDrop(int itemID, Vector3 position)
    {
        Item item = InventoryManager.Instance != null ? InventoryManager.Instance.GetItem(itemID) : null;
        Sprite sprite = null;

        if (item != null)
        {
            sprite = item.itemOnWorldSprite != null ? item.itemOnWorldSprite : item.itemIcon;
        }

        if (sprite == null)
        {
            if (CropManager.Instance != null)
            {
                CropManager.Instance.SpawnFruit(itemID, position);
            }

            return;
        }

        GameObject drop = new GameObject("Drop_" + itemID);
        drop.transform.position = position;
        drop.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

        SpriteRenderer renderer = drop.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = SortingLayerName;
        renderer.sortingOrder = 500;

        BoxCollider2D box = drop.AddComponent<BoxCollider2D>();
        box.size = new Vector2(1f, 1f);

        FruitPickup pickup = drop.AddComponent<FruitPickup>();
        pickup.useColliderDetection = true;
        pickup.Initialize(itemID);
    }

    private void EnsureSprites()
    {
        if (veinSprite != null)
        {
            return;
        }

        Sprite sheetSprite = Resources.Load<Sprite>("Art/Trees/ore_sheet");
        Texture2D sheet = Resources.Load<Texture2D>("Art/Trees/ore_sheet");

        if (sheet != null)
        {
            sheet.filterMode = FilterMode.Point;
            sheet.wrapMode = TextureWrapMode.Clamp;

            const int cell = 16;

            for (int y = sheet.height - cell; y >= 0; y -= cell)
            {
                for (int x = 0; x + cell <= sheet.width; x += cell)
                {
                    bool hasPixel = false;

                    for (int py = y; py < y + cell && !hasPixel; py++)
                    {
                        for (int px = x; px < x + cell; px++)
                        {
                            if (sheet.GetPixel(px, py).a > 0.05f)
                            {
                                hasPixel = true;
                                break;
                            }
                        }
                    }

                    if (!hasPixel)
                    {
                        continue;
                    }

                    veinSprites.Add(Sprite.Create(
                        sheet,
                        new Rect(x, y, cell, cell),
                        new Vector2(0.5f, 0.5f),
                        cell));
                }
            }
        }

        if (sheetSprite != null && veinSprites.Count == 0)
        {
            veinSprites.Add(sheetSprite);
        }

        if (veinSprites.Count > 0)
        {
            veinSprite = veinSprites[0];
            minedSprite = veinSprite;

            Debug.Log("[矿洞] 从图集切出矿脉图 " + veinSprites.Count + " 种");

            return;
        }

        veinSprite = Resources.Load<Sprite>("Art/Trees/ore_vein");

        if (veinSprite != null)
        {
            minedSprite = Resources.Load<Sprite>("Art/Trees/ore_vein_mined");

            if (minedSprite == null)
            {
                minedSprite = veinSprite;
            }

            return;
        }

        veinSprite = CreateVeinSprite(false);
        minedSprite = CreateVeinSprite(true);
    }

    private static Sprite CreateVeinSprite(bool mined)
    {
        const int size = 30;

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color clear = new Color(0f, 0f, 0f, 0f);
        Color rockDark = mined ? new Color(0.28f, 0.3f, 0.34f) : new Color(0.38f, 0.4f, 0.45f);
        Color rockLight = mined ? new Color(0.42f, 0.44f, 0.48f) : new Color(0.62f, 0.65f, 0.7f);
        Color ore = new Color(0.91f, 0.66f, 0.25f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                texture.SetPixel(x, y, clear);
            }
        }

        Color outline = mined ? new Color(0.18f, 0.19f, 0.22f) : new Color(0.22f, 0.24f, 0.27f);

        for (int y = 3; y < 27; y++)
        {
            for (int x = 3; x < 27; x++)
            {
                float dx = (x - 15f) / 11f;
                float dy = (y - 15f) / 10f;
                float distance = dx * dx + dy * dy;

                if (distance <= 1f)
                {
                    texture.SetPixel(x, y, distance > 0.72f ? outline : (y > 15 ? rockLight : rockDark));
                }
            }
        }

        texture.SetPixel(13, 19, outline);
        texture.SetPixel(14, 18, outline);
        texture.SetPixel(15, 17, outline);

        if (!mined)
        {
            for (int i = 0; i < 4; i++)
            {
                int ox = Random.Range(8, 22);
                int oy = Random.Range(8, 22);

                texture.SetPixel(ox, oy, ore);
                texture.SetPixel(ox + 1, oy, ore);
                texture.SetPixel(ox, oy + 1, ore);
            }
        }

        texture.Apply();

        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }
}