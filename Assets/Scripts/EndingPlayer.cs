using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-time ending cutscene "归乡宴". Purely event-driven (no per-frame polling),
/// builds its canvas once, plays a few pages, then destroys it -> zero resident cost.
/// </summary>
public class EndingPlayer : MonoBehaviour
{
    private static readonly string[] Titles =
    {
        "归乡", "归乡宴", "外婆的信", "新的开始"
    };

    private static readonly string[] Bodies =
    {
        "老镇长把地契郑重交到你手里。\n“这块田，从今天起，真正回家了。”",
        "傍晚，老宅的院子里亮起了灯。\n老板娘端来热汤，铁匠提着酒，渔夫拎着刚捕的鱼，\n全镇的人都来了，像很多年前一样热闹。",
        "灯下，你又读了外婆留下的最后一封信：\n“我从没指望你还清什么，\n我只是想给你留一个，随时能回来的地方。”",
        "田舍小镇的故事，还在继续。\n从明天起，是属于你的、自由的田园日子。\n——后日谈，开始。"
    };

    private static EndingPlayer instance;

    private GameObject canvasObject;
    private TextMeshProUGUI titleLabel;
    private TextMeshProUGUI bodyLabel;
    private TMP_FontAsset font;
    private int page;
    private bool playing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (instance != null) return;
        GameObject go = new GameObject("EndingPlayer");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<EndingPlayer>();
    }

    private void OnEnable()
    {
        EventHandler.StoryEndingEvent += Play;
    }

    private void OnDisable()
    {
        EventHandler.StoryEndingEvent -= Play;
    }

    private void Update()
    {
        if (playing && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
        {
            Next();
        }
    }

    public void Play()
    {
        if (playing) return;
        playing = true;
        page = 0;
        Build();
        ShowPage();
    }

    private void Build()
    {
        font = UiFont.GetChineseFont();

        canvasObject = new GameObject("EndingCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 960;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(canvasObject);

        GameObject bgO = new GameObject("Background");
        bgO.transform.SetParent(canvasObject.transform, false);
        Image bg = bgO.AddComponent<Image>();
        bg.color = new Color(0.07f, 0.06f, 0.05f, 1f);
        RectTransform br = bg.rectTransform;
        br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one;
        br.offsetMin = Vector2.zero; br.offsetMax = Vector2.zero;

        // full-area invisible next button
        GameObject btnO = new GameObject("NextButton");
        btnO.transform.SetParent(canvasObject.transform, false);
        Image btnImg = btnO.AddComponent<Image>();
        btnImg.color = new Color(1f, 1f, 1f, 0f);
        Button btn = btnO.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(Next);
        RectTransform brt = btnO.GetComponent<RectTransform>();
        brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
        brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;

        titleLabel = CreateLabel(canvasObject.transform, "归乡", 48, TextAlignmentOptions.Center);
        RectTransform trt = titleLabel.rectTransform;
        trt.anchorMin = new Vector2(0.5f, 0.5f);
        trt.anchorMax = new Vector2(0.5f, 0.5f);
        trt.pivot = new Vector2(0.5f, 0.5f);
        trt.sizeDelta = new Vector2(1200f, 80f);
        trt.anchoredPosition = new Vector2(0f, 200f);

        bodyLabel = CreateLabel(canvasObject.transform, string.Empty, 34, TextAlignmentOptions.Center);
        RectTransform btr = bodyLabel.rectTransform;
        btr.anchorMin = new Vector2(0.5f, 0.5f);
        btr.anchorMax = new Vector2(0.5f, 0.5f);
        btr.pivot = new Vector2(0.5f, 0.5f);
        btr.sizeDelta = new Vector2(1300f, 380f);
        btr.anchoredPosition = Vector2.zero;

        TextMeshProUGUI hint = CreateLabel(canvasObject.transform, "点击任意处继续（空格也可以）", 26, TextAlignmentOptions.Center);
        RectTransform hrt = hint.rectTransform;
        hrt.anchorMin = new Vector2(0.5f, 0f);
        hrt.anchorMax = new Vector2(0.5f, 0f);
        hrt.pivot = new Vector2(0.5f, 0f);
        hrt.sizeDelta = new Vector2(900f, 50f);
        hrt.anchoredPosition = new Vector2(0f, 120f);
        hint.color = new Color(0.72f, 0.68f, 0.6f, 1f);
    }

    private TextMeshProUGUI CreateLabel(Transform parent, string text, int size, TextAlignmentOptions alignment)
    {
        GameObject o = new GameObject("Label");
        o.transform.SetParent(parent, false);
        TextMeshProUGUI label = o.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.alignment = alignment;
        label.fontSize = size;
        label.color = new Color(0.97f, 0.92f, 0.83f, 1f);
        label.raycastTarget = false;
        if (font != null) label.font = font;
        return label;
    }

    private void ShowPage()
    {
        if (titleLabel != null) titleLabel.text = Titles[page];
        if (bodyLabel != null) bodyLabel.text = Bodies[page];
    }

    private void Next()
    {
        if (!playing) return;
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
        if (canvasObject != null)
        {
            Destroy(canvasObject);
            canvasObject = null;
        }
    }
}
