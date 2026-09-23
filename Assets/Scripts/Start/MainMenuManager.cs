using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;
using Inventory;

public class MainMenuManager : MonoBehaviour
{
    [Header("按钮引用")]
    [Tooltip("新游戏按钮")]
    [SerializeField] private Button newGameButton;

    [Tooltip("继续游戏按钮")]
    [SerializeField] private Button continueGameButton;

    [Tooltip("退出游戏按钮")]
    [SerializeField] private Button exitGameButton;

    [Header("场景设置")]
    [Tooltip("游戏主场景名称")]
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("存档设置")]
    [Tooltip("存档文件名")]
    [SerializeField] private string saveFileName = "farmGameSave.json";

    [Header("UI设置")]
    [Tooltip("无存档时继续按钮是否置灰")]
    [SerializeField] private bool disableContinueWhenNoSave = true;


    /// <summary>
    /// 存档文件完整路径
    /// </summary>
    private string SaveFilePath => SaveStorage.Current.GetLocation(saveFileName);

    /// <summary>
    /// 初始化,通过文件系统检查存档状态
    /// </summary>
    private void Awake()
    {
        Time.timeScale = 1f;
        UpdateContinueButtonState();

        Debug.Log($"【主菜单】初始化完成，存档路径：{SaveFilePath}");
    }


    private void OnEnable()
    {
        if (newGameButton != null)
        {
            newGameButton.onClick.RemoveListener(OnNewGameClicked);
            newGameButton.onClick.AddListener(OnNewGameClicked);
        }

        if (continueGameButton != null)
        {
            continueGameButton.onClick.RemoveListener(OnContinueGameClicked);
            continueGameButton.onClick.AddListener(OnContinueGameClicked);
        }

        if (exitGameButton != null)
        {
            exitGameButton.onClick.RemoveListener(OnExitGameClicked);
            exitGameButton.onClick.AddListener(OnExitGameClicked);
        }
    }


    private void OnDisable()
    {
        if (newGameButton != null)
        {
            newGameButton.onClick.RemoveListener(OnNewGameClicked);
        }

        if (continueGameButton != null)
        {
            continueGameButton.onClick.RemoveListener(OnContinueGameClicked);
        }

        if (exitGameButton != null)
        {
            exitGameButton.onClick.RemoveListener(OnExitGameClicked);
        }
    }

    /// <summary>
    /// 通过检查存档路径是否有文件来确定继续游戏按钮状态
    /// </summary>
    private void UpdateContinueButtonState()
    {
        if (continueGameButton == null)
        {
            return;
        }

        // 直接通过文件系统检查是否存在存档
        bool hasSaveFile = SaveStorage.Exists(saveFileName);


        // 根据配置决定是否禁用按钮
        if (disableContinueWhenNoSave)
        {
            continueGameButton.interactable = hasSaveFile;
        }
    }


    /// <summary>
    /// 新游戏按钮点击事件
    /// </summary>
    private void OnNewGameClicked()
    {
        // 禁用按钮防止重复点击
        DisableAllButtons();

        // 交给常驻的 SaveLoadManager 去执行，
        // 避免本对象随 StartScene 卸载后协程被中断
        DebtManager.ResetStory();
        AreaUnlockManager.ResetProgress();
        WaterWellManager.ResetCharges();
            SaveLoadManager.Instance.StartNewGame(gameSceneName);
    }

    /// <summary>
    /// 继续游戏按钮点击事件
    /// </summary>
    private void OnContinueGameClicked()
    {
        DisableAllButtons();

        SaveLoadManager.Instance.StartContinueGame(gameSceneName);
    }


    /// <summary>
    /// 退出游戏按钮点击事件
    /// </summary>
    private void OnExitGameClicked()
    {
        // 编辑器模式：停止运行
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
        Debug.Log("网页/小游戏环境不支持直接退出");
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// 防止加载中重复点击
    /// </summary>
    private void DisableAllButtons()
    {
        if (newGameButton != null) newGameButton.interactable = false;
        if (continueGameButton != null) continueGameButton.interactable = false;
        if (exitGameButton != null) exitGameButton.interactable = false;
    }

}