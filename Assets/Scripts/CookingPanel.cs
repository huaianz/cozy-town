using System.Collections.Generic;
using Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CookingPanel : MonoBehaviour
{
    private static CookingPanel instance;

    private GameObject canvasObject;
    private TextMeshProUGUI infoLabel;
    private TMP_FontAsset font;
    private bool built;
    private ScrollRect scrollRect;
    private RectTransform contentRect;
    private bool isOpen;

    public static void Toggle()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("CookingPanel");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<CookingPanel>();
        }

        instance.SetOpen(!instance.isOpen);
    }

    private void SetOpen(bool open)
    {
        isOpen = open;

        if (!built)
        {
            if (!open)
            {
                return;
            }

            Build();
        }

        canvasObject.SetActive(open);

        if (open)
        {
            Refresh();
        }
    }

    private void Build()
    {
        built = true;
        font = UiFont.GetChineseFont();

        canvasObject = new GameObject("CookingCanvas");

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 870;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(canvasObject);

        GameObject dimObject = new GameObject("Dim");
        dimObject.transform.SetParent(canvasObject.transform, false);

        Image dim = dimObject.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.35f);

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
            panel.color = new Color(0.12f, 0.09f, 0.06f, 0.96f);
        }

        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(1240f, 740f);
        panelRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI title = CreateLabel(panelObject.transform, "厨房", 40, TextAlignmentOptions.Center);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(40f, 0f);
        titleRect.offsetMax = new Vector2(-40f, 0f);
        titleRect.sizeDelta = new Vector2(-80f, 60f);
        titleRect.anchoredPosition = new Vector2(0f, -22f);

        GameObject scrollObject = new GameObject("Scroll");
        scrollObject.transform.SetParent(panelObject.transform, false);

        scrollRect = scrollObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 30f;

        RectTransform scrollArea = scrollObject.GetComponent<RectTransform>();
        scrollArea.anchorMin = new Vector2(0f, 0f);
        scrollArea.anchorMax = new Vector2(1f, 1f);
        scrollArea.offsetMin = new Vector2(48f, 215f);
        scrollArea.offsetMax = new Vector2(-48f, -100f);

        GameObject viewportObject = new GameObject("Viewport");
        viewportObject.transform.SetParent(scrollObject.transform, false);

        RectTransform viewportRect = viewportObject.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        Image dragImage = viewportObject.AddComponent<Image>();
        dragImage.color = new Color(0f, 0f, 0f, 0.001f);
        dragImage.raycastTarget = true;

        viewportObject.AddComponent<RectMask2D>();

        GameObject contentObject = new GameObject("Content");
        contentObject.transform.SetParent(viewportObject.transform, false);

        contentRect = contentObject.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 400f);

        infoLabel = CreateLabel(contentObject.transform, string.Empty, 30, TextAlignmentOptions.TopLeft);
        RectTransform infoRect = infoLabel.rectTransform;
        infoRect.anchorMin = new Vector2(0f, 1f);
        infoRect.anchorMax = new Vector2(1f, 1f);
        infoRect.pivot = new Vector2(0.5f, 1f);
        infoRect.sizeDelta = new Vector2(0f, 400f);
        infoRect.anchoredPosition = Vector2.zero;
        infoLabel.lineSpacing = 10f;
        infoLabel.enableWordWrapping = true;

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;

        List<CookingManager.Recipe> recipes = CookingManager.GetRecipes();

        for (int i = 0; i < recipes.Count; i++)
        {
            CookingManager.Recipe recipe = recipes[i];
            int column = i % 3;
            int row = i / 3;
            float x = -300f + column * 300f;
            float y = 130f - row * 80f;

            CreateButton(panelObject.transform, "制作" + recipe.name, new Vector2(x, y), () => Cook(recipe));
        }


        CreateButton(panelObject.transform, "关闭", new Vector2(0f, -40f), Close);
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

    private void CreateButton(Transform parent, string text, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject("Button");
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.AddComponent<Image>();

        Sprite buttonSprite = UiSprites.GetSliced("button_wood", 16f);

        if (buttonSprite != null)
        {
            image.sprite = buttonSprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }
        else
        {
            image.color = new Color(0f, 0f, 0f, 0.45f);
        }

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(214f, 64f);
        rect.anchoredPosition = position;

        TextMeshProUGUI label = CreateLabel(buttonObject.transform, text, 22, TextAlignmentOptions.Center);
        RectTransform textRect = label.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    private void Cook(CookingManager.Recipe recipe)
    {
        if (!CookingManager.Cook(recipe))
        {
            EventHandler.CallFarmEventEvent("厨房", "材料不够，做不了" + recipe.name, true);
            return;
        }

        EventHandler.CallFarmEventEvent("厨房", "做好了" + recipe.name + "，拿去卖钱吧", false);
        Refresh();
    }

    private void OpenUpgrade()
    {
        SetOpen(false);
        ToolUpgradePanel.Toggle();
    }

    private void CreateArrow(Transform parent, string text, Vector2 position, float direction)
    {
        GameObject buttonObject = new GameObject("Arrow" + text);
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.AddComponent<Image>();
        Sprite buttonSprite = UiSprites.GetSliced("button_wood", 16f);

        if (buttonSprite != null)
        {
            image.sprite = buttonSprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }
        else
        {
            image.color = new Color(0f, 0f, 0f, 0.45f);
        }

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => ScrollPage(direction));

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(72f, 72f);
        rect.anchoredPosition = position;

        TextMeshProUGUI label = CreateLabel(buttonObject.transform, text, 22, TextAlignmentOptions.Center);
        RectTransform textRect = label.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    private void ScrollPage(float direction)
    {
        if (scrollRect == null)
        {
            return;
        }

        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition + direction);
    }

    private void Close()
    {
        SetOpen(false);
    }

    private void Refresh()
    {
        if (infoLabel == null)
        {
            return;
        }

        List<CookingManager.Recipe> recipes = CookingManager.GetRecipes();
        string text = string.Empty;

        for (int i = 0; i < recipes.Count; i++)
        {
            CookingManager.Recipe recipe = recipes[i];

            text += recipe.name + "：" + CookingManager.GetIngredientText(recipe);

            if (InventoryManager.Instance != null)
            {
                Item result = InventoryManager.Instance.GetItem(recipe.resultItemID);
                int price = result != null ? Mathf.RoundToInt(result.itemPrice * result.sellPercentage) : 0;

                text += "　卖价 " + price;
            }

            text += CookingManager.CanCook(recipe) ? "　（可制作）" : "　（材料不足）";
            text += "\n\n";
        }

        infoLabel.text = text;

        if (contentRect != null)
        {
            float height = Mathf.Max(infoLabel.preferredHeight + 24f, 300f);
            contentRect.sizeDelta = new Vector2(0f, height);
            infoLabel.rectTransform.sizeDelta = new Vector2(0f, height);
        }
    }
}