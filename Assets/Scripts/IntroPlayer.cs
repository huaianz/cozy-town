using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IntroPlayer : MonoBehaviour
{
    private static readonly string[] Titles =
    {
        "一",
        "二",
        "三",
        "四",
        "五"
    };

    private static readonly string[] Bodies =
    {
        "某天，一个从田舍小镇寄来的旧木盒放在桌上。\n里面是一把生锈的铜钥匙、一张泛黄的地契照片，\n还有外婆留下的信：老宅和那片田，一直为你留着。",
        "于是你辞去城里的工作，提着旧皮箱坐上了回乡的车。\n车窗外是成片的田、低矮的山，还有很远处的海。\n我回到了阔别多年的小镇。",
        "车站的旧木牌上写着：田舍小镇。\n青壮年多已外出，镇上安静得能听见风。\n你到来的这一天，风把牌子的灰吹掉了一些。",
        "老宅门前，老镇长已经在等你。\n他说起外婆晚年为修缮老宅欠下的费用，地契一直抵押在镇公所。\n他给了你一个约定：一年之内分三期把费用补齐，地契就归你。",
        "第一期：500 金币，春末为限。\n门前的荒地、一把旧锄头和几粒种子已经备好。\n从今天起，这块田要靠你自己养活。"
    };

    private static IntroPlayer instance;

    private GameObject canvasObject;
    private TextMeshProUGUI titleLabel;
    private TextMeshProUGUI bodyLabel;

    private TMP_FontAsset font;
    private int page;
    private bool playing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (instance != null)
        {
            return;
        }

        GameObject go = new GameObject("IntroPlayer");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<IntroPlayer>();
    }

    private void Update()
    {
        if (playing)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                Next();
            }

            return;
        }

        if (DebtManager.IntroPlayed)
        {
            return;
        }

        if (FindObjectOfType<GridMapMangaer>() == null)
        {
            return;
        }

        Play();
    }

    public void Play()
    {
        playing = true;
        page = 0;

        Build();
        ShowPage();
    }

    private void Build()
    {
        font = UiFont.GetChineseFont();

        canvasObject = new GameObject("IntroCanvas");

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 950;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(canvasObject);

        GameObject backgroundObject = new GameObject("Background");
        backgroundObject.transform.SetParent(canvasObject.transform, false);

        Image background = backgroundObject.AddComponent<Image>();
        background.color = new Color(0.08f, 0.07f, 0.06f, 1f);

        RectTransform backgroundRect = background.rectTransform;
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        GameObject buttonObject = new GameObject("NextButton");
        buttonObject.transform.SetParent(canvasObject.transform, false);

        Image buttonImage = buttonObject.AddComponent<Image>();
        Sprite buttonSprite = UiSprites.GetSliced("button_wood", 16f);

        if (buttonSprite != null)
        {
            buttonImage.sprite = buttonSprite;
            buttonImage.type = Image.Type.Sliced;
            buttonImage.color = Color.white;
        }
        else
        {
            buttonImage.color = new Color(1f, 1f, 1f, 0.15f);
        }

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(Next);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.sizeDelta = new Vector2(1920f, 1080f);
        buttonRect.anchoredPosition = new Vector2(0f, -180f);

        titleLabel = CreateLabel(canvasObject.transform, "一", 46, TextAlignmentOptions.Center);
        RectTransform titleRect = titleLabel.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(1200f, 80f);
        titleRect.anchoredPosition = new Vector2(0f, 190f);

        bodyLabel = CreateLabel(canvasObject.transform, string.Empty, 34, TextAlignmentOptions.Center);
        RectTransform bodyRect = bodyLabel.rectTransform;
        bodyRect.anchorMin = new Vector2(0.5f, 0.5f);
        bodyRect.anchorMax = new Vector2(0.5f, 0.5f);
        bodyRect.pivot = new Vector2(0.5f, 0.5f);
        bodyRect.sizeDelta = new Vector2(1300f, 360f);
        bodyRect.anchoredPosition = new Vector2(0f, 0f);
        bodyLabel.lineSpacing = 18f;

    }

    private TextMeshProUGUI CreateLabel(Transform parent, string text, int size, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject("Label");
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.alignment = alignment;
        label.fontSize = size;
        label.color = new Color(0.96f, 0.92f, 0.84f, 1f);
        label.raycastTarget = false;

        if (font != null)
        {
            label.font = font;
        }

        return label;
    }

    private void ShowPage()
    {
        if (titleLabel != null)
        {
            titleLabel.text = Titles[page];
        }

        if (bodyLabel != null)
        {
            bodyLabel.text = Bodies[page];
        }
    }

    private void Next()
    {
        if (!playing)
        {
            return;
        }

        page++;

        if (page >= Bodies.Length)
        {
            Finish();
            return;
        }

        ShowPage();
    }

    private void Finish()
    {
        playing = false;

        DebtManager.MarkIntroPlayed();
        DebtManager.StartStory();

        if (canvasObject != null)
        {
            Destroy(canvasObject);
            canvasObject = null;
        }
    }
}