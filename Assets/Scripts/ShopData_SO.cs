using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ShopData_SO", menuName = "Inventory/ShopData_SO")]
public class ShopData_SO : ScriptableObject
{
    [Header("商店商品列表")]
    public List<ShopGoods> shopGoodsList = new List<ShopGoods>();

}
