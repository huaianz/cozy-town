using System.Runtime.InteropServices;
using UnityEngine;

public static class DouyinBridge
{
    public const string AppId = "ttaef9d84430815fb407";
    public const string RewardedAdUnitId = "";

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern int Douyin_IsMiniGame();

    [DllImport("__Internal")]
    private static extern void Douyin_Login();

    [DllImport("__Internal")]
    private static extern void Douyin_SaveStorage(string key, string value);

    [DllImport("__Internal")]
    private static extern void Douyin_Share(string title);

    [DllImport("__Internal")]
    private static extern int Douyin_ShowRewardedAd(string adUnit);

    [DllImport("__Internal")]
    private static extern void Douyin_StartRecord();

    [DllImport("__Internal")]
    private static extern void Douyin_StopRecord();

    public static bool IsMiniGame()
    {
        return Douyin_IsMiniGame() == 1;
    }

    public static void Login()
    {
        Douyin_Login();
    }

    public static void SaveStorage(string key, string value)
    {
        Douyin_SaveStorage(key, value);
    }

    public static void Share(string title)
    {
        Douyin_Share(title);
    }

    public static bool ShowRewardedAd()
    {
        if (string.IsNullOrEmpty(RewardedAdUnitId))
        {
            EventHandler.CallFarmEventEvent("广告", "还没有配置广告位 ID", true);
            return false;
        }

        return Douyin_ShowRewardedAd(RewardedAdUnitId) == 1;
    }

    public static void StartRecord()
    {
        Douyin_StartRecord();
    }

    public static void StopRecord()
    {
        Douyin_StopRecord();
    }
#else
    public static bool IsMiniGame()
    {
        return false;
    }

    public static void Login()
    {
        Debug.Log("[抖音] 编辑器里不执行登录");
    }

    public static void SaveStorage(string key, string value)
    {
        PlayerPrefs.SetString(key, value);
    }

    public static void Share(string title)
    {
        EventHandler.CallFarmEventEvent("分享", "当前环境不支持分享（浏览器测试模式）", true);
    }

    public static bool ShowRewardedAd()
    {
        EventHandler.CallFarmEventEvent("广告", "当前环境不支持激励视频", true);
        return false;
    }

    public static void StartRecord()
    {
        Debug.Log("[抖音] 编辑器里不执行录屏");
    }

    public static void StopRecord()
    {
        Debug.Log("[抖音] 编辑器里不执行录屏");
    }
#endif
}