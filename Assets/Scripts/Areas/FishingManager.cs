using System.Collections;
using Inventory;
using UnityEngine;

public class FishingManager : MonoBehaviour
{
    private const int RodItemID = 39;
    private const int CarpItemID = 40;
    private const int BassItemID = 41;
    private const int GoldFishItemID = 42;
    private const int StaminaCost = 4;
    private const string SortingLayerName = "Ground Top";
    private const int SortingOrder = 520;

    private static FishingManager instance;

    private GameObject bobber;
    private Vector3 bobberBase;
    private int state;
    private float biteTimer;

    public static FishingManager Instance
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

        GameObject go = new GameObject("FishingManager");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<FishingManager>();
    }

    public void CastAt(Vector3 worldPosition)
    {
        int rodCount = InventoryManager.Instance != null ? InventoryManager.Instance.GetItemAmountInBag(RodItemID) : -1;
        PlayerController playerCheck = FindObjectOfType<PlayerController>();

        Debug.Log("[钓鱼] 收到抛竿请求 位置=" + worldPosition + " 是水面=" + IsPond(worldPosition) + " 鱼竿数量=" + rodCount + " 体力=" + (playerCheck != null ? playerCheck.CurrentStamina : -1) + " state=" + state);

        if (state == 1)
        {
            EventHandler.CallFarmEventEvent("钓鱼", "浮标还没动静，再等等……", true);
            return;
        }

        if (state == 2)
        {
            Reel(worldPosition);
            return;
        }

        if (!IsPond(worldPosition))
        {
            EventHandler.CallFarmEventEvent("钓鱼", "这里不是水面，钓不到鱼", true);
            return;
        }

        if (PondManager.Instance != null && !PondManager.Instance.HasMatureFish())
        {
            EventHandler.CallFarmEventEvent("钓鱼", "塘里没有成熟的鱼，先投鱼苗、喂鱼食", true);
            return;
        }

        if (InventoryManager.Instance == null || InventoryManager.Instance.GetItemAmountInBag(RodItemID) <= 0)
        {
            EventHandler.CallFarmEventEvent("钓鱼", "需要先买一根鱼竿（商店 120 金币）", true);
            return;
        }

        PlayerController player = FindObjectOfType<PlayerController>();

        if (player == null || player.CurrentStamina < StaminaCost)
        {
            EventHandler.CallFarmEventEvent("钓鱼", "体力不够，先歇一会儿", true);
            return;
        }

        player.SetStamina(player.CurrentStamina - StaminaCost);

        StartCoroutine(CastRoutine(worldPosition));
    }

    private IEnumerator CastRoutine(Vector3 worldPosition)
    {
        state = 1;
        bobberBase = worldPosition;

        CreateBobber(worldPosition);

        EventHandler.CallFarmEventEvent("钓鱼", "甩竿入水，静静等着……", false);

        yield return new WaitForSeconds(Random.Range(2f, 5f));

        if (state != 1)
        {
            yield break;
        }

        state = 2;
        biteTimer = 2.5f + (ToolDurability.GetLevel(ItemType.FishingRod) - 1) * 1f;

        EventHandler.CallFarmEventEvent("钓鱼", "有东西咬钩了！再点一下水面收竿", false);
    }

    private void Reel(Vector3 worldPosition)
    {
        state = 0;

        Vector3 position = bobber != null ? bobber.transform.position : worldPosition;

        DestroyBobber();

        if (Random.value < 0.12f)
        {
            EventHandler.CallFarmEventEvent("钓鱼", "拉起来是空的，鱼跑了", true);
            return;
        }

        int itemID = PondManager.Instance != null ? PondManager.Instance.CatchFish() : RollFish();

        if (itemID < 0)
        {
            itemID = RollFish();
        }

        if (CropManager.Instance != null)
        {
            CropManager.Instance.SpawnFruit(itemID, position);
        }

        Item item = InventoryManager.Instance != null ? InventoryManager.Instance.GetItem(itemID) : null;
        string itemName = item != null ? item.itemName : "鱼";

        EventHandler.CallFarmEventEvent("钓鱼", "钓上来一条" + itemName + "，点一下捡起来", false);
    }

    private static int RollFish()
    {
        float roll = Random.value;

        if (roll < 0.08f)
        {
            return GoldFishItemID;
        }

        if (roll < 0.38f)
        {
            return BassItemID;
        }

        return CarpItemID;
    }

    private void CreateBobber(Vector3 worldPosition)
    {
        DestroyBobber();

        GameObject prefab = Resources.Load<GameObject>("Prefabs/BobberAnim");

        if (prefab != null)
        {
            bobber = Instantiate(prefab, worldPosition, Quaternion.identity);
            Debug.Log("[钓鱼] 使用你的浮标预制体（带动画）");
        }
        else
        {
            bobber = new GameObject("FishingBobber");
            bobber.transform.position = worldPosition;

            SpriteRenderer fallbackRenderer = bobber.AddComponent<SpriteRenderer>();
            fallbackRenderer.sprite = LoadBobberSprite();

            Debug.LogWarning("[钓鱼] 没找到 Prefabs/BobberAnim，改用单张静图浮标");
        }

        SpriteRenderer[] renderers = bobber.GetComponentsInChildren<SpriteRenderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingLayerName = SortingLayerName;
            renderers[i].sortingOrder = SortingOrder;
        }
    }

    private void DestroyBobber()
    {
        if (bobber != null)
        {
            Destroy(bobber);
        }

        bobber = null;
    }

    private void CancelCast()
    {
        state = 0;
        DestroyBobber();
        EventHandler.CallFarmEventEvent("钓鱼", "收竿了", true);
    }

    private static Sprite LoadBobberSprite()
    {
        Sprite[] sliced = Resources.LoadAll<Sprite>("Art/Trees/bobber");

        if (sliced != null && sliced.Length > 0)
        {
            return sliced[0];
        }

        Texture2D texture = Resources.Load<Texture2D>("Art/Trees/bobber");

        if (texture == null)
        {
            Debug.LogWarning("[钓鱼] 找不到浮标贴图 Art/Trees/bobber");
            return null;
        }

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        float cell = Mathf.Min(24f, Mathf.Min(texture.width, texture.height));

        return Sprite.Create(
            texture,
            new Rect(0f, texture.height - cell, cell, cell),
            new Vector2(0.5f, 0.5f),
            cell);
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

    private void Update()
    {
        if (state == 2)
        {
            biteTimer -= Time.deltaTime;

            if (bobber != null)
            {
                bobber.transform.position = bobberBase + Vector3.up * Mathf.Sin(Time.time * 24f) * 0.15f;
            }

            if (biteTimer <= 0f)
            {
                state = 0;
                DestroyBobber();
                EventHandler.CallFarmEventEvent("钓鱼", "手慢了，鱼跑了", true);
            }
        }
    }
}