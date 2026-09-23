using System.Collections.Generic;
using Inventory;
using UnityEngine;

public class PondManager : MonoBehaviour
{
    private const int Capacity = 10;
    private const int MatureGrowth = 6;
    private const int FeedItemID = 55;
    private const int GrassItemID = 57;

    private static PondManager instance;

    private class Fish
    {
        public int species;
        public int growth;
    }

    private readonly List<Fish> fishes = new List<Fish>();
    private int feed;

    public static PondManager Instance
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

        GameObject go = new GameObject("PondManager");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<PondManager>();
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
        if (fishes.Count == 0)
        {
            return;
        }

        int boost = feed > 0 ? 1 : 0;

        if (feed > 0)
        {
            feed--;
        }

        for (int i = 0; i < fishes.Count; i++)
        {
            fishes[i].growth += 2 + boost;

            if (fishes[i].growth > MatureGrowth)
            {
                fishes[i].growth = MatureGrowth;
            }
        }

        EventHandler.CallFarmEventEvent("鱼塘", "塘里 " + fishes.Count + " 尾鱼，其中 " + CountMature() + " 尾已成熟", false);
    }

    public bool ThrowItem(int itemID, Vector3 worldPosition)
    {
        int species = SpeciesFromFry(itemID);

        if (itemID == 51 || itemID == 52 || itemID == 53 || itemID == 54 || itemID == 55 || itemID == 57)
        {
            Debug.Log("[鱼塘] 投掷请求 物品=" + itemID + " 鱼种=" + species + " 位置=" + worldPosition + " 是水面=" + IsPond(worldPosition) + " 塘内鱼=" + fishes.Count);
        }

        if (species > 0)
        {
            if (!IsPond(worldPosition))
            {
                EventHandler.CallFarmEventEvent("鱼塘", "要对着水面投鱼苗", true);
                return true;
            }

            if (fishes.Count >= Capacity)
            {
                EventHandler.CallFarmEventEvent("鱼塘", "塘满了（上限 " + Capacity + " 尾），先钓几条上来", true);
                return true;
            }

            Fish fish = new Fish();
            fish.species = species;
            fish.growth = 0;
            fishes.Add(fish);

            Consume(itemID, 1);

            EventHandler.CallFarmEventEvent("鱼塘", "投下 1 尾" + SpeciesName(species) + "苗，塘里共 " + fishes.Count + " 尾", false);

            return true;
        }

        if (itemID == FeedItemID)
        {
            if (!IsPond(worldPosition))
            {
                EventHandler.CallFarmEventEvent("鱼塘", "要对着水面撒鱼食", true);
                return true;
            }

            feed += 3;
            Consume(itemID, 1);

            EventHandler.CallFarmEventEvent("鱼塘", "撒下 1 份鱼食，鱼儿长得更快了", false);

            return true;
        }

        if (itemID == GrassItemID)
        {
            if (!IsPond(worldPosition))
            {
                EventHandler.CallFarmEventEvent("鱼塘", "要对着水面丢野草", true);
                return true;
            }

            int grassCarp = 0;

            for (int i = 0; i < fishes.Count; i++)
            {
                if (fishes[i].species == 1)
                {
                    grassCarp++;
                }
            }

            if (grassCarp == 0)
            {
                EventHandler.CallFarmEventEvent("鱼塘", "塘里没有草鱼，野草没人吃", true);
                return true;
            }

            feed += 2;
            Consume(itemID, 1);

            EventHandler.CallFarmEventEvent("鱼塘", "把野草丢进塘里，草鱼吃得很欢", false);

            return true;
        }

        return false;
    }

    public int FishCount
    {
        get { return fishes.Count; }
    }

    public bool HasMatureFish()
    {
        return CountMature() > 0;
    }

    public int CountMature()
    {
        int count = 0;

        for (int i = 0; i < fishes.Count; i++)
        {
            if (fishes[i].growth >= MatureGrowth)
            {
                count++;
            }
        }

        return count;
    }

    public int CatchFish()
    {
        List<int> mature = new List<int>();

        for (int i = 0; i < fishes.Count; i++)
        {
            if (fishes[i].growth >= MatureGrowth)
            {
                mature.Add(i);
            }
        }

        if (mature.Count == 0)
        {
            return -1;
        }

        int index = mature[Random.Range(0, mature.Count)];
        int species = fishes[index].species;

        fishes.RemoveAt(index);

        return ProductFromSpecies(species);
    }

    private static int SpeciesFromFry(int itemID)
    {
        if (itemID == 51) return 1;
        if (itemID == 52) return 2;
        if (itemID == 53) return 3;
        if (itemID == 54) return 4;

        return 0;
    }

    private static int ProductFromSpecies(int species)
    {
        if (species == 1) return 56;
        if (species == 2) return 40;
        if (species == 3) return 41;
        if (species == 4) return 42;

        return 40;
    }

    private static string SpeciesName(int species)
    {
        if (species == 1) return "草鱼";
        if (species == 2) return "鲤鱼";
        if (species == 3) return "鲈鱼";
        if (species == 4) return "金鱼";

        return "鱼";
    }

    private static void Consume(int itemID, int amount)
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.RemoveItem(itemID, amount);
        }
    }

    private static bool IsPond(Vector3 worldPosition)
    {
        if (HitsPond(Physics2D.OverlapPointAll(worldPosition, Physics2D.AllLayers)))
        {
            return true;
        }

        return HitsPond(Physics2D.OverlapCircleAll(worldPosition, 0.5f, Physics2D.AllLayers));
    }

    private static bool HitsPond(Collider2D[] hits)
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
                string name = check.gameObject.name.ToLower();

                if (name.Contains("pond") || name.Contains("water") || name.Contains("鱼") || name.Contains("塘") || name.Contains("池"))
                {
                    return true;
                }

                string tag = check.gameObject.tag;

                if (!string.IsNullOrEmpty(tag) && tag.ToLower().Contains("water"))
                {
                    return true;
                }

                check = check.parent;
            }
        }

        return false;
    }
}