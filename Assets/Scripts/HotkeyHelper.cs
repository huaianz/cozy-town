using Inventory;
using UnityEngine;

public class HotkeyHelper : MonoBehaviour
{
    private static HotkeyHelper instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (instance != null)
        {
            return;
        }

        GameObject go = new GameObject("HotkeyHelper");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<HotkeyHelper>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            DeedPanelUI.Toggle();
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            SkipToEvening();
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            IntroPlayer intro = FindObjectOfType<IntroPlayer>();

            if (intro != null)
            {
                PlayerPrefs.SetInt("CozyTown_IntroPlayed", 0);
                PlayerPrefs.Save();
                intro.Play();
            }
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            AddMoney(1000);
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            DebtManager.StartStory();
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            AreaUnlockPanelUI.Toggle();
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            UnlockAllAreas();
        }
    }

    private static void UnlockAllAreas()
    {
        AreaUnlockManager manager = AreaUnlockManager.Instance;

        if (manager == null)
        {
            return;
        }

        for (int i = 0; i < manager.Areas.Count; i++)
        {
            AreaUnlockManager.AreaInfo area = manager.Areas[i];
            PlayerPrefs.SetInt("CozyTown_Area_" + area.kind, 1);
        }

        PlayerPrefs.Save();
        manager.Refresh();
        AreaUnlockPanelUI.RefreshIfOpen();
        Debug.Log("[调试] 已解锁全部区域");
    }

    private static void AddMoney(int amount)
    {
        if (ShopManager.Instance == null)
        {
            return;
        }

        ShopManager.Instance.PlayerMoney += amount;
        EventHandler.CallUpdateMoneyEvent(ShopManager.Instance.PlayerMoney);
        EventHandler.CallFarmEventEvent("调试", "金币 +" + amount + "，当前 " + ShopManager.Instance.PlayerMoney, false);
    }

    private static void SkipToEvening()
    {
        TimeManager timeManager = FindObjectOfType<TimeManager>();

        if (timeManager == null)
        {
            return;
        }

        int second;
        int minute;
        int hour;
        int day;
        int month;
        int year;
        Season season;

        timeManager.GetTimeData(out second, out minute, out hour, out day, out month, out year, out season);
        timeManager.SetTimeData(0, 0, 18, day, month, year, season);

        EventHandler.CallFarmEventEvent("调试", "时间跳到 18:00，可以去睡觉了", false);
    }
}