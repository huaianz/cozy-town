using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MapData_SO", menuName = "Map/MapData")]
public class MapData_SO : ScriptableObject
{
    public List<TileProperty> tileProperties;
    //用于存储地图中每个瓦片的属性
}
//存储地图数据
