using System.Collections.Generic;
using Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SleepReportPanel : MonoBehaviour
{
    private static SleepReportPanel instance;

    private GameObject canvasObject;
    private TextMeshProUGUI titleLabel;
    private TextMeshProUGUI bodyLabel;
    private TMP_FontAsset font;
    private bool built;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        Ensure();
    }

    private static void Ensure()
    {
        if (instance != null)
        {
            return;
        }

        GameObject go = new GameObject("SleepReportPanel");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<SleepReportPanel>();
    }

    private void OnEnable()
    {
        EventHandler.GameDayEvent += OnGameDay;
    }

    private void OnDisable()
    {
        EventHandler.GameDayEvent -= OnGameDay;
    }

    private void OnGameDay(int day, Season season)
    {
        if (FindObjectOfType<GridMapMangaer>() == null)
        {
            return;
        }

        if (!built)
        {
            Build();
        }

        titleLabel.text = "第 " + day + " 天 · 早上好";
        bodyLabel.text = BuildBody(day, season);

        SetOpen(true);
    }

    private string BuildBody(int day, Season season)
    {
        string body = "— 昨夜 —\n";

        int planted = 0;
        int watered = 0;
        int mature = 0;

        GridMapMangaer map = FindObjectOfType<GridMapMangaer>();

        if (map != null)
        {
            map.GetFarmSummary(out planted, out watered, out mature);
        }

        body += "田里 " + planted + " 株作物";

        if (mature > 0)
        {
            body += "，其中 " + mature + " 株已经成熟";
        }

        body += "\n";

        if (watered > 0)
        {
            body += watered + " 块地还是湿的\n";
        }

        int matureFish = 0;

        if (PondManager.Instance != null)
        {
            matureFish = PondManager.Instance.CountMature();
            body += "鱼塘 " + PondManager.Instance.FishCount + " 尾鱼";

            if (matureFish > 0)
            {
                body += "，成熟 " + matureFish + " 尾";
            }

            body += "\n";
        }

        int veins = 0;

        if (MineManager.Instance != null)
        {
            veins = MineManager.Instance.ActiveVeins;
            body += "矿洞还有 " + veins + " 处矿脉\n";
        }

        if (MarketDay.IsMarketDay(day))
        {
            body += "今天是集市日，卖价 x1.5\n";
        }
        else if (MarketDay.IsTomorrowMarket(day))
        {
            body += "明天是集市日，可以先把货囤着\n";
        }
        else
        {
            body += "集市还有 " + (7 - day % 7) + " 天\n";
        }

        List<CommissionData> commissions = ShopManager.Instance != null ? ShopManager.Instance.GetCommissions() : null;

        if (commissions != null && commissions.Count > 0)
        {
            body += "委托 " + commissions.Count + " 个";

            if (ShopManager.Instance.LastExpiredCommissions > 0)
            {
                body += "（昨夜过期 " + ShopManager.Instance.LastExpiredCommissions + " 个）";
            }

            body += "\n";
        }

        string nightEvent = RollNightEvent();

        if (!string.IsNullOrEmpty(nightEvent))
        {
            body += "\n" + nightEvent + "\n";
        }

        body += "\n— 今天可以做什么 —\n";

        List<string> tips = new List<string>();

        if (mature > 0)
        {
            tips.Add("· 有 " + mature + " 株作物成熟了，去收割");
        }

        if (matureFish > 0)
        {
            tips.Add("· 鱼塘有 " + matureFish + " 尾成熟，可以钓上来卖钱");
        }

        if (veins > 0)
        {
            tips.Add("· 矿洞有 " + veins + " 处矿脉，挖矿能攒工具升级的材料");
        }

        if (commissions != null && commissions.Count > 0)
        {
            int soon = 99;

            for (int i = 0; i < commissions.Count; i++)
            {
                if (!commissions[i].delivered && commissions[i].daysLeft < soon)
                {
                    soon = commissions[i].daysLeft;
                }
            }

            if (soon < 99)
            {
                tips.Add("· 有 " + commissions.Count + " 个委托，最急的还剩 " + soon + " 天");
            }
        }

        if (MarketDay.IsMarketDay(day))
        {
            tips.Add("· 今天是集市日，卖价 x1.5，别按平时价卖");
        }

        if (WaterWellManager.Instance != null && !WaterWellManager.Instance.HasWater)
        {
            tips.Add("· 水壶空了，先去水井打水");
        }

        if (map != null && map.ScarecrowSaves > 0)
        {
            tips.Add("· 昨晚的坏天气被稻草人挡住了 " + map.ScarecrowSaves + " 次");
        }

        if (tips.Count == 0)
        {
            tips.Add("· 浇浇水、去矿洞挖几镐，或者甩两杆钓鱼");
        }

        int shown = Mathf.Min(4, tips.Count);

        for (int i = 0; i < shown; i++)
        {
            body += tips[i] + "\n";
        }

        return body;
    }

    private static string RollNightEvent()
    {
        float roll = UnityEngine.Random.value;

        if (roll < 0.16f)
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.AddItem(29, 2);
            }

            return "夜里有旅人问路，临走塞给你 纤维x2。";
        }

        if (roll < 0.3f)
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.AddItem(43, 1);
            }

            return "一颗流星划过，你在田边捡到 幸运果实x1。";
        }

        if (roll < 0.44f)
        {
            int money = ShopManager.Instance != null ? ShopManager.Instance.PlayerMoney : 0;

            if (money >= 120 && ShopManager.Instance != null)
            {
                int loss = Mathf.Max(10, Mathf.RoundToInt(money * 0.05f));

                ShopManager.Instance.PlayerMoney -= loss;
                EventHandler.CallUpdateMoneyEvent(ShopManager.Instance.PlayerMoney);

                return "夜里小偷摸进院子，损失了 " + loss + " 金币。";
            }

            return "夜里院门口有响动，你出去看了看，什么都没丢。";
        }

        if (roll < 0.62f)
        {
            return "猫头鹰蹲在水井边上看了你一会儿，扑棱棱飞走了。";
        }

        if (roll < 0.78f)
        {
            return "睡得挺沉，梦见自己在田里跑来跑去。";
        }

        return null;
    }

    private void SetOpen(bool open)
    {
        if (canvasObject != null)
        {
            canvasObject.SetActive(open);
        }
    }

    private void Build()
    {
        built = true;
        font = UiFont.GetChineseFont();

        canvasObject = new GameObject("SleepReportCanvas");

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 884;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(canvasObject);

        GameObject dimObject = new GameObject("Dim");
        dimObject.transform.SetParent(canvasObject.transform, false);

        Image dim = dimObject.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.45f);

        RectTransform dimRect = dim.rectTransform;
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = Vector2.zero;
        dimRect.offsetMax = Vector2.zero;

        GameObject panelObject = new GameObject("Panel");
        panelObject.transform.SetParent(canvasObject.transform, false);

        Image panel = panelObject.AddComponent<Image>();
        Sprite panelSprite = UiSprites.GetSliced("panel_wood", 24f);

        if (panelSprite != null)
        {
            panel.sprite = panelSprite;
            panel.type = Image.Type.Sliced;
            panel.color = Color.white;
        }
        else
        {
            panel.color = new Color(0.12f, 0.09f, 0.06f, 0.97f);
        }

        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(1160f, 820f);
        panelRect.anchoredPosition = Vector2.zero;

        titleLabel = CreateLabel(panelObject.transform, string.Empty, 44, TextAlignmentOptions.Center);
        RectTransform titleRect = titleLabel.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(44f, 0f);
        titleRect.offsetMax = new Vector2(-44f, 0f);
        titleRect.sizeDelta = new Vector2(-88f, 64f);
        titleRect.anchoredPosition = new Vector2(0f, -26f);

        bodyLabel = CreateLabel(panelObject.transform, string.Empty, 28, TextAlignmentOptions.TopLeft);
        RectTransform bodyRect = bodyLabel.rectTransform;
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.offsetMin = new Vector2(70f, 150f);
        bodyRect.offsetMax = new Vector2(-70f, -110f);
        bodyLabel.lineSpacing = 14f;
        bodyLabel.enableWordWrapping = true;

        GameObject buttonObject = new GameObject("WakeButton");
        buttonObject.transform.SetParent(panelObject.transform, false);

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
            buttonImage.color = new Color(0f, 0f, 0f, 0.45f);
        }

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(() => SetOpen(false));

        RectTransform buttonRect = buttonImage.rectTransform;
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.sizeDelta = new Vector2(300f, 80f);
        buttonRect.anchoredPosition = new Vector2(0f, 40f);

        TextMeshProUGUI buttonLabel = CreateLabel(buttonObject.transform, "起床干活", 32, TextAlignmentOptions.Center);
        RectTransform buttonLabelRect = buttonLabel.rectTransform;
        buttonLabelRect.anchorMin = Vector2.zero;
        buttonLabelRect.anchorMax = Vector2.one;
        buttonLabelRect.offsetMin = Vector2.zero;
        buttonLabelRect.offsetMax = Vector2.zero;

        canvasObject.SetActive(false);
    }

    private TextMeshProUGUI CreateLabel(Transform parent, string text, int size, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject("Label");
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.alignment = alignment;
        label.fontSize = size;
        label.color = new Color(0.97f, 0.92f, 0.82f, 1f);
        label.raycastTarget = false;

        if (font != null)
        {
            label.font = font;
        }

        return label;
    }
}
