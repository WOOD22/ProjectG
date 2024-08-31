using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float cameraMoveSpeed = 10f; // 카메라 이동 속도
    private Transform targetUnitTransform; // 현재 턴의 유닛 Transform

    void Update()
    {
        HandleCameraMovement();
    }

    // WASD를 이용한 카메라 이동과 스페이스 바를 통한 유닛 위치로 이동
    private void HandleCameraMovement()
    {
        Vector3 cameraPosition = transform.position;

        // WASD 키를 사용하여 카메라 이동
        if (Input.GetKey(KeyCode.W))
        {
            cameraPosition.y += cameraMoveSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.S))
        {
            cameraPosition.y -= cameraMoveSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.A))
        {
            cameraPosition.x -= cameraMoveSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.D))
        {
            cameraPosition.x += cameraMoveSpeed * Time.deltaTime;
        }

        transform.position = cameraPosition;

        // 스페이스 바를 누르면 현재 턴의 유닛 위치로 카메라 이동
        if (Input.GetKeyDown(KeyCode.Space) && targetUnitTransform != null)
        {
            MoveCameraToUnit(targetUnitTransform);
        }
    }

    // 카메라를 특정 유닛 위치로 이동시키는 메서드
    public void MoveCameraToUnit(Transform unitTransform)
    {
        transform.position = new Vector3(unitTransform.position.x, unitTransform.position.y, transform.position.z);
    }

    // 현재 턴의 유닛을 설정하고 즉시 카메라를 이동시키는 메서드
    public void SetTargetUnit(Transform unitTransform)
    {
        targetUnitTransform = unitTransform;
        MoveCameraToUnit(unitTransform); // 유닛 타겟 설정 후 즉시 카메라 이동
    }
}