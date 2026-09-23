#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class AreaBoundsGuide
{
    private const bool Enabled = true;

    static AreaBoundsGuide()
    {
        SceneView.duringSceneGui -= OnSceneGui;
        SceneView.duringSceneGui += OnSceneGui;
    }

    private static void OnSceneGui(SceneView view)
    {
        if (!Enabled)
        {
            return;
        }

        DrawArea("果园 40x24", new Vector2(60f, 0f), new Vector2(40f, 24f), new Color(0.35f, 0.95f, 0.45f, 0.9f));
        DrawArea("矿洞 20x12", new Vector2(-60f, 0f), new Vector2(20f, 12f), new Color(0.95f, 0.65f, 0.3f, 0.9f));
        DrawArea("鱼塘 22x14", new Vector2(0f, -52f), new Vector2(22f, 14f), new Color(0.35f, 0.75f, 0.98f, 0.9f));
    }

    private static void DrawArea(string label, Vector2 center, Vector2 size, Color color)
    {
        float halfX = size.x * 0.5f;
        float halfY = size.y * 0.5f;

        Vector3 min = new Vector3(center.x - halfX, center.y - halfY, 0f);
        Vector3 max = new Vector3(center.x + halfX, center.y + halfY, 0f);

        Handles.color = color;
        Handles.DrawWireCube((min + max) * 0.5f, new Vector3(size.x, size.y, 0f));

        Handles.color = new Color(color.r, color.g, color.b, 0.25f);

        for (float x = min.x + 1f; x < max.x; x += 1f)
        {
            Handles.DrawLine(new Vector3(x, min.y, 0f), new Vector3(x, max.y, 0f));
        }

        for (float y = min.y + 1f; y < max.y; y += 1f)
        {
            Handles.DrawLine(new Vector3(min.x, y, 0f), new Vector3(max.x, y, 0f));
        }

        Handles.color = color;
        Handles.Label(new Vector3(center.x - halfX + 0.5f, max.y + 0.6f, 0f), label + "   中心 (" + center.x + ", " + center.y + ")");
    }
}
#endif