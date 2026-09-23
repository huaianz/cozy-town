public static class FarmEventEffects
{
    public static float growthBonus;
    public static int harvestBonus;
    public static bool growthFrozen;

    public static void ResetDaily()
    {
        growthBonus = 0f;
        harvestBonus = 0;
        growthFrozen = false;
    }
}