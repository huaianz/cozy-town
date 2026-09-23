using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Settings : MonoBehaviour
{
    //时间相关
    public const float secondThreshold = 0.01f; // 数值越小时间越快
    public const int secondHold = 59;
    public const int minuteHold = 59;
    public const int hourHold = 23;
    public const int dayHold = 30;
    public const int seasonHold = 3;
    public const int monthHold = 12;

    public const int dryCropGrowthPerDay = 1;
    public const int wateredCropGrowthPerDay = 2;
    public const float wateredCropGrowthBonus = 0.5f;
}
//游戏中各种常量设置