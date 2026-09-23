using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// High-performance audio manager.
/// - Auto self-instantiated (no scene drag required), persistent across scenes.
/// - SFX: fixed AudioSource pool -> caps simultaneous voices, avoids audio-thread spikes.
/// - Clips: lazy Resources.Load with cache (no upfront memory for unheard sounds).
/// - BGM: two sources cross-fade on season change; 2D; no spatial cost.
/// - Volume layers (BGM / SFX) persisted via PlayerPrefs.
/// </summary>
public class AudioManager : Singleton<AudioManager>
{
    private const string K_BGM_VOL = "CozyTown_BGMVol";
    private const string K_SFX_VOL = "CozyTown_SFXVol";
    private const string K_BGM_MUTE = "CozyTown_BGMMute";
    private const string K_SFX_MUTE = "CozyTown_SFXMute";

    private const int SFX_VOICES = 8;
    private const int SFX_VARIANT_LIMIT = 6;
    private const float FADE_SECONDS = 2.0f;

    [Header("BGM players (auto-created)")]
    public AudioSource bgmSource;      // active BGM source (kept for SettingsUI compatibility)
    public AudioSource bgmSourceAlt;

    [Header("SFX players (auto-created pool)")]
    public AudioSource sfxSource;      // first pooled source (kept for compatibility)

    private readonly AudioSource[] sfxPool = new AudioSource[SFX_VOICES];
    private int sfxCursor;

    private readonly Dictionary<SfxType, AudioClip[]> sfxCache =
        new Dictionary<SfxType, AudioClip[]>();
    private readonly Dictionary<Season, AudioClip> bgmCache =
        new Dictionary<Season, AudioClip>();

    private static readonly string[] SfxNames =
    {
        "Dig", "Water", "Plant", "Harvest", "Chop", "Mine",
        "Pickup", "Buy", "Sell", "Deliver", "Sleep"
    };
    private static readonly string[] BgmNames =
    {
        "Spring", "Summer", "Autumn", "Winter"
    };

    private float bgmVolume = 0.7f;
    private float sfxVolume = 0.9f;
    private bool bgmMuted;
    private bool sfxMuted;
    private Season currentSeason;
    private bool hasSeason;
    private Coroutine fadeRoutine;
    private Coroutine bootRoutine;
    private Coroutine bgmRoutine;
    private int bgmRetries;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("AudioManager");
        DontDestroyOnLoad(go);
        go.AddComponent<AudioManager>();
    }

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

        bgmVolume = PlayerPrefs.GetFloat(K_BGM_VOL, 0.7f);
        sfxVolume = PlayerPrefs.GetFloat(K_SFX_VOL, 0.9f);
        bgmMuted = PlayerPrefs.GetInt(K_BGM_MUTE, 0) == 1;
        sfxMuted = PlayerPrefs.GetInt(K_SFX_MUTE, 0) == 1;

        bgmSource = Create2DSource("BGM_A", true, 0f);
        bgmSourceAlt = Create2DSource("BGM_B", true, 0f);

        for (int i = 0; i < SFX_VOICES; i++)
        {
            sfxPool[i] = Create2DSource("SFX_" + i, false, 0f);
        }
        sfxSource = sfxPool[0];
    }

    private void OnEnable()
    {
        EventHandler.GameDayEvent += OnGameDay;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        StartBootRoutine();
    }

    private void OnDisable()
    {
        EventHandler.GameDayEvent -= OnGameDay;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        if (bootRoutine != null)
        {
            StopCoroutine(bootRoutine);
            bootRoutine = null;
        }
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        StartBootRoutine();
    }

    /// <summary>
    /// TimeManager only exists in the game scene, so a manager created in the main menu
    /// would never find it and the whole session stayed silent. Retry on every scene
    /// load, then keep a slow watchdog until a TimeManager shows up.
    /// </summary>
    private void StartBootRoutine()
    {
        if (hasSeason) return;
        if (bootRoutine != null) StopCoroutine(bootRoutine);
        bootRoutine = StartCoroutine(BootBgmRoutine());
    }

    private IEnumerator BootBgmRoutine()
    {
        TimeManager tm = null;
        int ticks = 0;
        while (tm == null)
        {
            tm = FindObjectOfType<TimeManager>();
            if (tm != null) break;
            ticks++;
            yield return new WaitForSeconds(ticks < 20 ? 0.5f : 3f);
        }

        int s2, m2, h2, d2, mo2, y2;
        Season season;
        tm.GetTimeData(out s2, out m2, out h2, out d2, out mo2, out y2, out season);
        EnsureStarted(season);
        bootRoutine = null;
    }

    private void EnsureStarted(Season season)
    {
        if (hasSeason) return;

        AudioClip clip = GetBgmClip(season);
        if (clip == null)
        {
            Debug.LogWarning("[音乐] 找不到背景音乐：Audio/BGM/" + season);
            return;
        }

        hasSeason = true;
        currentSeason = season;
        bgmSource.clip = clip;
        bgmSource.volume = bgmMuted ? 0f : bgmVolume;
        bgmSource.loop = true;
        bgmSource.Play();
        Debug.Log("[音乐] 开始播放：" + season + "  曲目=" + clip.name + "  时长=" + clip.length.ToString("F1") + "秒  音量=" + bgmVolume.ToString("F2") + (bgmMuted ? "  设置里已关闭" : ""));

        if (bgmRoutine != null)
        {
            StopCoroutine(bgmRoutine);
        }

        bgmRoutine = StartCoroutine(VerifyBgmRoutine());
    }

    private IEnumerator VerifyBgmRoutine()
    {
        yield return new WaitForSecondsRealtime(1.5f);

        if (bgmSource == null || bgmSource.isPlaying)
        {
            yield break;
        }

        if (bgmMuted || bgmVolume <= 0.001f)
        {
            Debug.Log("[音乐] 没有声音：设置里音乐音量是 " + bgmVolume.ToString("F2") + (bgmMuted ? "（已关闭）" : ""));
            yield break;
        }

        if (bgmRetries >= 3)
        {
            Debug.LogWarning("[音乐] 音乐始终没有播放，检查 Assets/Resources/Audio/BGM 里的文件");
            yield break;
        }

        bgmRetries++;
        Debug.LogWarning("[音乐] 音乐没播放起来，重试第 " + bgmRetries + " 次");

        hasSeason = false;
        StartBootRoutine();
    }

    private AudioSource Create2DSource(string n, bool loop, float spatial)
    {
        GameObject child = new GameObject(n);
        child.transform.SetParent(transform, false);
        AudioSource src = child.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = loop;
        src.spatialBlend = spatial;   // 0 = fully 2D, no distance/attenuation cost
        return src;
    }

    // ---------------- BGM ----------------
    private void OnGameDay(int day, Season season)
    {
        if (!hasSeason)
        {
            EnsureStarted(season);
            return;
        }
        if (season != currentSeason)
        {
            currentSeason = season;
            CrossFadeBgm(season);
        }
    }

    private AudioClip GetBgmClip(Season season)
    {
        if (bgmCache.TryGetValue(season, out AudioClip c)) return c;
        int idx = (int)season;
        string name = (idx >= 0 && idx < BgmNames.Length) ? BgmNames[idx] : null;
        AudioClip clip = string.IsNullOrEmpty(name)
            ? null : Resources.Load<AudioClip>("Audio/BGM/" + name);

        if (clip == null)
        {
            AudioClip[] all = Resources.LoadAll<AudioClip>("Audio/BGM");

            if (all != null && all.Length > 0)
            {
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] == null || string.IsNullOrEmpty(name))
                    {
                        continue;
                    }

                    if (all[i].name.ToLower().Contains(name.ToLower()))
                    {
                        clip = all[i];
                        break;
                    }
                }

                if (clip == null && idx >= 0 && idx < all.Length)
                {
                    clip = all[idx];
                }
            }
        }

        if (clip != null) bgmCache[season] = clip;
        return clip;
    }

    private void CrossFadeBgm(Season season)
    {
        AudioClip clip = GetBgmClip(season);
        if (clip == null)
        {
            Debug.LogWarning("[音乐] 找不到背景音乐：Audio/BGM/" + season);
            return;
        }
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(CrossFadeRoutine(clip));
    }

    private IEnumerator CrossFadeRoutine(AudioClip next)
    {
        AudioSource from = bgmSource;
        AudioSource to = bgmSourceAlt;
        to.clip = next;
        to.loop = true;
        to.volume = 0f;
        to.Play();

        float fromStart = from.volume;
        float target = bgmMuted ? 0f : bgmVolume;
        float elapsed = 0f;
        while (elapsed < FADE_SECONDS)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(elapsed / FADE_SECONDS);
            to.volume = target * k;
            from.volume = fromStart * (1f - k);
            yield return null;
        }
        to.volume = target;
        from.Stop();
        from.volume = 0f;

        // swap so bgmSource always points at the active one
        bgmSourceAlt = from;
        bgmSource = to;
        fadeRoutine = null;
    }

    // ---------------- SFX ----------------
    public void PlaySfx(SfxType type)
    {
        PlaySfx(type, 1f);
    }

    public void PlaySfx(SfxType type, float volumeScale)
    {
        if (sfxMuted) return;
        AudioClip clip = GetSfxClip(type);
        if (clip == null) return;

        AudioSource src = sfxPool[sfxCursor];
        sfxCursor = (sfxCursor + 1) % SFX_VOICES;
        src.pitch = UnityEngine.Random.Range(0.94f, 1.06f);
        src.volume = Mathf.Clamp01(sfxVolume * Mathf.Clamp(volumeScale, 0.1f, 2f));
        src.PlayOneShot(clip);
    }

    public void PlayClip(AudioClip clip)
    {
        if (clip == null || sfxMuted) return;
        AudioSource src = sfxPool[sfxCursor];
        sfxCursor = (sfxCursor + 1) % SFX_VOICES;
        src.pitch = 1f;
        src.volume = sfxVolume;
        src.PlayOneShot(clip);
    }

    private AudioClip GetSfxClip(SfxType type)
    {
        AudioClip[] clips = GetSfxClips(type);
        if (clips.Length == 0) return null;
        if (clips.Length == 1) return clips[0];
        return clips[UnityEngine.Random.Range(0, clips.Length)];
    }

    private AudioClip[] GetSfxClips(SfxType type)
    {
        if (sfxCache.TryGetValue(type, out AudioClip[] cached)) return cached;

        List<AudioClip> list = new List<AudioClip>(4);
        int idx = (int)type;
        string name = (idx >= 0 && idx < SfxNames.Length) ? SfxNames[idx] : null;
        if (!string.IsNullOrEmpty(name))
        {
            AudioClip first = Resources.Load<AudioClip>("Audio/SFX/" + name);
            if (first != null)
            {
                list.Add(first);
                for (int i = 2; i <= SFX_VARIANT_LIMIT; i++)
                {
                    AudioClip extra = Resources.Load<AudioClip>("Audio/SFX/" + name + "_" + i);
                    if (extra == null) break;
                    list.Add(extra);
                }
            }
        }

        AudioClip[] arr = list.ToArray();
        sfxCache[type] = arr;
        return arr;
    }

    // ---------------- Volume / mute (persisted) ----------------
    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(K_BGM_VOL, bgmVolume);
        if (bgmSource != null && !bgmMuted) bgmSource.volume = bgmVolume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(K_SFX_VOL, sfxVolume);
    }

    public void ToggleBGM(bool isOn)
    {
        bgmMuted = !isOn;
        PlayerPrefs.SetInt(K_BGM_MUTE, bgmMuted ? 1 : 0);
        if (bgmSource != null) bgmSource.volume = bgmMuted ? 0f : bgmVolume;
        if (bgmSourceAlt != null && bgmSourceAlt.isPlaying)
            bgmSourceAlt.volume = bgmMuted ? 0f : bgmVolume;
    }

    public void ToggleSFX(bool isOn)
    {
        sfxMuted = !isOn;
        PlayerPrefs.SetInt(K_SFX_MUTE, sfxMuted ? 1 : 0);
    }

    public float BgmVolume { get { return bgmVolume; } }
    public float SfxVolume { get { return sfxVolume; } }
    public bool BgmIsOn { get { return !bgmMuted; } }
    public bool SfxIsOn { get { return !sfxMuted; } }
}
