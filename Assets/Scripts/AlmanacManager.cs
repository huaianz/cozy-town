using System.Collections.Generic;
using Inventory;
using UnityEngine;

public static class AlmanacManager
{
    private const string StorageKey = "CozyTown_Almanac";

    private static readonly HashSet<int> discovered = new HashSet<int>();
    private static bool loaded;

    private static void EnsureLoaded()
    {
        if (loaded)
        {
            return;
        }

        loaded = true;

        string raw = PlayerPrefs.GetString(StorageKey, string.Empty);

        if (string.IsNullOrEmpty(raw))
        {
            return;
        }

        string[] parts = raw.Split(',');

        for (int i = 0; i < parts.Length; i++)
        {
            int id;

            if (int.TryParse(parts[i], out id))
            {
                discovered.Add(id);
            }
        }
    }

    public static bool IsDiscovered(int itemID)
    {
        EnsureLoaded();

        return discovered.Contains(itemID);
    }

    public static void Discover(int itemID)
    {
        EnsureLoaded();

        if (itemID <= 0 || discovered.Contains(itemID))
        {
            return;
        }

        discovered.Add(itemID);
        Save();
    }

    public static void SyncBag()
    {
        InventoryManager manager = InventoryManager.Instance;

        if (manager == null || manager.playerBag == null || manager.playerBag.BagList == null)
        {
            return;
        }

        EnsureLoaded();

        bool changed = false;

        for (int i = 0; i < manager.playerBag.BagList.Count; i++)
        {
            InventoryItem slot = manager.playerBag.BagList[i];

            if (slot.itemID <= 0)
            {
                continue;
            }

            if (discovered.Add(slot.itemID))
            {
                changed = true;
            }
        }

        if (changed)
        {
            Save();
        }
    }

    public static int DiscoveredCount
    {
        get
        {
            EnsureLoaded();
            return discovered.Count;
        }
    }

    private static void Save()
    {
        List<string> parts = new List<string>();

        foreach (int id in discovered)
        {
            parts.Add(id.ToString());
        }

        PlayerPrefs.SetString(StorageKey, string.Join(",", parts.ToArray()));
        PlayerPrefs.Save();
    }
}