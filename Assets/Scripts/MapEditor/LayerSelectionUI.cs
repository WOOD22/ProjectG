using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Tilemaps;

public class LayerSelectionUI : MonoBehaviour
{
    public Transform layerContainer; // 레이어 UI 아이템들이 포함될 부모 컨테이너 (예: ScrollView의 Content)
    public GameObject layerItemPrefab; // 레이어 UI 아이템 프리팹
    public LayerManager layerManager; // LayerManager 참조
    public TilePlacer tilePlacer; // TilePlacer 참조
    private int layerNum;
    void Start()
    {
        InitializeLayerUI();
    }

    void Update()
    {
        // PageUp 키 입력 감지
        if (Input.GetKeyDown(KeyCode.PageUp) && int.Parse(layerManager.activeLayerName) < layerManager.layers.Count-1)
        {
            layerNum = int.Parse(layerManager.activeLayerName);
            layerNum = layerNum + 1;
            SelectLayer(layerManager.layers[layerNum].layerName);
        }

        // PageDown 키 입력 감지
        if (Input.GetKeyDown(KeyCode.PageDown) && int.Parse(layerManager.activeLayerName) > 0)
        {
            layerNum = int.Parse(layerManager.activeLayerName);
            layerNum = layerNum - 1;
            SelectLayer(layerManager.layers[layerNum].layerName);
        }
    }

    void InitializeLayerUI()
    {
        foreach (var layer in layerManager.layers)
        {
            // 레이어 아이템 프리팹 인스턴스화
            GameObject item = Instantiate(layerItemPrefab, layerContainer);

            // 레이어 이름 설정
            TMP_Text layerNameText = item.transform.Find("LayerName").GetComponent<TMP_Text>();
            if (layerNameText != null)
                layerNameText.text = layer.layerName;

            // 가시성 토글 설정
            Toggle visibilityToggle = item.transform.Find("VisibilityToggle").GetComponent<Toggle>();
            if (visibilityToggle != null)
            {
                visibilityToggle.isOn = layer.isVisible;
                visibilityToggle.onValueChanged.AddListener((isOn) => layerManager.ToggleLayerVisibility(layer.layerName));
            }

            // 잠금 토글 설정
            Toggle lockToggle = item.transform.Find("LockToggle").GetComponent<Toggle>();
            if (lockToggle != null)
            {
                lockToggle.isOn = layer.isLocked;
                lockToggle.onValueChanged.AddListener((isOn) => layerManager.ToggleLayerLock(layer.layerName));
            }

            // 레이어 선택 버튼 설정
            Button selectButton = item.transform.Find("SelectButton").GetComponent<Button>();
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(() => SelectLayer(layer.layerName));
            }
        }
    }

    void SelectLayer(string layerName)
    {
        tilePlacer.tilemap = layerManager.GetTilemap(layerName);
        layerManager.SetActiveLayer(layerName); // 활성 레이어 설정
    }

    void OnHeightChanged(LayerManager.Layer layer, string value)
    {
        if (int.TryParse(value, out int newHeight))
        {
            if (newHeight < 0)
            {
                Debug.LogWarning("Height cannot be negative.");
                return;
            }

            layer.height = newHeight;

            // 타일맵의 렌더 순서를 고도 값에 따라 변경
            TilemapRenderer renderer = layer.tilemap.GetComponent<TilemapRenderer>();
            renderer.sortingOrder = layer.height;

            // 레이어 렌더링 업데이트
            layerManager.UpdateLayerRendering();
        }
        else
        {
            // 잘못된 입력 처리 (예: 숫자가 아닌 값)
            Debug.LogWarning("Invalid height value entered.");
        }
    }
}
