using System;

public interface IPlatformBridge
{
    string PlatformName { get; }

    bool IsMiniGame { get; }

    void Login(Action<bool, string> onComplete);

    void Share(string title, Action<bool> onComplete);

    void ShowRewardedAd(Action<bool> onComplete);

    void Vibrate(int milliseconds);
}

public class DefaultPlatformBridge : IPlatformBridge
{
    public string PlatformName
    {
        get { return "PC"; }
    }

    public bool IsMiniGame
    {
        get { return false; }
    }

    public void Login(Action<bool, string> onComplete)
    {
        if (onComplete != null)
        {
            onComplete(false, string.Empty);
        }
    }

    public void Share(string title, Action<bool> onComplete)
    {
        if (onComplete != null)
        {
            onComplete(false);
        }
    }

    public void ShowRewardedAd(Action<bool> onComplete)
    {
        if (onComplete != null)
        {
            onComplete(false);
        }
    }

    public void Vibrate(int milliseconds)
    {
    }
}

public class MiniGamePlatformBridge : IPlatformBridge
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern int MiniGame_IsAvailable();

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern int MiniGame_Share(string title);

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void MiniGame_Vibrate(int milliseconds);
#endif

    public string PlatformName
    {
        get { return "MiniGame"; }
    }

    public bool IsMiniGame
    {
        get { return true; }
    }

    public void Login(Action<bool, string> onComplete)
    {
        if (onComplete != null)
        {
            onComplete(false, string.Empty);
        }
    }

    public void Share(string title, Action<bool> onComplete)
    {
        bool success = false;

#if UNITY_WEBGL && !UNITY_EDITOR
        if (MiniGame_IsAvailable() == 1)
        {
            success = MiniGame_Share(title) == 1;
        }
#endif

        if (onComplete != null)
        {
            onComplete(success);
        }
    }

    public void ShowRewardedAd(Action<bool> onComplete)
    {
        if (onComplete != null)
        {
            onComplete(false);
        }
    }

    public void Vibrate(int milliseconds)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (MiniGame_IsAvailable() == 1)
        {
            MiniGame_Vibrate(milliseconds);
        }
#endif
    }
}

public static class PlatformBridge
{
    private static IPlatformBridge current;

    public static IPlatformBridge Current
    {
        get
        {
            if (current == null)
            {
                current = CreateDefault();
            }

            return current;
        }
    }

    private static IPlatformBridge CreateDefault()
    {
#if UNITY_WEBGL
        return new MiniGamePlatformBridge();
#else
        return new DefaultPlatformBridge();
#endif
    }
}