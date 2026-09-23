using UnityEngine;

public static class WeatherEffects
{
    public static string GetName(WeatherType weather)
    {
        switch (weather)
        {
            case WeatherType.Sunny:
                return "晴天";
            case WeatherType.Cloudy:
                return "多云";
            case WeatherType.Overcast:
                return "阴天";
            case WeatherType.LightRain:
                return "小雨";
            case WeatherType.Rainy:
                return "中雨";
            case WeatherType.HeavyRain:
                return "大雨";
            case WeatherType.Thunderstorm:
                return "雷暴";
            case WeatherType.Mist:
                return "薄雾";
            case WeatherType.DenseFog:
                return "浓雾";
            case WeatherType.LightSnow:
                return "小雪";
            case WeatherType.HeavySnow:
                return "大雪";
            default:
                return "晴天";
        }
    }

    public static string GetSeasonName(Season season)
    {
        switch (season)
        {
            case Season.夏天:
                return "夏天";

            case Season.秋天:
                return "秋天";

            case Season.冬天:
                return "冬天";

            default:
                return "春天";
        }
    }

    public static float GetGrowthMultiplier(WeatherType weather, Season season)
    {
        switch (weather)
        {
            case WeatherType.LightRain:
                return 1.15f;
            case WeatherType.Rainy:
                return 1.1f;
            case WeatherType.HeavyRain:
            case WeatherType.Thunderstorm:
                return 1.05f;
            case WeatherType.Mist:
                return 1.08f;
            case WeatherType.DenseFog:
                return 0.95f;
            case WeatherType.Overcast:
                return 0.98f;
            case WeatherType.LightSnow:
            case WeatherType.HeavySnow:
                return 0f;
            case WeatherType.Sunny:
                return season == Season.秋天 ? 1.08f : 1f;
            default:
                return 1f;
        }
    }

    public static float GetAutoWaterChance(WeatherType weather)
    {
        switch (weather)
        {
            case WeatherType.Rainy:
            case WeatherType.HeavyRain:
            case WeatherType.Thunderstorm:
                return 1f;
            case WeatherType.LightRain:
                return 0.7f;
            case WeatherType.Mist:
                return 0.4f;
            case WeatherType.Cloudy:
                return 0.35f;
            case WeatherType.Overcast:
            case WeatherType.DenseFog:
                return 0.2f;
            case WeatherType.LightSnow:
            case WeatherType.HeavySnow:
                return 0.3f;
            default:
                return 0f;
        }
    }

    public static float GetStrikeChance(WeatherType weather, Season season)
    {
        if (weather != WeatherType.Thunderstorm)
        {
            return 0f;
        }

        return season == Season.夏天 ? 0.25f : 0.15f;
    }


    public static bool IsRainy(WeatherType weather)
    {
        return weather == WeatherType.LightRain
            || weather == WeatherType.Rainy
            || weather == WeatherType.HeavyRain
            || weather == WeatherType.Thunderstorm;
    }

    public static float GetRainIntensity(WeatherType weather)
    {
        switch (weather)
        {
            case WeatherType.LightRain:
                return 0.45f;
            case WeatherType.Rainy:
                return 0.75f;
            case WeatherType.HeavyRain:
                return 1.1f;
            case WeatherType.Thunderstorm:
                return 1.3f;
            default:
                return 0f;
        }
    }

    public static bool IsFoggy(WeatherType weather)
    {
        return weather == WeatherType.Mist || weather == WeatherType.DenseFog;
    }

    public static bool IsSnowy(WeatherType weather)
    {
        return weather == WeatherType.LightSnow || weather == WeatherType.HeavySnow;
    }

    public static Color GetOverlayColor(WeatherType weather)
    {
        switch (weather)
        {
            case WeatherType.LightRain:
                return new Color(0.2f, 0.3f, 0.55f, 0.07f);
            case WeatherType.Rainy:
                return new Color(0.2f, 0.3f, 0.55f, 0.12f);
            case WeatherType.HeavyRain:
                return new Color(0.16f, 0.24f, 0.5f, 0.2f);
            case WeatherType.Thunderstorm:
                return new Color(0.1f, 0.14f, 0.34f, 0.26f);
            case WeatherType.Mist:
                return new Color(0.8f, 0.84f, 0.88f, 0.12f);
            case WeatherType.DenseFog:
                return new Color(0.8f, 0.84f, 0.88f, 0.26f);
            case WeatherType.Cloudy:
                return new Color(0.4f, 0.5f, 0.6f, 0.05f);
            case WeatherType.Overcast:
                return new Color(0.35f, 0.4f, 0.5f, 0.1f);
            case WeatherType.LightSnow:
                return new Color(0.85f, 0.9f, 0.98f, 0.1f);
            case WeatherType.HeavySnow:
                return new Color(0.85f, 0.9f, 0.98f, 0.22f);
            default:
                return new Color(0f, 0f, 0f, 0f);
        }
    }

    public static float GetLightScale(WeatherType weather)
    {
        switch (weather)
        {
            case WeatherType.Cloudy:
                return 0.9f;
            case WeatherType.Overcast:
                return 0.82f;
            case WeatherType.LightRain:
                return 0.85f;
            case WeatherType.Rainy:
                return 0.62f;
            case WeatherType.HeavyRain:
                return 0.55f;
            case WeatherType.Thunderstorm:
                return 0.48f;
            case WeatherType.Mist:
                return 0.9f;
            case WeatherType.DenseFog:
                return 0.78f;
            case WeatherType.LightSnow:
                return 0.92f;
            case WeatherType.HeavySnow:
                return 0.8f;
            default:
                return 1f;
        }
    }
}