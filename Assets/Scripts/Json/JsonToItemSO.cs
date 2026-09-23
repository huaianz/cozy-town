using UnityEngine;
using System.Collections.Generic;

// 用来包裹Json数组的辅助类，把json文件写入ScriptableObject
[System.Serializable]
public class ItemListWrapper
{
    public List<Item> items;
}

public static class JsonToItemSO
{
    public static void LoadJsonIntoSO(TextAsset jsonFile, ItemData_SO so)
    {
        // 解析Json
        ItemListWrapper wrapper = JsonUtility.FromJson<ItemListWrapper>(jsonFile.text);

        // 覆盖到SO
        so.Items = wrapper.items;

    }
}