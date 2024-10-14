using UnityEngine;
using UnityEngine.U2D;

public class CameraController : MonoBehaviour
{
    public Camera mainCamera;
    public PixelPerfectCamera pixelPerfectCamera;

    public float moveSpeed = 5f;
    public float zoomResistance = 0.5f; // 휠 굴림 저항값 (높을수록 저항이 커짐)

    private Vector3 lastPanPosition;
    private float zoomProgress = 0f;
    private int[] ppuValues = { 16, 32, 64, 128 };
    private int currentPPUIndex = 0; // 기본값 16를 가리키는 인덱스

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    void Update()
    {
        HandleMovement();
        HandleZoom();
    }

    void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal"); // A, D 키 또는 화살표 좌우
        float v = Input.GetAxis("Vertical");   // W, S 키 또는 화살표 상하

        Vector3 movement = new Vector3(h, v, 0f);
        transform.Translate(movement * moveSpeed * Time.deltaTime, Space.World);
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            zoomProgress += scroll / zoomResistance;

            if (Mathf.Abs(zoomProgress) >= 1f)
            {
                int zoomDirection = -(int)Mathf.Sign(zoomProgress); // 여기를 수정했습니다
                currentPPUIndex = Mathf.Clamp(currentPPUIndex - zoomDirection, 0, ppuValues.Length - 1);
                pixelPerfectCamera.assetsPPU = ppuValues[currentPPUIndex];
                zoomProgress = 0f;
            }
        }
        else
        {
            // 휠 굴림이 멈추면 진행도를 서서히 0으로 리셋
            zoomProgress = Mathf.MoveTowards(zoomProgress, 0, Time.deltaTime);
        }
    }
}