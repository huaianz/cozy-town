using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Inventory;
using DG.Tweening;  
using TMPro;

public class TimeUI : MonoBehaviour
{
    public RectTransform dayNightImage;//显示昼夜变化的图像
    public RectTransform clockParent;//时钟格子
    public Image seasonImage;//季节图像
    public TextMeshProUGUI dateText;//日期文本
    public TextMeshProUGUI timeText;//时间文本

    public Sprite[] seasonSprites;//季节图像数组
    public TextMeshProUGUI staminaText;

    public Image weatherImage;

    public Sprite[] weatherSprites;

    public TextMeshProUGUI orderText;

    private List<GameObject> clockBlocks = new List<GameObject>();//时钟块列表
    private string lastTimeText;
    private string lastDateText;
    private int lastSeasonIndex = -1;
    private int lastHourBlockIndex = -1;
    private Tween dayNightTween;

    private void Awake()
    {
        for (int i = 0; i < clockParent.childCount; i++)
        {
            clockBlocks.Add(clockParent.GetChild(i).gameObject);
            //遍历时钟格子所有图像，然后把它添加到时钟块列表里
            clockParent.GetChild(i).gameObject.SetActive(false);
            //让它们不显示，以便在需要时动态显示
        }
    }

    private void OnEnable()
    {
        EventHandler.GameMinuteEvent += OnGameMinuteEvent;
        EventHandler.GameDateEvent += OnGameDateEvent;
        EventHandler.StaminaChangedEvent += OnStaminaChanged;
        EventHandler.WeatherChangedEvent += OnWeatherChangedEvent;
        EventHandler.CommissionChangedEvent += OnCommissionChangedEvent;
        //订阅时间
    }

    private void OnDisable()
    {
        EventHandler.GameMinuteEvent -= OnGameMinuteEvent;
        EventHandler.GameDateEvent -= OnGameDateEvent;
        EventHandler.StaminaChangedEvent -= OnStaminaChanged;
        EventHandler.WeatherChangedEvent -= OnWeatherChangedEvent;
        EventHandler.CommissionChangedEvent -= OnCommissionChangedEvent;
    }
    private void OnCommissionChangedEvent()
    {
        if (orderText == null || ShopManager.Instance == null)
        {
            return;
        }

        List<CommissionData> commissions = ShopManager.Instance.GetCommissions();

        if (commissions == null || commissions.Count == 0)
        {
            orderText.text = string.Empty;
            return;
        }

        string text = "委托：";

        for (int i = 0; i < commissions.Count; i++)
        {
            CommissionData commission = commissions[i];

            if (i > 0)
            {
                text += "　";
            }

            Item item = InventoryManager.Instance != null ? InventoryManager.Instance.GetItem(commission.itemID) : null;
            string itemName = item != null ? item.itemName : commission.itemID.ToString();

            text += itemName + "×" + commission.amount + "（+" + commission.reward + "）";

            if (commission.delivered)
            {
                text += "（已交付）";
            }
            else
            {
                text += "剩" + commission.daysLeft + "天";
            }
        }

        if (ShopManager.Instance.HasFulfillableCommission())
        {
            text += "　按 E 交付";
        }

        orderText.text = text;
    }

    private void OnWeatherChangedEvent(WeatherType weather)
    {
        if (weatherImage == null || weatherSprites == null)
        {
            return;
        }

        int index = (int)weather;

        if (index < 0 || index >= weatherSprites.Length)
        {
            return;
        }

        weatherImage.sprite = weatherSprites[index];
    }

    private void OnStaminaChanged(int current, int max)
    {
        if (staminaText == null)
        {
            return;
        }

        staminaText.text = "体力 " + current + "/" + max;
    }

    private void OnGameMinuteEvent(int minute, int hour)
    {
        string text = hour.ToString("00") + ":" + minute.ToString("00");

        if (text == lastTimeText)
        {
            return;
        }

        lastTimeText = text;
        timeText.text = text;
        //当订阅的事件被触发以后，更新时间文本
    }
    private void OnGameDateEvent(int hour, int day, int month, int year, Season season)
    {
        string text = year + "年" + month.ToString("00") + "月" + day.ToString("00") + "日";

        if (text != lastDateText)
        {
            lastDateText = text;
            dateText.text = text;
        }

        int seasonIndex = (int)season;

        if (seasonIndex != lastSeasonIndex)
        {
            lastSeasonIndex = seasonIndex;
            seasonImage.sprite = seasonSprites[seasonIndex];
        }
        //更新季节图像

        SwitchHourImage(hour);
        DayNightImageRotate(hour);
        //更新时钟块和昼夜变化图像
    }
    /// <summary>
    /// 根据小时切换时间块显示
    /// </summary>
    /// <param name="hour"></param>
    private void SwitchHourImage(int hour)
    {
        int index = hour / 4;

        if (index == lastHourBlockIndex)
        {
            return;
        }

        lastHourBlockIndex = index;

        if (index == 0)
        {
            foreach (var item in clockBlocks)
            {
                item.SetActive(false);
            }
        }//index=0，则隐藏所有时钟块
        else
        {
            for (int i = 0; i < clockBlocks.Count; i++)
            {
                if (i < index + 1)
                    clockBlocks[i].SetActive(true);
                else
                    clockBlocks[i].SetActive(false);
            }//显示相应的时钟块
        }
    }

    private void DayNightImageRotate(int hour)
    {
        var target = new Vector3(0, 0, hour * 15 - 90);

        if (dayNightTween != null)
        {
            dayNightTween.Kill();
        }

        dayNightTween = dayNightImage.DORotate(target, 1f, RotateMode.Fast);
    }//根据当前小时数，计算昼夜变化图像的目标旋转角度
}
//管理游戏中的时间 UI，确保玩家能够实时了解当前的时间和季节信息