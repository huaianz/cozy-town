using Inventory;
using UnityEngine;

public class DebtManager : MonoBehaviour
{
    private const string KeyPhase = "CozyTown_StoryPhase";
    private const string KeyPaid = "CozyTown_PaidAmount";
    private const string KeyIntro = "CozyTown_IntroPlayed";

    private static readonly int[] Amounts = { 0, 500, 2000, 5000 };
    private static readonly int[] Deadlines = { 0, 28, 56, 84 };
    private static readonly string[] Titles = { "尚未开始", "荒地", "生计", "归乡" };
    private static readonly string[] LetterTexts =
    {
        "",
        "第一封 · 土地：种下的东西或许会被风雨打倒，但土地从不会辜负认真对待它的人。",
        "第二封 · 邻里：一个人守不住一座小镇，是大家互相帮衬，才有了田舍。",
        "第三封 · 回家：我从没指望你还清什么，我只是想给你留一个，随时能回来的地方。"
    };

    private static DebtManager instance;

    private static TimeManager cachedTimeManager;
    private int currentDay;
    private int warnedDay = -1;

    public static DebtManager Instance
    {
        get { return instance; }
    }

    public static int Phase
    {
        get { return PlayerPrefs.GetInt(KeyPhase, 0); }
    }

    public static int Paid
    {
        get { return PlayerPrefs.GetInt(KeyPaid, 0); }
    }

    public static int CurrentAmount
    {
        get
        {
            int phase = Phase;

            if (phase < 1 || phase > 3)
            {
                return 0;
            }

            return Amounts[phase];
        }
    }

    public static int CurrentDeadline
    {
        get
        {
            int phase = Phase;

            if (phase < 1 || phase > 3)
            {
                return 0;
            }

            return Deadlines[phase];
        }
    }

    public static string CurrentTitle
    {
        get
        {
            int phase = Phase;

            if (phase < 1 || phase > 3)
            {
                return Titles[0];
            }

            return Titles[phase];
        }
    }

    public static bool IntroPlayed
    {
        get { return PlayerPrefs.GetInt(KeyIntro, 0) == 1; }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (instance != null)
        {
            return;
        }

        GameObject go = new GameObject("DebtManager");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<DebtManager>();
    }

    private void OnEnable()
    {
        EventHandler.GameDayEvent += OnGameDay;
    }

    private void OnDisable()
    {
        EventHandler.GameDayEvent -= OnGameDay;
    }

    public static void StartStory()
    {
        PlayerPrefs.SetInt(KeyPhase, 1);
        PlayerPrefs.SetInt(KeyPaid, 0);
        PlayerPrefs.Save();

        EventHandler.CallFarmEventEvent("赎地 · 第一期", "老镇长：一年内分三期把费用补齐，地契就归你。第一期 500 金币，请尽快筹齐。", false);
    }

    public static void ResetStory()
    {
        PlayerPrefs.SetInt(KeyIntro, 0);
        PlayerPrefs.SetInt(KeyPhase, 0);
        PlayerPrefs.SetInt(KeyPaid, 0);
        PlayerPrefs.Save();
    }

    public static void MarkIntroPlayed()
    {
        PlayerPrefs.SetInt(KeyIntro, 1);
        PlayerPrefs.Save();
    }

    public static int RemainingDays()
    {
        int deadline = CurrentDeadline;

        if (deadline <= 0)
        {
            return 0;
        }

        TimeManager timeManager = cachedTimeManager;
        if (timeManager == null)
        {
            cachedTimeManager = FindObjectOfType<TimeManager>();
            timeManager = cachedTimeManager;
        }

        if (timeManager == null)
        {
            return deadline;
        }

        int second;
        int minute;
        int hour;
        int day;
        int month;
        int year;
        Season season;

        timeManager.GetTimeData(out second, out minute, out hour, out day, out month, out year, out season);

        return Mathf.Max(0, deadline - day);
    }

    public static bool CanPay()
    {
        int phase = Phase;

        if (phase < 1 || phase > 3)
        {
            return false;
        }

        int need = Amounts[phase] - Paid;

        return need > 0 && ShopManager.Instance != null && ShopManager.Instance.PlayerMoney >= need;
    }

    public static bool Pay()
    {
        int phase = Phase;

        if (phase < 1 || phase > 3)
        {
            return false;
        }

        int need = Amounts[phase] - Paid;

        if (need <= 0)
        {
            return false;
        }

        if (ShopManager.Instance == null || ShopManager.Instance.PlayerMoney < need)
        {
            EventHandler.CallFarmEventEvent("赎地", "金币不够，还差 " + need + " 金币（需要 " + need + "）", true);
            return false;
        }

        ShopManager.Instance.PlayerMoney -= need;
        EventHandler.CallUpdateMoneyEvent(ShopManager.Instance.PlayerMoney);

        PlayerPrefs.SetInt(KeyPaid, Amounts[phase]);
        PlayerPrefs.Save();

        EventHandler.CallFarmEventEvent("赎地", "第" + phase + "期已缴清！", false);
        EventHandler.CallFarmEventEvent("回忆信", LetterTexts[phase], false);

        PlayerPrefs.SetInt(KeyPhase, phase + 1);
        PlayerPrefs.SetInt(KeyPaid, 0);
        PlayerPrefs.Save();

        if (phase >= 3)
        {
            PlayerPrefs.SetInt(KeyPhase, 4);
            PlayerPrefs.Save();

            EventHandler.CallFarmEventEvent("归乡", "老镇长把地契交到你手里：这块田，从今天起真正回家了。", false);

            EventHandler.CallStoryEndingEvent();
        }
        else
        {
            int next = phase + 1;

            EventHandler.CallFarmEventEvent("赎地 · 第" + next + "期",
                "下一期 " + Amounts[next] + " 金币，第 " + Deadlines[next] + " 天前缴清。", false);
        }

        return true;
    }

    private void OnGameDay(int day, Season season)
    {
        currentDay = day;

        int phase = Phase;

        if (phase < 1 || phase > 3)
        {
            return;
        }

        int deadline = Deadlines[phase];
        int need = Amounts[phase] - Paid;

        if (need <= 0)
        {
            return;
        }

        if (day == deadline - 3 && warnedDay != day)
        {
            warnedDay = day;
            EventHandler.CallFarmEventEvent("赎地提醒", "距第 " + phase + " 期还款还有 3 天，还差 " + need + " 金币", true);
        }

        if (day >= deadline && warnedDay != day + 1000)
        {
            warnedDay = day + 1000;
            EventHandler.CallFarmEventEvent("赎地宽限", "第 " + phase + " 期还差 " + need + " 金币。镇长说：不着急，先宽限几天。", true);
        }
    }
}