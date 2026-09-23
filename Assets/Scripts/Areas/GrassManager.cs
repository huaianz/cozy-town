using System.Collections.Generic;
using Inventory;
using UnityEngine;

public class GrassManager : MonoBehaviour
{
    private const int GrassItemID = 57;
    private const int FiberItemID = 29;

    private static GrassManager instance;

    private readonly List<GameObject> grasses = new List<GameObject>();
    private float scanTimer;

    public static GrassManager Instance
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

        GameObject go = new GameObject("GrassManager");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<GrassManager>();
    }

    private void Update()
    {
        scanTimer += Time.deltaTime;

        if (scanTimer < 2f)
        {
            return;
        }

        scanTimer = 0f;
        ScanGrass();
    }

    private static bool IsGrassName(GameObject obj)
    {
        Transform current = obj.transform;

        while (current != null)
        {
            string lower = current.gameObject.name.ToLower();

            if (lower.Contains("grass") || lower.Contains("Ò°²Ý") || lower.Contains("weed"))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private void ScanGrass()
    {
        grasses.Clear();

        SpriteRenderer[] all = FindObjectsOfType<SpriteRenderer>();

        for (int i = 0; i < all.Length; i++)
        {
            SpriteRenderer renderer = all[i];

            if (renderer == null || !renderer.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (IsGrassName(renderer.gameObject))
            {
                grasses.Add(renderer.gameObject);
            }
        }
    }

    public bool TryCut(Vector3 worldPosition)
    {
        GameObject grass = FindGrassAt(worldPosition);

        if (grass == null)
        {
            return false;
        }

        grass.SetActive(false);

        int amount = Random.Range(1, 3);
        int itemID = Random.value < 0.5f ? GrassItemID : FiberItemID;

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(itemID, amount);
        }

        Item item = InventoryManager.Instance != null ? InventoryManager.Instance.GetItem(itemID) : null;
        string itemName = item != null ? item.itemName : "ËØ²Ä";

        EventHandler.CallFarmEventEvent("¸î²Ý", "¸îµ½ " + amount + " ·Ý" + itemName, false);

        if (Random.value < 0.25f && InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(55, 1);
            EventHandler.CallFarmEventEvent("¸î²Ý", "²Ý´ÔÀï·­³ö ÓãÊ³x1", false);
        }

        return true;
    }

    public bool HasGrassAt(Vector3 worldPosition)
    {
        return FindGrassAt(worldPosition) != null;
    }

    private GameObject FindGrassAt(Vector3 worldPosition)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPosition);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] != null && IsGrassName(hits[i].gameObject))
            {
                return RootGrass(hits[i].gameObject);
            }
        }

        if (grasses.Count == 0)
        {
            ScanGrass();
        }

        GameObject best = null;
        float bestDistance = 1.2f;

        for (int i = 0; i < grasses.Count; i++)
        {
            GameObject candidate = grasses[i];

            if (candidate == null || !candidate.activeInHierarchy)
            {
                continue;
            }

            float distance = Vector3.Distance(candidate.transform.position, worldPosition);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = candidate;
            }
        }

        return best;
    }

    private static GameObject RootGrass(GameObject start)
    {
        Transform current = start.transform;

        while (current.parent != null && current.parent.gameObject.name.ToLower().Contains("grass"))
        {
            current = current.parent;
        }

        return current.gameObject;
    }
}