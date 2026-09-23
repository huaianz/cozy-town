using UnityEngine;

public class TestImport : MonoBehaviour
{
    public TextAsset itemJson;

    public ItemData_SO itemSO;

    void Start()
    {
        JsonToItemSO.LoadJsonIntoSO(itemJson, itemSO);
    }
}