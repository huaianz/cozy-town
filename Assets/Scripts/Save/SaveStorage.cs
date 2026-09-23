using System.IO;
using UnityEngine;

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

public interface ISaveStorage
{
    string GetLocation(string key);

    bool Exists(string key);

    string Read(string key);

    void Write(string key, string json);

    void Delete(string key);
}

public class FileSaveStorage : ISaveStorage
{
    public string GetLocation(string key)
    {
        return Path.Combine(Application.persistentDataPath, key);
    }

    public bool Exists(string key)
    {
        return File.Exists(GetLocation(key));
    }

    public string Read(string key)
    {
        return File.ReadAllText(GetLocation(key));
    }

    public void Write(string key, string json)
    {
        string path = GetLocation(key);
        string tempPath = path + ".tmp";

        File.WriteAllText(tempPath, json);

        if (File.Exists(path))
        {
            File.Replace(tempPath, path, null);
        }
        else
        {
            File.Move(tempPath, path);
        }
    }

    public void Delete(string key)
    {
        string path = GetLocation(key);

        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}

public class PlayerPrefsSaveStorage : ISaveStorage
{
    public string GetLocation(string key)
    {
        return "PlayerPrefs:" + key;
    }

    public bool Exists(string key)
    {
        return PlayerPrefs.HasKey(key);
    }

    public string Read(string key)
    {
        return PlayerPrefs.GetString(key, string.Empty);
    }

    public void Write(string key, string json)
    {
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save();
    }

    public void Delete(string key)
    {
        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
    }
}

public class MiniGameSaveStorage : ISaveStorage
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern int MiniGame_IsAvailable();

    [DllImport("__Internal")]
    private static extern string MiniGame_GetStorage(string key);

    [DllImport("__Internal")]
    private static extern void MiniGame_SetStorage(string key, string value);

    [DllImport("__Internal")]
    private static extern void MiniGame_DeleteStorage(string key);
#endif

    private readonly PlayerPrefsSaveStorage fallback = new PlayerPrefsSaveStorage();

    private static bool Available
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return MiniGame_IsAvailable() == 1;
#else
            return false;
#endif
        }
    }

    public string GetLocation(string key)
    {
        if (Available)
        {
            return "MiniGameStorage:" + key;
        }

        return fallback.GetLocation(key);
    }

    public bool Exists(string key)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (Available)
        {
            return !string.IsNullOrEmpty(MiniGame_GetStorage(key));
        }
#endif

        return fallback.Exists(key);
    }

    public string Read(string key)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (Available)
        {
            return MiniGame_GetStorage(key);
        }
#endif

        return fallback.Read(key);
    }

    public void Write(string key, string json)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (Available)
        {
            MiniGame_SetStorage(key, json);
            return;
        }
#endif

        fallback.Write(key, json);
    }

    public void Delete(string key)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (Available)
        {
            MiniGame_DeleteStorage(key);
            return;
        }
#endif

        fallback.Delete(key);
    }
}

public static class SaveStorage
{
    private static ISaveStorage current;

    public static ISaveStorage Current
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

    public static bool Exists(string key)
    {
        return Current.Exists(key);
    }

    private static ISaveStorage CreateDefault()
    {
#if UNITY_WEBGL
        return new MiniGameSaveStorage();
#else
        return new FileSaveStorage();
#endif
    }
}