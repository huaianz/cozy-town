using TMPro;
using UnityEngine;

public static class UiFont
{
    private static TMP_FontAsset cached;
    private static bool tried;

    public static TMP_FontAsset GetChineseFont()
    {
        if (tried)
        {
            return cached;
        }

        tried = true;

        Font[] fonts = Resources.LoadAll<Font>("Front");

        if (fonts != null)
        {
            Font source = null;

            for (int i = 0; i < fonts.Length; i++)
            {
                if (fonts[i] != null && fonts[i].name.Contains("hzk"))
                {
                    source = fonts[i];
                    break;
                }
            }

            if (source == null && fonts.Length > 0)
            {
                source = fonts[0];
            }

            if (source != null)
            {
                cached = TMP_FontAsset.CreateFontAsset(source);
            }
        }

        if (cached == null)
        {
            cached = Resources.Load<TMP_FontAsset>("Front/mplus hzk SDF");
        }

        return cached;
    }
}