#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class TreeRangeGuide
{
    private const float ClickRadius = 3.5f;

    static TreeRangeGuide()
    {
        SceneView.duringSceneGui -= OnSceneGui;
        SceneView.duringSceneGui += OnSceneGui;
    }

    private static void OnSceneGui(SceneView view)
    {
        DrawTag("fruit tree", new Color(0.3f, 1f, 0.45f, 1f));
        DrawTag("Tree", new Color(1f, 0.62f, 0.2f, 1f));
    }

    private static void DrawTag(string tag, Color color)
    {
        GameObject[] objects;

        try
        {
            objects = GameObject.FindGameObjectsWithTag(tag);
        }
        catch
        {
            return;
        }

        for (int i = 0; i < objects.Length; i++)
        {
            GameObject go = objects[i];

            if (go == null)
            {
                continue;
            }

            Vector3 anchor = go.transform.position;

            Handles.color = color;
            Handles.DrawWireDisc(anchor, Vector3.forward, ClickRadius);
            Handles.DrawWireDisc(anchor, Vector3.forward, 0.5f);
            Handles.Label(
                anchor + Vector3.up * 0.8f,
                go.name + "  锚点(" + Mathf.FloorToInt(anchor.x) + "," + Mathf.FloorToInt(anchor.y) + ")");

            SpriteRenderer renderer = go.GetComponentInChildren<SpriteRenderer>();

            if (renderer != null)
            {
                Handles.color = new Color(1f, 1f, 1f, 0.55f);
                Handles.DrawWireCube(renderer.bounds.center, renderer.bounds.size);
            }
        }
    }
}
#endif