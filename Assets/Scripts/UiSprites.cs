using System.Collections.Generic;
using UnityEngine;

public static class UiSprites
{
    private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    public static Sprite Get(string name)
    {
        return Load(name, 0f);
    }

    public static Sprite GetSliced(string name, float border)
    {
        return Load(name, border);
    }

    private static Sprite Load(string name, float border)
    {
        string key = name + "|" + border;
        Sprite cached;

        if (cache.TryGetValue(key, out cached))
        {
            return cached;
        }

        Texture2D texture = Resources.Load<Texture2D>("UITheme/" + name);

        if (texture == null)
        {
            cache[key] = null;
            return null;
        }

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Vector4 borders = border > 0f
            ? new Vector4(border, border, border, border)
            : Vector4.zero;

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            borders);

        cache[key] = sprite;

        return sprite;
    }
}