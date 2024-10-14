using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TilePaletteUI : MonoBehaviour
{
    public Transform paletteContainer;
    public GameObject paletteItemPrefab;
    public TileData[] tileDatas;
    public TilePlacer tilePlacer;

    void Start()
    {
        foreach (TileData data in tileDatas)
        {
            GameObject item = Instantiate(paletteItemPrefab, paletteContainer);
            item.GetComponent<Image>().sprite = data.tileSprite;
            item.GetComponent<Button>().onClick.AddListener(() => OnTileSelected(data));
        }
    }

    void OnTileSelected(TileData data)
    {
        tilePlacer.currentTileData = data;
    }
}
