using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class LayerManager : MonoBehaviour
{
    public static LayerManager Instance;

    [System.Serializable]
    public class Layer
    {
        public string layerName;
        public Tilemap tilemap;
        public bool isVisible = true;
        public bool isLocked = false;
        public int height; // 고도 값
        public Material tilemapMaterial; // 각 레이어의 머티리얼
    }

    public List<Layer> layers = new List<Layer>();
    public string activeLayerName; // 현재 활성화된 레이어 이름

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        foreach (var layer in layers)
        {
            // 각 타일맵에 개별 머티리얼 설정
            TilemapRenderer renderer = layer.tilemap.GetComponent<TilemapRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            layer.tilemapMaterial = renderer.material;

            // Sorting Layer 설정 (모든 레이어가 동일한 Sorting Layer 사용)
            renderer.sortingLayerName = "Default"; // 필요에 따라 다른 Sorting Layer 사용 가능

            // 초기 렌더 순서 설정
            renderer.sortingOrder = layer.height;
        }

        // 초기 활성 레이어 설정
        if (layers.Count > 0)
        {
            SetActiveLayer(layers[0].layerName);
        }
    }

    public void SetActiveLayer(string layerName)
    {
        activeLayerName = layerName;
        UpdateLayerRendering();
    }

    public void UpdateLayerRendering()
    {
        int activeLayerHeight = GetLayerHeight(activeLayerName);

        foreach (var layer in layers)
        {
            TilemapRenderer renderer = layer.tilemap.GetComponent<TilemapRenderer>();
            Material material = layer.tilemapMaterial;

            if (layer.layerName == activeLayerName)
            {
                // 현재 레이어: 명도값 1, 알파값 1
                material.color = new Color(1f, 1f, 1f, 1f);
            }
            else if (layer.height < activeLayerHeight)
            {
                // 현재 레이어보다 낮은 레이어: 명도값 0.5, 알파값 1
                material.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            }
            else if (layer.height > activeLayerHeight)
            {
                // 현재 레이어보다 높은 레이어: 명도값 1, 알파값 0.5
                material.color = new Color(1f, 1f, 1f, 0.05f);
            }
            else
            {
                // 동일한 높이의 다른 레이어 (필요에 따라 조정)
                material.color = new Color(1f, 1f, 1f, 1f);
            }
        }
    }

    public void ToggleLayerVisibility(string layerName)
    {
        Layer layer = layers.Find(l => l.layerName == layerName);
        if (layer != null)
        {
            layer.isVisible = !layer.isVisible;
            layer.tilemap.gameObject.SetActive(layer.isVisible);
        }
    }

    public void ToggleLayerLock(string layerName)
    {
        Layer layer = layers.Find(l => l.layerName == layerName);
        if (layer != null)
        {
            layer.isLocked = !layer.isLocked;
        }
    }

    public Tilemap GetTilemap(string layerName)
    {
        Layer layer = layers.Find(l => l.layerName == layerName);
        return layer?.tilemap;
    }

    public bool IsLayerLocked(string layerName)
    {
        Layer layer = layers.Find(l => l.layerName == layerName);
        return layer != null && layer.isLocked;
    }

    public int GetLayerHeight(string layerName)
    {
        Layer layer = layers.Find(l => l.layerName == layerName);
        return layer != null ? layer.height : 0;
    }
}
