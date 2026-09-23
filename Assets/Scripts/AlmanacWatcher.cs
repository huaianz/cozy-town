using Inventory;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AlmanacWatcher : MonoBehaviour
{
    private static AlmanacWatcher instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (instance != null)
        {
            return;
        }

        GameObject go = new GameObject("AlmanacWatcher");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<AlmanacWatcher>();
    }

    private void OnEnable()
    {
        EventHandler.UpdateInventoryUI += OnInventoryChanged;
        EventHandler.GameDayEvent += OnGameDay;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        EventHandler.UpdateInventoryUI -= OnInventoryChanged;
        EventHandler.GameDayEvent -= OnGameDay;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnInventoryChanged(InventoryLocation location, System.Collections.Generic.List<InventoryItem> items)
    {
        AlmanacManager.SyncBag();
    }

    private void OnGameDay(int day, Season season)
    {
        AlmanacManager.SyncBag();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AlmanacManager.SyncBag();
    }
}