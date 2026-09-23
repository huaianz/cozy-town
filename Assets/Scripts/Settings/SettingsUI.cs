using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("设置面板")]
    public GameObject settingsPanel;

    [Header("UI组件")]
    public Toggle bgmToggle;
    public Slider volumeSlider;
    public Button closeButton;
    public Button exitButton;

    private bool isSettingsOpen = false;

    [Header("存档按钮")]
    public Button saveButton;
    public Button loadButton;

    private void Start()
    {
        // 初始关闭设置面板
        settingsPanel.SetActive(false);

        // 绑定所有按钮事件
        closeButton.onClick.AddListener(CloseSettings);
        exitButton.onClick.AddListener(ExitGame);
        bgmToggle.onValueChanged.AddListener(OnBGMToggleChanged);
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

        // 初始音量同步
        volumeSlider.value = AudioManager.Instance.bgmSource.volume;
        bgmToggle.isOn = !AudioManager.Instance.bgmSource.mute;

        saveButton.onClick.AddListener(OnSaveClicked);
        loadButton.onClick.AddListener(OnLoadClicked);
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            ToggleSettings();
        }
    }

    /// <summary>
    /// 打开/关闭设置
    /// </summary>
    public void ToggleSettings()
    {
        isSettingsOpen = !isSettingsOpen;
        settingsPanel.SetActive(isSettingsOpen);
        
        // 打开设置暂停游戏，关闭恢复
        Time.timeScale = isSettingsOpen ? 0f : 1f;
    }

    /// <summary>
    /// 关闭设置
    /// </summary>
    public void CloseSettings()
    {
        isSettingsOpen = false;
        settingsPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    /// <summary>
    /// 音乐开关变化
    /// </summary>
    private void OnBGMToggleChanged(bool isOn)
    {
        AudioManager.Instance.ToggleBGM(isOn);
    }

    /// <summary>
    /// 音量变化
    /// </summary>
    private void OnVolumeChanged(float volume)
    {
        AudioManager.Instance.SetBGMVolume(volume);
    }

    /// <summary>
    /// 退出游戏
    /// </summary>
    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
        isSettingsOpen = false;

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        Time.timeScale = 1f;

        Debug.Log("网页/小游戏环境不支持直接退出，已关闭设置面板");
#else
        Application.Quit();
#endif
    }


    /// <summary>
    /// 点击保存按钮
    /// </summary>
    private void OnSaveClicked()
    {
        if (SaveLoadManager.Instance != null)
        {
            SaveLoadManager.Instance.ManualSave();
        }
    }

    /// <summary>
    /// 点击加载按钮
    /// </summary>
    private void OnLoadClicked()
    {
        if (SaveLoadManager.Instance != null)
        {
            SaveLoadManager.Instance.LoadGame();
        }
    }
}