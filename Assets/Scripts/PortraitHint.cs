using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PortraitHint : MonoBehaviour
{
    private const float ShowDuration = 8f;
    private const float BannerHeightRatio = 0.13f;

    private static PortraitHint instance;

    private GameObject canvasObject;
    private TextMeshProUGUI label;
    private float shownTime;
    private bool dismissed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (instance != null)
        {
            return;
        }

        GameObject go = new GameObject("PortraitHint");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<PortraitHint>();
    }

    private void Update()
    {
        bool portrait = Screen.height > Screen.width;

        if (!portrait)
        {
            dismissed = false;
            shownTime = 0f;
        }

        if (dismissed)
        {
            Hide();
            return;
        }

        bool shouldShow = portrait && (Application.isMobilePlatform || PointerInput.HasUsedTouch);

        if (!shouldShow)
        {
            Hide();
            return;
        }

        EnsureUI();

        shownTime += Time.unscaledDeltaTime;

        if (shownTime >= ShowDuration)
        {
            dismissed = true;
            Hide();
            return;
        }

        if (canvasObject != null && !canvasObject.activeSelf)
        {
            canvasObject.SetActive(true);
        }
    }

    private void Hide()
    {
        if (canvasObject != null && canvasObject.activeSelf)
        {
            canvasObject.SetActive(false);
        }
    }

    private void EnsureUI()
    {
        if (canvasObject != null)
        {
            return;
        }

        canvasObject = new GameObject("PortraitHintCanvas");

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;

        canvasObject.AddComponent<CanvasScaler>();
        DontDestroyOnLoad(canvasObject);

        GameObject bannerObject = new GameObject("Banner");
        bannerObject.transform.SetParent(canvasObject.transform, false);

        Image banner = bannerObject.AddComponent<Image>();
        banner.color = new Color(0f, 0f, 0f, 0.72f);
        banner.raycastTarget = false;

        RectTransform bannerRect = banner.rectTransform;
        bannerRect.anchorMin = new Vector2(0f, 1f - BannerHeightRatio);
        bannerRect.anchorMax = new Vector2(1f, 1f);
        bannerRect.offsetMin = Vector2.zero;
        bannerRect.offsetMax = Vector2.zero;

        GameObject textObject = new GameObject("Label");
        textObject.transform.SetParent(bannerObject.transform, false);

        label = textObject.AddComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 46;
        label.color = Color.white;
        label.raycastTarget = false;
        label.enableWordWrapping = false;

        TMP_FontAsset font = UiFont.GetChineseFont();

        if (font != null)
        {
            label.font = font;
            label.text = "把手机横过来可以看到整片农场";
        }
        else
        {
            label.text = "Rotate your phone to see the whole farm";
        }

        RectTransform textRect = label.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }
}