using UnityEngine;
using UnityEngine.EventSystems;

public class PickupClickHandler : MonoBehaviour
{
    private static PickupClickHandler instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (instance != null)
        {
            return;
        }

        GameObject go = new GameObject("PickupClickHandler");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<PickupClickHandler>();
    }

    private void Update()
    {
        if (!PointerInput.TappedThisFrame)
        {
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Camera mainCamera = Camera.main;

        if (mainCamera == null)
        {
            return;
        }

        Vector2 screenPoint = PointerInput.Position;
        Vector3 worldPoint = mainCamera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, 0f));

        if (TryCollect(worldPoint))
        {
            return;
        }
    }

    private static bool TryCollect(Vector3 worldPoint)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPoint);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null)
            {
                continue;
            }

            FruitPickup pickup = hits[i].GetComponent<FruitPickup>();

            if (pickup == null)
            {
                pickup = hits[i].GetComponentInParent<FruitPickup>();
            }

            if (pickup != null)
            {
                pickup.Collect();
                Debug.Log("[拾取] 点击拾取成功");

                return true;
            }
        }

        FruitPickup[] all = FindObjectsOfType<FruitPickup>();
        FruitPickup nearest = null;
        float bestDistance = 1.3f;

        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null || !all[i].gameObject.activeInHierarchy)
            {
                continue;
            }

            float distance = Vector3.Distance(all[i].transform.position, worldPoint);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                nearest = all[i];
            }
        }

        if (nearest != null)
        {
            nearest.Collect();
            Debug.Log("[拾取] 点击拾取成功（范围兜底）");

            return true;
        }

        return false;
    }
}