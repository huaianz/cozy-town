using System.Collections;
using System.Collections.Generic;
using Inventory;
using UnityEngine;

public class FruitTreeManager : MonoBehaviour
{
    private const string FruitTreeTag = "fruit tree";
    private const string ChopTreeTag = "Tree";
    private const int WoodItemID = 27;
    private const int FruitItemID = 35;
    private const string FruitSortingLayer = "Ground Top";
    private const int FruitSortingOrder = 500;

    private class TreeData
    {
        public GameObject root;
        public GameObject top;
        public GameObject fruit;
        public GameObject active;
        public Season displayedSeason = Season.春天;
        public bool isFruitTree;
        public bool ripe;
        public readonly System.Collections.Generic.List<GameObject> fruits = new System.Collections.Generic.List<GameObject>();
        public int fruitTimer;
        public int hits;
        public int hitsNeeded = 3;
        public bool falling;
    }

    private static FruitTreeManager instance;

    private readonly List<TreeData> trees = new List<TreeData>();
    private bool built;
    private Season currentSeason = Season.春天;

    public static FruitTreeManager Instance
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

        GameObject go = new GameObject("FruitTreeManager");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<FruitTreeManager>();
    }

    private void OnEnable()
    {
        EventHandler.GameDayEvent += OnGameDay;
        EventHandler.WeatherChangedEvent += OnWeatherChanged;
    }

    private void OnDisable()
    {
        EventHandler.GameDayEvent -= OnGameDay;
        EventHandler.WeatherChangedEvent -= OnWeatherChanged;
    }

    private void OnGameDay(int day, Season season)
    {
        currentSeason = season;
        EnsureTrees();

        for (int i = 0; i < trees.Count; i++)
        {
            TreeData tree = trees[i];

            if (tree.fruitTimer > 0)
            {
                tree.fruitTimer--;

                if (tree.fruitTimer <= 0)
                {
                    tree.ripe = true;
                }
            }
        }

        ApplyFruitVisual();
    }

    private void OnWeatherChanged(WeatherType weather)
    {
        EnsureTrees();

        if (weather == WeatherType.HeavyRain || weather == WeatherType.Thunderstorm)
        {
            DropFruit(weather == WeatherType.Thunderstorm ? 0.5f : 0.3f);
        }
    }

    private bool tilesRegistered;
    private float retryTimer;

    private void Start()
    {
        EnsureTrees();
    }

    private void Update()
    {
        if (trees.Count == 0)
        {
            built = false;
            tilesRegistered = false;
        }

        if (tilesRegistered)
        {
            return;
        }

        retryTimer += Time.deltaTime;

        if (retryTimer < 0.5f)
        {
            return;
        }

        retryTimer = 0f;
        EnsureTrees();
        RegisterAllTiles();
    }

    private void RegisterAllTiles()
    {
        if (GridMapMangaer.Instance == null || trees.Count == 0)
        {
            return;
        }

        for (int i = 0; i < trees.Count; i++)
        {
            if (trees[i].root != null)
            {
                RegisterTileUnder(trees[i].root.transform.position);
            }
        }

        tilesRegistered = true;

        Debug.Log("[果园] 地块登记完成，共 " + trees.Count + " 棵树");
    }

    private void RegisterByName()
    {
        Transform[] all = Resources.FindObjectsOfTypeAll<Transform>();

        for (int i = 0; i < all.Length; i++)
        {
            Transform tr = all[i];

            if (tr == null)
            {
                continue;
            }

            GameObject candidate = tr.gameObject;

            if (candidate.scene.name == null)
            {
                continue;
            }

            string lower = candidate.name.ToLower();

            if (lower.Contains("croptree"))
            {
                RegisterTree(candidate, false);
            }
            else if (lower.Contains("tree green") || lower.Contains("treepink") || lower.Contains("treeyellow"))
            {
                RegisterTree(candidate, true);
            }
        }

        Debug.Log("[果园] 按名字兜底识别，共 " + trees.Count + " 棵");
    }

    public void EnsureTrees()
    {
        if (built)
        {
            return;
        }

        built = true;

        GameObject[] fruitObjects = FindTagged(FruitTreeTag);

        for (int i = 0; i < fruitObjects.Length; i++)
        {
            RegisterTree(fruitObjects[i], true);
        }

        GameObject[] chopObjects = FindTagged(ChopTreeTag);

        for (int i = 0; i < chopObjects.Length; i++)
        {
            RegisterTree(chopObjects[i], false);
        }

        if (trees.Count == 0)
        {
            RegisterByName();
        }

        Debug.Log("[果园] 识别到果树 " + CountFruitTrees() + " 棵，可砍树 " + (trees.Count - CountFruitTrees()) + " 棵");

        ApplyFruitVisual();
    }

    private int CountFruitTrees()
    {
        int count = 0;

        for (int i = 0; i < trees.Count; i++)
        {
            if (trees[i].isFruitTree)
            {
                count++;
            }
        }

        return count;
    }

    private static GameObject[] FindTagged(string tag)
    {
        try
        {
            return GameObject.FindGameObjectsWithTag(tag);
        }
        catch
        {
            Debug.LogWarning("[果园] 场景里没有定义 Tag: " + tag + "（请在 Tags and Layers 里添加）");
            return new GameObject[0];
        }
    }

    private void RegisterTree(GameObject root, bool isFruitTree)
    {
        if (root == null)
        {
            return;
        }

        for (int i = 0; i < trees.Count; i++)
        {
            if (trees[i].root == root)
            {
                return;
            }
        }

        TreeData data = new TreeData();
        data.root = root;
        data.isFruitTree = isFruitTree;
        data.ripe = isFruitTree;
        data.fruitTimer = 0;
        data.hitsNeeded = Random.Range(3, 6);
        data.top = FindChild(root.transform, "tob");

        if (data.top == null)
        {
            data.top = FindChild(root.transform, "top");
        }
        data.fruit = FindChild(root.transform, "fruit");
        data.active = root;
        data.displayedSeason = Season.春天;

        trees.Add(data);

        RegisterTileUnder(root.transform.position);
    }

    private void RegisterTileUnder(Vector3 position)
    {
        GridMapMangaer map = GridMapMangaer.Instance;

        if (map == null)
        {
            return;
        }

        int baseX = Mathf.FloorToInt(position.x);
        int baseY = Mathf.FloorToInt(position.y);

        for (int dx = -2; dx <= 2; dx++)
        {
            for (int dy = -3; dy <= 3; dy++)
            {
                map.RegisterGeneratedTile(baseX + dx, baseY + dy);
            }
        }
    }

    private static GameObject FindChild(Transform parent, string keyword)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child.name.ToLower().Contains(keyword))
            {
                return child.gameObject;
            }

            GameObject deeper = FindChild(child, keyword);

            if (deeper != null)
            {
                return deeper;
            }
        }

        return null;
    }

    private void SpawnFruits(TreeData tree)
    {
        if (tree.root == null)
        {
            return;
        }

        Sprite apple = Resources.Load<Sprite>("Art/Items/Plants/Apple_hold");

        if (apple == null)
        {
            Debug.LogWarning("[果园] 找不到苹果贴图：Art/Items/Plants/Apple_hold");
            return;
        }

        SpriteRenderer topRenderer = null;

        if (tree.top != null)
        {
            topRenderer = tree.top.GetComponent<SpriteRenderer>();
        }

        if (topRenderer == null)
        {
            topRenderer = tree.root.GetComponentInChildren<SpriteRenderer>();
        }

        if (topRenderer == null)
        {
            return;
        }

        Bounds area = topRenderer.bounds;
        int count = Random.Range(5, 9);

        for (int i = 0; i < count; i++)
        {
            GameObject fruit = new GameObject("Fruit");
            fruit.transform.SetParent(tree.root.transform, true);

            float x = Random.Range(area.min.x + 0.3f, area.max.x - 0.3f);
            float y = Random.Range(area.min.y + 0.2f, area.center.y);

            fruit.transform.position = new Vector3(x, y, 0f);
            fruit.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

            SpriteRenderer renderer = fruit.AddComponent<SpriteRenderer>();
            renderer.sprite = apple;
            int highestOrder = topRenderer.sortingOrder;
            SpriteRenderer[] renderers = tree.root.GetComponentsInChildren<SpriteRenderer>(true);

            for (int r = 0; r < renderers.Length; r++)
            {
                if (renderers[r] != null && renderers[r].sortingOrder > highestOrder)
                {
                    highestOrder = renderers[r].sortingOrder;
                }
            }

            renderer.sortingLayerName = FruitSortingLayer;
            renderer.sortingOrder = FruitSortingOrder;

            tree.fruits.Add(fruit);
        }

        Debug.Log("[果园] 树上长出果子 " + count + " 个");
    }

    private void ClearFruits(TreeData tree)
    {
        for (int i = 0; i < tree.fruits.Count; i++)
        {
            if (tree.fruits[i] != null)
            {
                Destroy(tree.fruits[i]);
            }
        }

        tree.fruits.Clear();
    }

    private void ApplySeason(TreeData tree)
    {
        if (!tree.isFruitTree || tree.displayedSeason == currentSeason)
        {
            return;
        }

        string prefabName = currentSeason == Season.夏天
            ? "Prefabs/TreePink"
            : (currentSeason == Season.秋天 ? "Prefabs/TreeYellow" : "Prefabs/Tree Green");

        GameObject prefab = Resources.Load<GameObject>(prefabName);

        if (prefab == null)
        {
            Debug.LogWarning("[果园] 找不到季节预制体: " + prefabName);
            tree.displayedSeason = currentSeason;
            return;
        }

        Vector3 position = tree.root.transform.position;
        Quaternion rotation = tree.root.transform.rotation;

        if (tree.active != null && tree.active != tree.root)
        {
            Destroy(tree.active);
        }

        tree.root.SetActive(false);

        GameObject variant = Instantiate(prefab, position, rotation);
        tree.active = variant;
        tree.top = FindChild(variant.transform, "tob");

        if (tree.top == null)
        {
            tree.top = FindChild(variant.transform, "top");
        }

        tree.fruit = null;
        tree.fruits.Clear();
        tree.displayedSeason = currentSeason;

        Debug.Log("[果园] 换季：" + currentSeason + " 已替换为 " + prefabName);
    }

    private void ApplyFruitVisual()
    {
        bool winter = currentSeason == Season.冬天;

        for (int i = 0; i < trees.Count; i++)
        {
            TreeData tree = trees[i];

            if (tree.root == null)
            {
                continue;
            }

            ApplySeason(tree);

            if (tree.isFruitTree && tree.top != null)
            {
                tree.top.SetActive(!winter);
            }

            bool showFruit = tree.isFruitTree && tree.ripe && !winter && currentSeason == Season.秋天;

            if (showFruit && tree.fruits.Count == 0)
            {
                SpawnFruits(tree);
            }
            else if (!showFruit && tree.fruits.Count > 0)
            {
                ClearFruits(tree);
            }
        }
    }

    private TreeData FindTree(GameObject target)
    {
        if (target == null)
        {
            return null;
        }

        for (int i = 0; i < trees.Count; i++)
        {
            TreeData tree = trees[i];

            if (tree.root == null)
            {
                continue;
            }

            if (tree.root == target || target.transform.IsChildOf(tree.root.transform) || tree.root.transform.IsChildOf(target.transform))
            {
                return tree;
            }
        }

        return null;
    }

    private TreeData FindTreeNear(Vector3 point, float maxDistance)
    {
        TreeData best = null;
        float bestDistance = maxDistance;

        for (int i = 0; i < trees.Count; i++)
        {
            TreeData tree = trees[i];

            if (tree.root == null)
            {
                continue;
            }

            float distance = Vector3.Distance(tree.root.transform.position, point);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = tree;
            }
        }

        return best;
    }

    public bool HasTree(int x, int y)
    {
        return FindTreeNear(new Vector3(x + 0.5f, y + 0.5f, 0f), 3.5f) != null;
    }

    public bool CanHarvest(int x, int y)
    {
        TreeData tree = FindTreeNear(new Vector3(x + 0.5f, y + 0.5f, 0f), 3.5f);

        if (tree == null || !tree.isFruitTree || !tree.ripe)
        {
            return false;
        }

        TimeManager timeManager = FindObjectOfType<TimeManager>();

        return timeManager != null ? timeManager.GetSeason() == Season.秋天 : currentSeason == Season.秋天;
    }

    public bool TryHarvest(int x, int y)
    {
        TreeData tree = FindTreeNear(new Vector3(x + 0.5f, y + 0.5f, 0f), 3.5f);

        if (tree == null || !tree.isFruitTree)
        {
            Debug.Log("[果园] 镰刀：这个位置没有果树");
            return false;
        }

        if (!tree.ripe)
        {
            Debug.Log("[果园] 镰刀：这棵树的果子还没成熟（收完要等 3 天）");
            return false;
        }

        TimeManager timeManager = FindObjectOfType<TimeManager>();
        Season season = timeManager != null ? timeManager.GetSeason() : currentSeason;

        if (season != Season.秋天)
        {
            Debug.Log("[果园] 镰刀：现在不是秋天，果树还没结果");
            return false;
        }

        tree.ripe = false;
        tree.fruitTimer = 3;
        ApplyFruitVisual();

        int amount = Random.Range(2, 5);
        Vector3 position = tree.root.transform.position;

        for (int i = 0; i < amount; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-1.8f, 1.8f), Random.Range(-2.8f, -1.4f), 0f);

            if (CropManager.Instance != null)
            {
                CropManager.Instance.SpawnFruit(FruitItemID, position + offset);
            }
        }

        EventHandler.CallFarmEventEvent("果园", "收获了 " + amount + " 个果子", false);

        return true;
    }

    public bool TryChop(int x, int y)
    {
        TreeData tree = FindTreeNear(new Vector3(x + 0.5f, y + 0.5f, 0f), 3.5f);

        if (tree == null)
        {
            Debug.Log("[果园] 斧头：这个位置没有找到树");
            return false;
        }

        if (tree.isFruitTree)
        {
            Debug.Log("[果园] 斧头：这是果树（fruit tree），不能砍");
            return false;
        }

        if (tree.falling)
        {
            return false;
        }

        tree.hits++;

        bool finalHit = tree.hits >= tree.hitsNeeded;

        StartCoroutine(ChopHitRoutine(tree, finalHit));

        if (finalHit)
        {
            EventHandler.CallFarmEventEvent("砍树", "树被砍倒了！", false);
        }
        else
        {
            EventHandler.CallFarmEventEvent("砍树", "砍了一下，还需要 " + (tree.hitsNeeded - tree.hits) + " 下", false);
        }

        return true;
    }

    private static void KeepStump(GameObject root)
    {
        bool hasStump = false;

        for (int i = 0; i < root.transform.childCount; i++)
        {
            Transform child = root.transform.GetChild(i);
            bool isStump = child.name.ToLower().Contains("bottom");

            child.gameObject.SetActive(isStump);

            if (isStump)
            {
                hasStump = true;
            }
        }

        if (!hasStump)
        {
            root.SetActive(false);
        }
        else
        {
            Debug.Log("[果园] 树墩保留了：" + root.name);
        }
    }

    private void DropWood(Vector3 position)
    {
        GameObject prefab = Resources.Load<GameObject>("Prefabs/BounceItemBase");

        if (prefab == null)
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.AddItem(WoodItemID, Random.Range(2, 4));
            }

            Debug.LogWarning("[果园] 找不到 Prefabs/BounceItemBase，木材直接进背包了");
            return;
        }

        int amount = Random.Range(2, 4);

        for (int i = 0; i < amount; i++)
        {
            Vector3 spawnPosition = position + new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(0f, 0.4f), 0f);

            if (prefab.GetComponent<FruitPickup>() != null)
            {
                FruitPickup.Spawn(prefab, WoodItemID, spawnPosition);
            }
            else
            {
                GameObject drop = Instantiate(prefab, spawnPosition, Quaternion.identity);
                FruitPickup pickup = drop.GetComponent<FruitPickup>();

                if (pickup == null)
                {
                    pickup = drop.AddComponent<FruitPickup>();
                }

                pickup.useColliderDetection = true;
                pickup.Initialize(WoodItemID);
            }
        }

        Debug.Log("[果园] 掉落了 " + amount + " 个木材掉落物，点击拾取");
    }

    private IEnumerator ChopHitRoutine(TreeData tree, bool finalHit)
    {
        Animator animator = tree.root != null ? tree.root.GetComponentInChildren<Animator>() : null;

        if (animator != null)
        {
            if (finalHit)
            {
                tree.falling = true;
                animator.SetTrigger(Random.value < 0.5f ? "FallingLeft" : "FallingRight");
                yield return new WaitForSeconds(0.9f);
            }
            else
            {
                animator.SetTrigger(tree.hits % 2 == 0 ? "RotateRight" : "RotateLeft");
                yield return new WaitForSeconds(0.35f);
            }
        }
        else
        {
            yield return new WaitForSeconds(0.25f);
        }

        if (!finalHit)
        {
            yield break;
        }

        Vector3 dropPosition = new Vector3(0f, 0f, 0f);

        if (tree.root != null)
        {
            dropPosition = tree.root.transform.position;
            KeepStump(tree.root);
        }

        trees.Remove(tree);

        DropWood(dropPosition);

        EventHandler.CallFarmEventEvent("砍树", "砍倒了一棵树，树墩留在了原地", false);
    }

    private float sortTimer;

    private void LateUpdate()
    {
        sortTimer += Time.deltaTime;

        if (sortTimer < 1f)
        {
            return;
        }

        sortTimer = 0f;

        FruitPickup[] pickups = FindObjectsOfType<FruitPickup>();

        for (int i = 0; i < pickups.Length; i++)
        {
            SpriteRenderer renderer = pickups[i] != null ? pickups[i].GetComponentInChildren<SpriteRenderer>() : null;

            if (renderer == null)
            {
                continue;
            }

            if (renderer.sortingLayerName != FruitSortingLayer || renderer.sortingOrder < FruitSortingOrder)
            {
                renderer.sortingLayerName = FruitSortingLayer;
                renderer.sortingOrder = FruitSortingOrder;
            }
        }
    }

    private void DropFruit(float chance)
    {
        for (int i = 0; i < trees.Count; i++)
        {
            TreeData tree = trees[i];

            if (tree.root == null || !tree.isFruitTree || !tree.ripe)
            {
                continue;
            }

            if (Random.value > chance)
            {
                continue;
            }

            tree.ripe = false;
            tree.fruitTimer = 3;

            if (CropManager.Instance != null)
            {
                CropManager.Instance.SpawnFruit(FruitItemID, tree.root.transform.position + new Vector3(0f, -1f, 0f));
            }
        }

        ApplyFruitVisual();
        EventHandler.CallFarmEventEvent("风雨", "风雨把树上的果子吹落了一些", true);
    }
}