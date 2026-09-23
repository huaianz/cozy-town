using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LandTileData
{
    public Vector2Int gridPosition;
    public bool isUnlocked;
    public int price;
    public FarmTile farmTile;

    public LandTileData(Vector2Int pos, int price)
    {
        gridPosition = pos;
        isUnlocked = false;
        this.price = price;
        farmTile = null;
    }
}
