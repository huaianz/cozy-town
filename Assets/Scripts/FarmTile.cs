using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FarmTile : MonoBehaviour
{
    [Header("地块类型")]
    public Sprite grasssorite;
    public Sprite emptysprite;
    public Sprite tilledSprite;
    public Sprite wateredSprite;

    [Header("高亮设置")]
    public Color highlightColor = Color.green;
    public Color invalidColor = Color.red;
}
