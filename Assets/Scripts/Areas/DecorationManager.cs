using System.Collections.Generic;
using Inventory;
using UnityEngine;

public class DecorationManager : MonoBehaviour
{
    private const string SaveKey = "CozyTown_Decorations";
    private const string SortingLayerName = "Ground Top";
    private const int SortingOrder = 25;

    private static DecorationManager instance;

    private readonly Dictionary<int, GameObject> placed = new Dictionary<int, GameObject>();
    private readonly Dictionary<int, int> placedItem = new Dictionary<int, int>();
    private bool loaded;

    public static DecorationManager Instance
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

        GameObject go = new GameObject("DecorationManager");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<DecorationManager>();
    }

    private void Update()
    {
        if (loaded)
        {
            return;
        }

        loaded = true;
        LoadSaved();
    }

    public static bool IsDecorationItem(int itemID)
    {
        return itemID >= 61 && itemID <= 64;
    }

    private static string SpriteName(int itemID)
    {
        if (itemID == 61) return "Art/Decor/fence";
        if (itemID == 62) return "Art/Decor/path";
        if (itemID == 63) return "Art/Decor/lantern";
        if (itemID == 64) return "Art/Decor/flowerpot";

        return string.Empty;
    }

    private static int Key(int x, int y)
    {
        return x * 1000 + y;
    }

    public bool TryPlace(int itemID, Vector3 worldPosition)
    {
        if (!IsDecorationItem(itemID))
        {
            return false;
        }

        int x = Mathf.FloorToInt(worldPosition.x);
        int y = Mathf.FloorToInt(worldPosition.y);
        int key = Key(x, y);

        if (placed.ContainsKey(key))
        {
            EventHandler.CallFarmEventEvent("装饰", "这一格已经摆过东西了", true);
            return true;
        }

        Sprite sprite = Resources.Load<Sprite>(SpriteName(itemID));

        if (sprite == null)
        {
            EventHandler.CallFarmEventEvent("装饰", "找不到装饰贴图", true);
            return true;
        }

        if (InventoryManager.Instance == null || InventoryManager.Instance.GetItemAmountInBag(itemID) <= 0)
        {
            EventHandler.CallFarmEventEvent("装饰", "背包里没有这个装饰", true);
            return true;
        }

        InventoryManager.Instance.RemoveItem(itemID, 1);

        Create(x, y, itemID, sprite);
        Save();

        EventHandler.CallFarmEventEvent("装饰", "摆好了一个装饰", false);

        return true;
    }

    private void Create(int x, int y, int itemID, Sprite sprite)
    {
        GameObject obj = new GameObject("Decor_" + itemID + "_" + x + "_" + y);
        obj.transform.position = new Vector3(x + 0.5f, y + 0.5f, 0f);
        obj.transform.SetParent(transform, true);

        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = SortingLayerName;
        renderer.sortingOrder = SortingOrder;

        BoxCollider2D box = obj.AddComponent<BoxCollider2D>();
        box.size = new Vector2(1f, 1f);

        int key = Key(x, y);
        placed[key] = obj;
        placedItem[key] = itemID;
    }

    public bool TryRemove(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt(worldPosition.x);
        int y = Mathf.FloorToInt(worldPosition.y);
        int key = Key(x, y);

        GameObject obj;

        if (!placed.TryGetValue(key, out obj))
        {
            return false;
        }

        int itemID;
        placedItem.TryGetValue(key, out itemID);

        if (obj != null)
        {
            Destroy(obj);
        }

        placed.Remove(key);
        placedItem.Remove(key);
        Save();

        Item item = InventoryManager.Instance != null ? InventoryManager.Instance.GetItem(itemID) : null;
        int refund = item != null ? Mathf.RoundToInt(item.itemPrice * item.sellPercentage) : 0;

        if (refund > 0 && ShopManager.Instance != null)
        {
            ShopManager.Instance.PlayerMoney += refund;
            EventHandler.CallUpdateMoneyEvent(ShopManager.Instance.PlayerMoney);
        }

        EventHandler.CallFarmEventEvent("装饰", "拆掉了一个装饰，返还 " + refund + " 金币", false);

        return true;
    }

    private void Save()
    {
        string data = string.Empty;

        foreach (KeyValuePair<int, int> pair in placedItem)
        {
            int key = pair.Key;
            int x = key / 1000;
            int y = key % 1000;

            if (y > 500)
            {
                y -= 1000;
            }

            data += pair.Value + ":" + x + ":" + y + ";";
        }

        PlayerPrefs.SetString(SaveKey, data);
        PlayerPrefs.Save();
    }

    private void LoadSaved()
    {
        string data = PlayerPrefs.GetString(SaveKey, string.Empty);

        if (string.IsNullOrEmpty(data))
        {
            return;
        }

        string[] entries = data.Split(';');

        for (int i = 0; i < entries.Length; i++)
        {
            string entry = entries[i];

            if (string.IsNullOrEmpty(entry))
            {
                continue;
            }

            string[] parts = entry.Split(':');

            if (parts.Length != 3)
            {
                continue;
            }

            int itemID;
            int x;
            int y;

            if (!int.TryParse(parts[0], out itemID) || !int.TryParse(parts[1], out x) || !int.TryParse(parts[2], out y))
            {
                continue;
            }

            Sprite sprite = Resources.Load<Sprite>(SpriteName(itemID));

            if (sprite == null)
            {
                continue;
            }

            Create(x, y, itemID, sprite);
        }

        Debug.Log("[装饰] 载入已摆放的装饰 " + placed.Count + " 个");
    }
}