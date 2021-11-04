/********************************************************************************/
// 작성일 : 2021.08.20
// 작성자 : 최진우
// 설  명 : 컨트롤 패널 매니저 (보기 이동/회전 처리)
/********************************************************************************/
// 수정일      | 종류 | 수정자 | 내용
// 2021.08.20 | ADD  | 최진우 | 신규 작성
/********************************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Interaction;
using Leap.Unity.Animation;

public class ControlPanelManager : MonoBehaviour
{
    /********************************************************************************/
    // 이동 방향 열거형
    /********************************************************************************/
    // Stop     : 정지
    // Left     : 왼쪽 이동
    // Right    : 오른쪽 이동
    // Front    : 전방 이동
    // Back     : 후방 이동
    // Down     : 상승
    // Up       : 하강
    /********************************************************************************/
    private enum Move {Stop, Left, Right, Front, Back, Down, Up};

    /********************************************************************************/
    // 회전 방향 열거형
    /********************************************************************************/
    // Stop     : 정지
    // Left     : 시계방향 회전
    // Right    : 반시계방향 회전
    /********************************************************************************/
    private enum Rotate {Stop, Left, Right};

    private Move bogieMove = Move.Stop;                     // 기본 이동 상태 : 정지
    private Rotate bogieRotate = Rotate.Stop;               // 기본 회전 상태 : 정지

    /********************************************************************************/
    // 보기 이동 관련 변수
    /********************************************************************************/
    [SerializeField]
    private float moveSpeed;                                // 보기 이동 속도
    [SerializeField]
    private float rotateSpeed;                              // 보기 회전 속도

    // 이동 및 회전 방향
    private Vector3 moveDir   = Vector3.zero;               // 기본 이동 방향 : 정지
    private Vector3 rotateDir = Vector3.zero;               // 기본 회전 방향 : 정지

    [SerializeField]
    private GameObject bogie;                               // 보기 할당

    // Update is called once per frame
    void Update()
    {
        // 스텝 시작 시 보기 할당
        SetBogie();
        if (bogie != null)
        {
            Movebogie();
            RotateBogie();
        }
    }

    // 이동 상태에 따른 보기 이동 로직
    private void Movebogie()
    {
        switch(bogieMove)
        {
            // 정지 상태
            case Move.Stop:
                bogie.transform.Translate(moveDir);
                break;

            // 좌측 이동
            case Move.Left:
                bogie.transform.Translate(moveDir * moveSpeed * Time.deltaTime, Space.World);
                break;

            // 우측 이동
            case Move.Right:
                bogie.transform.Translate(moveDir * moveSpeed * Time.deltaTime, Space.World);
                break;

            // 보기 전진
            case Move.Front:
                bogie.transform.Translate(moveDir * moveSpeed * Time.deltaTime, Space.World);
                break;

            // 보기 후진
            case Move.Back:
                bogie.transform.Translate(moveDir * moveSpeed * Time.deltaTime, Space.World);
                break;

            // 보기 하강
            case Move.Down:
                // y값이 0 이하로 내려가지 않도록 설정
                if (bogie.transform.position.y >= 0)
                {   
                    bogie.transform.Translate(moveDir * moveSpeed * Time.deltaTime);
                }
                else
                {
                    bogie.transform.Translate(Vector3.zero);
                }
                break;

            // 보기 상승
            case Move.Up:
                bogie.transform.Translate(moveDir * moveSpeed * Time.deltaTime);
                break;
        }
    }

    // 회전 상태에 따른 보기 회전 로직
    public void RotateBogie()
    {
        switch(bogieRotate)
        {
            // 정지 상태
            case Rotate.Stop:
                bogie.transform.Rotate(rotateDir * 0 * Time.deltaTime, Space.World);
                break;

            // 좌측 회전(시계 방향)
            case Rotate.Left:
                bogie.transform.Rotate(rotateDir * rotateSpeed * Time.deltaTime, Space.World);
                break;

            // 우측 회전(반시계 방향)
            case Rotate.Right:
                bogie.transform.Rotate(rotateDir * rotateSpeed * Time.deltaTime, Space.World);
                break;
        }
    }

    // 버튼 조작 X 기본상태 : 정지
    public void StopBogie()
    {
        bogieMove = Move.Stop;              // 이동 상태
        moveDir = Vector3.zero;             // 이동 방향
    }

    // 좌측 버튼 누르고 있을때 상태 변경
    public void LeftButtonPressed()
    {
        bogieMove = Move.Left;
        moveDir = -Vector3.right;
    }

    // 우측 버튼 누르고 있을때 상태 변경
    public void RightButtonPressed()
    {
        bogieMove = Move.Right;
        moveDir = Vector3.right;
    }

    // 전진 버튼 누르고 있을때 상태 변경
    public void FrontButtonPressed()
    {
        bogieMove = Move.Front;
        moveDir = Vector3.forward;
    }

    // 후진 버튼 누르고 있을때 상태 변경
    public void BackButtonPressed()
    {
        bogieMove = Move.Back;
        moveDir = -Vector3.forward;
    }

    // 하강 버튼 누르고 있을때 상태 변경
    public void DownButtonPressed()
    {
        bogieMove = Move.Down;
        moveDir = -Vector3.up;
    }

    // 상승 버튼 누르고 있을때 상태 변경
    public void UpButtonPressed()
    {
        bogieMove = Move.Up;
        moveDir = Vector3.up;
    }

    // 버튼 조작 X 기본상태 : 정지
    public void RotateStop()
    {
        bogieRotate = Rotate.Stop;
        rotateDir = Vector3.zero;
    }

    // 왼쪽 회전 누르고 있을때 상태 변경
    public void RotateLeftbuttonPressed()
    {
        bogieRotate = Rotate.Left;
        rotateDir = Vector3.up;
    }

    // 오른쪽 회전 누르고 있을때 상태 변경
    public void RotateRightbuttonPressed()
    {
        bogieRotate = Rotate.Right;
        rotateDir = -Vector3.up;
    }

    // PlayManager로부터 스크립트에 맞는 보기 가져오기
    public void SetBogie()
    {
        bogie = PlayManager.instance.moveableBogie;
    }
}