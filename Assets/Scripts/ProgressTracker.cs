using UnityEngine;

public class ProgressTracker : MonoBehaviour
{
    private static ProgressTracker instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (instance != null)
        {
            return;
        }

        GameObject go = new GameObject("ProgressTracker");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<ProgressTracker>();
    }

    private void OnEnable()
    {
        EventHandler.FarmEventEvent += OnFarmEvent;
        EventHandler.CommissionChangedEvent += OnCommissionChanged;
    }

    private void OnDisable()
    {
        EventHandler.FarmEventEvent -= OnFarmEvent;
        EventHandler.CommissionChangedEvent -= OnCommissionChanged;
    }

    private void OnFarmEvent(string title, string description, bool disaster)
    {
        ProgressManager.RegisterEvent(title);
        ProgressManager.RegisterEvent(description);
        ProgressManager.CheckAlmanac();
    }

    private void OnCommissionChanged()
    {
        ProgressManager.CheckAlmanac();
    }
}