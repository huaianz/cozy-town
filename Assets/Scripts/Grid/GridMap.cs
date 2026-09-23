using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Tilemaps;

[ExecuteInEditMode]
//这是一个特性，使得 GridMap 类中的代码在 Unity 编辑器中运行，即使游戏没有启动
public class GridMap : MonoBehaviour
{
    //地图数据
    public MapData_SO mapData;
    public GridType gridType;
    public Tilemap currentTilemap;

    private void OnEnable()
    {
        if(!Application.IsPlaying(this))
        {
            currentTilemap = GetComponent<Tilemap>();

            if (mapData != null)
            {
                //mapData.tileProperties是一个List<TileProperty> 类型的列表，存储了地图中每个瓦片的属性，将它清空，使其成为一个空列表
                mapData.tileProperties.Clear();
            }
                
        }
    }



    private void OnDisable()
    {
        if (!Application.IsPlaying(this))
        {
            currentTilemap = GetComponent<Tilemap>();
            //更新地图瓦片数据
            UpdateTileProperties();
            //只在编辑器里生效
#if UNITY_EDITOR
            if (mapData != null)
                EditorUtility.SetDirty(mapData);//标记数据为已修改，让 Unity 编辑器自动保存改动
#endif
        }
    }


    private void UpdateTileProperties()
    {
        //压缩瓦片地图的边界，让 Tilemap 只包裹真正有瓦片的区域，删掉空白多余的范围
        currentTilemap.CompressBounds();

        if (!Application.IsPlaying(this))//如果当前是编辑模式
        {
            if (mapData != null)
            {
                //获取已绘制瓦片范围的左下角
                Vector3Int startPos=currentTilemap.cellBounds.min;
                //获取右上角
                Vector3Int endPos=currentTilemap.cellBounds.max;

                for(int x=startPos.x;x<endPos.x;x++)
                {
                    for(int y=startPos.y;y<endPos.y;y++)
                    {
                        //遍历获取所有瓦片，获得它们的的位置
                        TileBase tile=currentTilemap.GetTile(new Vector3Int(x,y,0));

                        if(tile != null)
                        {
                            TileProperty newTile = new TileProperty
                            {
                                tileCoordinate = new Vector2Int(x, y),
                                gridType = this.gridType,
                                isUnlocked = true,

                                // 如果当前Tilemap是障碍物层，自动标记hasObstacle
                                hasObstacle = this.gridType == GridType.Obstacle,
                                // 自动根据瓦片类型设置障碍物类型
                                obstacleType = GetObstacleTypeByTile(tile)
                            };

                            mapData.tileProperties.Add(newTile);
                        }
                    }
                }
            }
        }
    }
    private ObstacleType GetObstacleTypeByTile(TileBase tile)
    {
        // 这里可以根据瓦片名字自动判断
        string tileName = tile.name.ToLower();
        if (tileName.Contains("tree") || tileName.Contains("树"))
            return ObstacleType.Tree;
        if (tileName.Contains("rock") || tileName.Contains("stone") || tileName.Contains("石头"))
            return ObstacleType.Rock;
        if (tileName.Contains("weed") || tileName.Contains("grass") || tileName.Contains("草"))
            return ObstacleType.Weed;

        return ObstacleType.Rock; // 默认石头，你可以自己改
    }
}

//在编辑器中动态更新地图数据，方便地图的管理和维护
