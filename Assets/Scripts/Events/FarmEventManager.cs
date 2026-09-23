using System.Collections.Generic;
using Inventory;
using UnityEngine;

public class FarmEventManager : MonoBehaviour
{
    private const float BaseChance = 0.45f;
    private const float WeatherWeightBonus = 4f;
    private const int HerbItemID = 48;
    private const int HoneyItemID = 30;
    private const int CompostItemID = 31;
    private const int FurItemID = 32;
    private const string LocustTitle = "虫灾";

    private class FarmEvent
    {
        public string title;
        public string description;
        public Season season;
        public WeatherType[] weathers;
        public bool strictWeather;
        public bool disaster;
        public float weight;
        public System.Action apply;
        public bool interactive;
        public string acceptText;
        public string declineText;
        public UnityEngine.Events.UnityAction accept;
        public UnityEngine.Events.UnityAction decline;
        public bool blockedByScarecrow;
    }

    private static FarmEventManager instance;

    private readonly List<FarmEvent> table = new List<FarmEvent>();
    private bool built;
    private int lastDay = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (instance != null)
        {
            return;
        }

        GameObject go = new GameObject("FarmEventManager");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<FarmEventManager>();
    }

    private void OnEnable()
    {
        EventHandler.WeatherChangedEvent += OnWeatherChanged;
    }

    private void OnDisable()
    {
        EventHandler.WeatherChangedEvent -= OnWeatherChanged;
    }

    private void OnWeatherChanged(WeatherType weather)
    {
        if (FindObjectOfType<GridMapMangaer>() == null)
        {
            return;
        }

        TimeManager timeManager = FindObjectOfType<TimeManager>();

        if (timeManager == null)
        {
            return;
        }

        timeManager.GetTimeData(out _, out _, out _, out int day, out _, out _, out Season season);

        if (lastDay < 0 || day == lastDay)
        {
            lastDay = day;
            return;
        }

        lastDay = day;

        FarmEventEffects.ResetDaily();

        if (EventChoicePanel.IsOpen)
        {
            return;
        }

        if (Random.value > BaseChance)
        {
            return;
        }

        BuildTable();

        FarmEvent picked = Pick(season, weather);

        if (picked == null)
        {
            return;
        }

        if (picked.interactive)
        {
            if (picked.blockedByScarecrow && HasScarecrow())
            {
                EventHandler.CallFarmEventEvent(picked.title, "田里的稻草人把虫群挡在了外面", false);
                return;
            }

            EventChoicePanel.Show(picked.title, picked.description, picked.acceptText, picked.accept, picked.declineText, picked.decline);
            return;
        }

        if (picked.apply != null)
        {
            picked.apply();
        }

        EventHandler.CallFarmEventEvent(picked.title, picked.description, picked.disaster);

    }

    private void BuildTable()
    {
        if (built)
        {
            return;
        }

        built = true;

        Add("春雨滋长", "雨水浸润土壤，露天作物今天长得更快", Season.春天,
            new WeatherType[] { WeatherType.LightRain, WeatherType.Mist, WeatherType.Rainy },
            true, false, 1.4f, ApplySpringGrowth);

        Add("野花丛生", "田边冒出成片的野花，能采到草药", Season.春天,
            new WeatherType[] { WeatherType.Sunny, WeatherType.Cloudy, WeatherType.Overcast },
            true, false, 1.2f, ApplyWildFlowers);

        Add("蜂群来访", "蜜蜂在花间忙碌，今天收获的果实更多", Season.春天,
            new WeatherType[] { WeatherType.Sunny },
            true, false, 1.1f, ApplyBees);

        Add("雷击", "一道闪电劈在田里，一块成熟作物被击毁", Season.春天,
            new WeatherType[] { WeatherType.Thunderstorm },
            true, true, 1.6f, ApplyLightning);

        Add("热浪", "连日晴热，没浇水的作物被晒蔫了", Season.夏天,
            new WeatherType[] { WeatherType.Sunny },
            true, true, 1.3f, ApplyHeatWave);

        Add("暴雨积水", "田里积了水，作物生长被打断", Season.夏天,
            new WeatherType[] { WeatherType.HeavyRain, WeatherType.Rainy },
            true, true, 1.3f, ApplyFlood);

        Add("雷击", "暴雨夹着闪电，成熟作物被劈毁", Season.夏天,
            new WeatherType[] { WeatherType.Thunderstorm },
            true, true, 1.8f, ApplyLightning);

        Add("蝴蝶迁徙", "蝶群经过田野，作物授粉更充分", Season.夏天,
            new WeatherType[] { WeatherType.Cloudy },
            true, false, 1.2f, ApplyButterflies);

        Add("秋霜初临", "夜里落了霜，未收割的作物受了冻伤", Season.秋天,
            new WeatherType[] { WeatherType.Overcast, WeatherType.DenseFog },
            true, true, 1.3f, ApplyAutumnFrost);

        Add("落叶堆", "风吹来一堆落叶，里面沤着上好的堆肥", Season.秋天,
            null, false, false, 1.2f, ApplyLeafPile);

        Add("候鸟过境", "候鸟飞过，地上落下几粒种子", Season.秋天,
            new WeatherType[] { WeatherType.Sunny },
            true, false, 1.2f, ApplyBirds);

        Add("绵绵秋雨", "温和的秋雨补足了水分，作物精神起来", Season.秋天,
            new WeatherType[] { WeatherType.LightRain, WeatherType.Rainy },
            true, false, 1.4f, ApplyAutumnRain);

        Add("厚雪封田", "积雪覆盖农田，露天作物停止生长", Season.冬天,
            new WeatherType[] { WeatherType.LightSnow, WeatherType.HeavySnow },
            true, false, 1.6f, ApplySnowCover);

        AddChoice("雪兔出没", "雪兔在雪地上探头探脑，抓住它要费些力气", Season.冬天,
            new WeatherType[] { WeatherType.LightSnow, WeatherType.Sunny },
            true, 1.6f, "花 8 点体力去追", AcceptSnowRabbit, "让它跑掉", null);

        Add("冬日暖阳", "难得的暖阳，作物缓过一口气", Season.冬天,
            new WeatherType[] { WeatherType.Sunny },
            true, false, 0.9f, ApplyWinterSun);

        Add("暴风雪", "风雪肆虐，成熟的作物冻死在田里", Season.冬天,
            new WeatherType[] { WeatherType.HeavySnow },
            true, true, 1.0f, ApplyBlizzard);

        AddChoice("流星", "夜里划过流星，田边露出一片矿脉", Season.春天, null, false, 0.35f, "花 5 点体力去挖", AcceptMeteor, "太累了，算了", null);
        AddChoice("流星", "夜里划过流星，田边露出一片矿脉", Season.夏天, null, false, 0.35f, "花 5 点体力去挖", AcceptMeteor, "太累了，算了", null);
        AddChoice("流星", "夜里划过流星，田边露出一片矿脉", Season.秋天, null, false, 0.35f, "花 5 点体力去挖", AcceptMeteor, "太累了，算了", null);
        AddChoice("流星", "夜里划过流星，田边露出一片矿脉", Season.冬天, null, false, 0.35f, "花 5 点体力去挖", AcceptMeteor, "太累了，算了", null);

        AddChoice("虫灾", "田里钻出虫群，扎稻草人可以把它们挡在田外", Season.春天, null, false, 0.4f, "放稻草人（消耗纤维 ×3）", AcceptScarecrow, "不管它", DeclineLocust);
        AddChoice("虫灾", "田里钻出虫群，扎稻草人可以把它们挡在田外", Season.夏天, null, false, 0.5f, "放稻草人（消耗纤维 ×3）", AcceptScarecrow, "不管它", DeclineLocust);
        AddChoice("虫灾", "田里钻出虫群，扎稻草人可以把它们挡在田外", Season.秋天, null, false, 0.45f, "放稻草人（消耗纤维 ×3）", AcceptScarecrow, "不管它", DeclineLocust);
        AddChoice("商人到访", "一位流浪商人路过，想卖给你几颗稀有种子", Season.春天,
            new WeatherType[] { WeatherType.Sunny, WeatherType.Cloudy, WeatherType.Overcast },
            true, 0.7f, "花 200 金币买下", AcceptMerchant, "不感兴趣", null);
        AddChoice("商人到访", "一位流浪商人路过，想卖给你几颗稀有种子", Season.夏天,
            new WeatherType[] { WeatherType.Sunny, WeatherType.Cloudy, WeatherType.Overcast },
            true, 0.7f, "花 200 金币买下", AcceptMerchant, "不感兴趣", null);
        AddChoice("商人到访", "一位流浪商人路过，想卖给你几颗稀有种子", Season.秋天,
            new WeatherType[] { WeatherType.Sunny, WeatherType.Cloudy, WeatherType.Overcast },
            true, 0.7f, "花 200 金币买下", AcceptMerchant, "不感兴趣", null);
        AddChoice("商人到访", "一位流浪商人路过，想卖给你几颗稀有种子", Season.冬天,
            new WeatherType[] { WeatherType.Sunny, WeatherType.Cloudy, WeatherType.Overcast },
            true, 0.7f, "花 200 金币买下", AcceptMerchant, "不感兴趣", null);
    }

    private void Add(string title, string description, Season season, WeatherType[] weathers, bool strictWeather, bool disaster, float weight, System.Action apply)
    {
        FarmEvent item = new FarmEvent();
        item.title = title;
        item.description = description;
        item.season = season;
        item.weathers = weathers;
        item.strictWeather = strictWeather;
        item.disaster = disaster;
        item.weight = weight;
        item.apply = apply;

        table.Add(item);
    }

    private void AddChoice(string title, string description, Season season, WeatherType[] weathers, bool strictWeather, float weight, string acceptText, UnityEngine.Events.UnityAction accept, string declineText, UnityEngine.Events.UnityAction decline)
    {
        FarmEvent item = new FarmEvent();
        item.title = title;
        item.description = description;
        item.season = season;
        item.weathers = weathers;
        item.strictWeather = strictWeather;
        item.disaster = false;
        item.weight = weight;
        item.interactive = true;
        item.acceptText = acceptText;
        item.declineText = declineText;
        item.accept = accept;
        item.decline = decline;
        item.blockedByScarecrow = title == LocustTitle;

        table.Add(item);
    }

    private FarmEvent Pick(Season season, WeatherType weather)
    {
        List<FarmEvent> candidates = new List<FarmEvent>();
        List<float> weights = new List<float>();
        float total = 0f;

        for (int i = 0; i < table.Count; i++)
        {
            FarmEvent item = table[i];

            if (item.season != season)
            {
                continue;
            }

            bool matches = Matches(item, weather);

            if (item.strictWeather && !matches)
            {
                continue;
            }

            float weight = item.weight * (matches ? WeatherWeightBonus : 1f);
            total += weight;
            candidates.Add(item);
            weights.Add(weight);
        }

        if (candidates.Count == 0 || total <= 0f)
        {
            return null;
        }

        float roll = Random.Range(0f, total);

        for (int i = 0; i < candidates.Count; i++)
        {
            roll -= weights[i];

            if (roll <= 0f)
            {
                return candidates[i];
            }
        }

        return candidates[candidates.Count - 1];
    }

    private static bool Matches(FarmEvent item, WeatherType weather)
    {
        if (item.weathers == null || item.weathers.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < item.weathers.Length; i++)
        {
            if (item.weathers[i] == weather)
            {
                return true;
            }
        }

        return false;
    }

    private void ApplySpringGrowth()
    {
        FarmEventEffects.growthBonus += 0.15f;
    }

    private void ApplyWildFlowers()
    {
        SpawnPickups(HerbItemID, 3);
    }

    private void ApplyBees()
    {
        FarmEventEffects.harvestBonus += 1;
        GiveItem(HoneyItemID, 1, 1);
    }

    private void ApplyLightning()
    {
        DestroyRandomMatureCrop();
    }

    private void ApplyHeatWave()
    {
        LoseGrowthProgress(1, true, 99);
    }

    private void ApplyFlood()
    {
        LoseGrowthProgress(1, false, 99);
    }

    private void ApplyButterflies()
    {
        FarmEventEffects.harvestBonus += 2;
    }

    private void ApplyAutumnFrost()
    {
        LoseGrowthProgress(1, false, 99);
    }

    private void ApplyLeafPile()
    {
        SpawnPickups(CompostItemID, 3);
    }

    private void ApplyBirds()
    {
        GiveRandomSeeds(1, 2);
    }

    private void ApplyAutumnRain()
    {
        GridMapMangaer map = FindObjectOfType<GridMapMangaer>();

        if (map != null)
        {
            map.WaterAllTiles();
        }

        FarmEventEffects.growthBonus += 0.1f;
    }

    private void ApplySnowCover()
    {
        FarmEventEffects.growthFrozen = true;
    }

    private void AcceptSnowRabbit()
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        GridMapMangaer map = FindObjectOfType<GridMapMangaer>();

        if (player == null || map == null || player.CurrentStamina < 8)
        {
            EventHandler.CallFarmEventEvent("雪兔出没", "你实在追不动了，雪兔溜走了", true);
            return;
        }

        player.SetStamina(player.CurrentStamina - 8);

        if (Random.value < 0.65f)
        {
            GiveItem(FurItemID, 2, 3);
            EventHandler.CallFarmEventEvent("雪兔出没", "你抓住了雪兔，得到几张柔软的毛皮", false);
        }
        else
        {
            EventHandler.CallFarmEventEvent("雪兔出没", "雪兔钻回洞里，什么也没抓到", true);
        }
    }

    private void ApplyWinterSun()
    {
        FarmEventEffects.growthBonus += 0.1f;
        GiveRandomSeeds(1, 1);
    }

    private void ApplyBlizzard()
    {
        FarmEventEffects.growthFrozen = true;
        FreezeMatureCrops(0.6f);
    }

    private void AcceptMeteor()
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        GridMapMangaer map = FindObjectOfType<GridMapMangaer>();

        if (player == null || map == null || player.CurrentStamina < 5)
        {
            EventHandler.CallFarmEventEvent("流星", "你实在没力气走过去，天亮后矿脉不见了", true);
            return;
        }

        player.SetStamina(player.CurrentStamina - 5);
        GiveItem(map.StoneItemID, 3, 5);
        EventHandler.CallFarmEventEvent("流星", "你连夜挖回了几块矿石", false);
    }

    private void AcceptScarecrow()
    {
        GridMapMangaer map = FindObjectOfType<GridMapMangaer>();

        if (map == null || InventoryManager.Instance == null)
        {
            return;
        }

        int fiber = InventoryManager.Instance.GetItemAmountInBag(map.FiberItemID);

        if (fiber < 3)
        {
            DeclineLocust();
            EventHandler.CallFarmEventEvent("虫灾", "纤维不够扎稻草人，作物被啃坏了", true);
            return;
        }

        InventoryManager.Instance.RemoveItem(map.FiberItemID, 3);
        EventHandler.CallFarmEventEvent("虫灾", "稻草人立在田边，虫群绕开了", false);
    }

    private void DeclineLocust()
    {
        LoseGrowthProgress(1, false, 3);
    }

    private void AcceptMerchant()
    {
        if (ShopManager.Instance == null)
        {
            return;
        }

        if (ShopManager.Instance.PlayerMoney < 200)
        {
            EventHandler.CallFarmEventEvent("商人到访", "金币不够，商人摇摇头走了", true);
            return;
        }

        ShopManager.Instance.PlayerMoney -= 200;
        EventHandler.CallUpdateMoneyEvent(ShopManager.Instance.PlayerMoney);
        GiveRandomSeeds(2, 3);
        EventHandler.CallFarmEventEvent("商人到访", "你买下了几颗稀有种子", false);
    }

    private bool HasScarecrow()
    {
        GridMapMangaer map = FindObjectOfType<GridMapMangaer>();

        return map != null && map.HasScarecrow();
    }

    private void SpawnPickups(int itemID, int count)
    {
        GridMapMangaer map = FindObjectOfType<GridMapMangaer>();
        CropManager cropManager = CropManager.Instance;

        if (map == null || cropManager == null)
        {
            GiveItem(itemID, 1, 2);
            return;
        }

        List<TileDetails> spots = new List<TileDetails>();

        foreach (TileDetails tile in map.GetAllTileDetails())
        {
            if (!tile.isUnlocked || tile.hasObstacle || tile.seedItemID != -1 || tile.daysSinceDug > -1)
            {
                continue;
            }

            spots.Add(tile);
        }

        if (spots.Count == 0)
        {
            GiveItem(itemID, 1, 2);
            return;
        }

        for (int i = 0; i < count; i++)
        {
            TileDetails spot = spots[Random.Range(0, spots.Count)];
            cropManager.SpawnFruit(itemID, new Vector3(spot.girdX + 0.5f, spot.girdY + 0.5f, 0f));
        }
    }

    private void GiveItem(int itemID, int min, int max)
    {
        if (itemID <= 0 || InventoryManager.Instance == null)
        {
            return;
        }

        InventoryManager.Instance.AddItem(itemID, Random.Range(min, max + 1));
    }

    private void GiveRandomSeeds(int min, int max)
    {
        CropManager cropManager = CropManager.Instance;

        if (cropManager == null || cropManager.cropData == null || cropManager.cropData.cropDetailsList == null)
        {
            return;
        }

        List<int> seeds = new List<int>();

        for (int i = 0; i < cropManager.cropData.cropDetailsList.Count; i++)
        {
            CropDetails details = cropManager.cropData.cropDetailsList[i];

            if (details != null && details.seedItemID > 0)
            {
                seeds.Add(details.seedItemID);
            }
        }

        if (seeds.Count == 0)
        {
            return;
        }

        int amount = Random.Range(min, max + 1);

        for (int i = 0; i < amount; i++)
        {
            GiveItem(seeds[Random.Range(0, seeds.Count)], 1, 1);
        }
    }

    private void DestroyRandomMatureCrop()
    {
        GridMapMangaer map = FindObjectOfType<GridMapMangaer>();
        CropManager cropManager = CropManager.Instance;

        if (map == null || cropManager == null)
        {
            return;
        }

        List<TileDetails> targets = new List<TileDetails>();

        foreach (TileDetails tile in map.GetAllTileDetails())
        {
            if (tile.seedItemID != -1 && cropManager.IsCropMature(tile))
            {
                targets.Add(tile);
            }
        }

        if (targets.Count == 0)
        {
            return;
        }

        cropManager.DestroyCrop(targets[Random.Range(0, targets.Count)]);
    }

    private void FreezeMatureCrops(float chance)
    {
        GridMapMangaer map = FindObjectOfType<GridMapMangaer>();
        CropManager cropManager = CropManager.Instance;

        if (map == null || cropManager == null)
        {
            return;
        }

        List<TileDetails> targets = new List<TileDetails>();

        foreach (TileDetails tile in map.GetAllTileDetails())
        {
            if (tile.seedItemID != -1 && !tile.inGreenhouse && cropManager.IsCropMature(tile) && Random.value < chance)
            {
                targets.Add(tile);
            }
        }

        for (int i = 0; i < targets.Count; i++)
        {
            cropManager.DestroyCrop(targets[i]);
        }
    }

    private void LoseGrowthProgress(int days, bool onlyUnwatered, int maxTiles)
    {
        GridMapMangaer map = FindObjectOfType<GridMapMangaer>();
        CropManager cropManager = CropManager.Instance;

        if (map == null || cropManager == null)
        {
            return;
        }

        int changed = 0;

        foreach (TileDetails tile in map.GetAllTileDetails())
        {
            if (tile.seedItemID == -1 || tile.inGreenhouse)
            {
                continue;
            }

            if (onlyUnwatered && (tile.isWatered || tile.daysSinceWatered > -1))
            {
                continue;
            }

            if (tile.growthDays <= 0)
            {
                continue;
            }

            tile.growthDays = Mathf.Max(0, tile.growthDays - days);
            cropManager.RefreshCropVisual(tile);

            changed++;

            if (changed >= maxTiles)
            {
                break;
            }
        }
    }
}