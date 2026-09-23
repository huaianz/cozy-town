using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    private int gameSecond, gameMinute, gameHour, gameDay, gameMonth, gameYear;
    //分别代表游戏中的秒、分钟、小时、天、月、年
    private Season gameSeason = Season.春天;
    //表示当前季节，默认季节是春天
    private int monthInSeaon = 3;
    //每个季节三个月

    public bool gameClockPause;
    //游戏时间是否暂停
    private float tikTime;
    //记录游戏时间流逝的变量

    [Header("睡觉")]
    [SerializeField] private KeyCode sleepKey = KeyCode.R;
    [SerializeField] private int sleepHour = 18;

    private bool sleptToday;

    [Header("天气")]
    private static readonly WeatherType[] springWeatherPool = new WeatherType[]
    {
        WeatherType.Sunny, WeatherType.Sunny, WeatherType.Sunny,
        WeatherType.Cloudy, WeatherType.Cloudy,
        WeatherType.Overcast, WeatherType.Overcast,
        WeatherType.LightRain, WeatherType.LightRain, WeatherType.LightRain,
        WeatherType.Mist, WeatherType.Mist,
        WeatherType.Thunderstorm
    };

    private static readonly WeatherType[] summerWeatherPool = new WeatherType[]
    {
        WeatherType.Sunny, WeatherType.Sunny, WeatherType.Sunny, WeatherType.Sunny,
        WeatherType.Cloudy, WeatherType.Cloudy,
        WeatherType.Overcast,
        WeatherType.HeavyRain, WeatherType.HeavyRain, WeatherType.HeavyRain,
        WeatherType.Thunderstorm, WeatherType.Thunderstorm,
        WeatherType.Mist
    };

    private static readonly WeatherType[] autumnWeatherPool = new WeatherType[]
    {
        WeatherType.Sunny, WeatherType.Sunny, WeatherType.Sunny,
        WeatherType.Cloudy, WeatherType.Cloudy,
        WeatherType.Overcast,
        WeatherType.LightRain, WeatherType.LightRain, WeatherType.LightRain,
        WeatherType.Mist, WeatherType.Mist,
        WeatherType.DenseFog, WeatherType.DenseFog
    };

    private static readonly WeatherType[] winterWeatherPool = new WeatherType[]
    {
        WeatherType.Sunny, WeatherType.Sunny,
        WeatherType.Cloudy, WeatherType.Cloudy,
        WeatherType.Overcast, WeatherType.Overcast,
        WeatherType.LightSnow, WeatherType.LightSnow, WeatherType.LightSnow,
        WeatherType.HeavySnow, WeatherType.HeavySnow,
        WeatherType.DenseFog, WeatherType.DenseFog
    };

    private WeatherType currentWeather = WeatherType.Sunny;

    private void Start()
    {
        //if (SaveLoadManager.Instance != null && SaveLoadManager.Instance.HasSaveFile())
        //{
        //    //让saveLoadManager中的LoadGame()来实现继续游戏
        //}
        //else
        //{
        //    // 否则开始新游戏，初始化默认时间
        //    NewGameTime();
        //    EventHandler.CallGameDateEvent(gameHour, gameDay, gameMonth, gameYear, gameSeason);
        //    EventHandler.CallGameMinuteEvent(gameMinute, gameHour);
        //}
    }
    private void Update()
    {
        if (!gameClockPause)
        {
            tikTime += Time.deltaTime;

            if (tikTime >= Settings.secondThreshold)
            {
                tikTime -= Settings.secondThreshold;
                UpdateGameTime();//更新游戏时间
            }
        }

        if (Input.GetKey(KeyCode.T))//按下T键快速增加时间
        {
            for (int i = 0; i < 60; i++)
            {
                UpdateGameTime();
            }
        }

        if (Input.GetKeyDown(KeyCode.G))//按下G键增加一天，并通知其他系统
        {
            NextDay();
            EventHandler.CallGameDateEvent(gameHour, gameDay, gameMonth, gameYear, gameSeason);
        }

        if (Input.GetKeyDown(sleepKey))
        {
            SleepToNextDay();
        }
    }

    //时间初始化设置
    public void NewGameTime()
    {
        gameSecond = 0;
        gameMinute = 0;
        gameHour = 7;
        gameDay = 20;
        gameMonth = 5;
        gameYear = 2026;
        gameSeason = SeasonFromMonth(gameMonth);

        SyncMonthInSeason();

        currentWeather = WeatherType.Sunny;
        EventHandler.CallWeatherChangedEvent(currentWeather);
    }
    private void UpdateGameTime()
    {
        gameSecond++;
        if (gameSecond > Settings.secondHold)
        {
            gameMinute++;
            gameSecond = 0;

            if (gameMinute > Settings.minuteHold)
            {
                gameHour++;
                gameMinute = 0;
                if (gameHour > Settings.hourHold)
                {
                    gameHour = 0;
                    NextDay();
                }
                EventHandler.CallGameDateEvent(gameHour, gameDay, gameMonth, gameYear, gameSeason);
            }
            EventHandler.CallGameMinuteEvent(gameMinute, gameHour);
        }

    }

    private void NextDay()
    {
        gameDay++;

        if (gameDay > Settings.dayHold)
        {
            gameDay = 1;
            gameMonth++;

            if (gameMonth > Settings.monthHold)
            {
                gameMonth = 1;
            }

            monthInSeaon--;

            if (monthInSeaon == 0)
            {
                monthInSeaon = 3;

                int seasonNumber = (int)gameSeason;
                seasonNumber++;

                if (seasonNumber > Settings.seasonHold)
                {
                    seasonNumber = 0;
                    gameYear++;
                }

                gameSeason = (Season)seasonNumber;

                if (gameYear > 9999)
                {
                    gameYear = 2022;
                }
            }
        }

        sleptToday = false;

        EventHandler.CallGameDayEvent(gameDay, gameSeason);
        MarketDay.Announce(gameDay);

        currentWeather = RollWeather();
        EventHandler.CallWeatherChangedEvent(currentWeather);
    }

    public void SleepToNextDay()
    {
        if (sleptToday)
        {
            EventHandler.CallFarmEventEvent("睡觉", "今天已经睡过了，等明天再休息吧", true);
            return;
        }

        if (gameHour < sleepHour)
        {
            PlayerController player = FindObjectOfType<PlayerController>();

            if (player == null || player.CurrentStamina > 0)
            {
                EventHandler.CallFarmEventEvent("睡觉", "天还早，到了 " + sleepHour + " 点以后再睡（体力用完可以提前休息）", true);
                return;
            }

            EventHandler.CallFarmEventEvent("睡觉", "太累了，提前休息到第二天", true);
        }

        sleptToday = true;

        NextDay();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(SfxType.Sleep);
        }

        gameHour = 6;
        gameMinute = 0;
        gameSecond = 0;
        tikTime = 0f;

        EventHandler.CallGameDateEvent(gameHour, gameDay, gameMonth, gameYear, gameSeason);
        EventHandler.CallGameMinuteEvent(gameMinute, gameHour);

        if (SaveLoadManager.Instance != null)
        {
            SaveLoadManager.Instance.SaveGame();
        }
    }

    public WeatherType GetWeather()
    {
        return currentWeather;
    }

    public void SetWeather(WeatherType weather)
    {
        currentWeather = weather;
        EventHandler.CallWeatherChangedEvent(currentWeather);
    }

    public Season GetSeason()
    {
        return gameSeason;
    }

    public static Season SeasonFromMonth(int month)
    {
        int index = ((month - 1) % 12 + 12) % 12 / 3;
        return (Season)index;
    }

    public void SetSeason(Season season)
    {
        gameSeason = season;
        gameMonth = 1 + (int)season * 3;
        monthInSeaon = 3;

        if (CropManager.Instance != null)
        {
            CropManager.Instance.SetSeason(season);
        }

        EventHandler.CallGameDateEvent(gameHour, gameDay, gameMonth, gameYear, gameSeason);

        currentWeather = RollWeather(season);
        EventHandler.CallWeatherChangedEvent(currentWeather);
    }

    private WeatherType RollWeather()
    {
        return RollWeather(gameSeason);
    }

    public WeatherType RollWeather(Season season)
    {
        WeatherType[] pool = GetSeasonWeatherPool(season);

        if (pool == null || pool.Length == 0)
        {
            return WeatherType.Sunny;
        }

        return pool[Random.Range(0, pool.Length)];
    }

    private static WeatherType[] GetSeasonWeatherPool(Season season)
    {
        switch (season)
        {
            case Season.夏天:
                return summerWeatherPool;

            case Season.秋天:
                return autumnWeatherPool;

            case Season.冬天:
                return winterWeatherPool;

            default:
                return springWeatherPool;
        }
    }

    /// <summary>
    /// 获取当前时间数据
    /// </summary>
    public void GetTimeData(out int second, out int minute, out int hour,
                           out int day, out int month, out int year, out Season season)
    {
        second = gameSecond;
        minute = gameMinute;
        hour = gameHour;
        day = gameDay;
        month = gameMonth;
        year = gameYear;
        season = gameSeason;
    }

    /// <summary>
    /// 设置时间数据
    /// </summary>
    public void SetTimeData(int second, int minute, int hour,
                           int day, int month, int year, Season season)
    {
        gameSecond = second;
        gameMinute = minute;
        gameHour = hour;
        gameDay = day;
        gameMonth = month;
        gameYear = year;
        gameSeason = season;
        //按月份重新推导"本季还剩几个月"
        SyncMonthInSeason();
        // 触发更新事件
        EventHandler.CallGameDateEvent(hour, day, month, year, season);
        EventHandler.CallGameMinuteEvent(minute, hour);
    }

    /// <summary>
    /// 按月份重新推导"本季还剩几个月"，让月份和季节始终保持一致
    /// 约定：1-3月春、4-6月夏、7-9月秋、10-12月冬
    /// </summary>
    private void SyncMonthInSeason()
    {
        monthInSeaon = 3 - (gameMonth - 1) % 3;
    }
}


//管理游戏中的时间系统，并确保其他系统能够及时响应时间的变化
