/********************************************************************************/
// 작성일 : 2021.09.08
// 작성자 : 최진우
// 설  명 : 메인 메뉴 매니저
/********************************************************************************/
// 수정일      | 종류 | 수정자 | 내용
// 2021.09.08 | ADD  | 최진우 | 신규 작성
/********************************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Michsky.UI.Shift;

public class MainMenuManager : MonoBehaviour
{
    /********************************************************************************/
    // 필요 컴포넌트
    /********************************************************************************/
    [SerializeField]
    private PlayManager playManager;                // PlayManager와 연결
    [SerializeField]
    private Camera cameraToFollow;                  // 사용자 HMD의 카메라 컴포넌트
    private Vector3 menuPosition;                   // 메인 메뉴 기본 위치
    [SerializeField]
    private GameObject[] engTexts;                  // 메인 메뉴 영어 텍스트 배열
    [SerializeField]
    private GameObject[] korTexts;                  // 메인 메뉴 한국어 텍스트 배열
    [SerializeField]
    private GameObject menuCanvas;                  // 설정창 캔버스
    [SerializeField]
    private GameObject textExit;


    /********************************************************************************/
    // 메뉴 움직임 설정 변수
    /********************************************************************************/
    [Range(0.7f, 2)]
    public float menuToCamDistance;                 // 카메라와 메뉴 사이의 거리 저장 변수
    public float moveSpeed;                         // 메뉴가 움직이는 속도
    public float moveCount;                         // 메뉴가 움직일 수 있는 기본 프레임
    public float moveAngle;                         // 메뉴가 움직이기 위한 임계 각도
    
    private bool isActivate = false;                // 시작시 한번만 메뉴 위치를 설정하기 위한 상태 변수
    private bool isMenuMoving = false;              // 메뉴가 움직이는 중인지 여부

    void Start()
    {
        // 메뉴 언어 기본 설정
        DataClass.LanguageType _languagetype  = playManager.GetLanguage();
        // 한국어 텍스트 활성화, 영어 텍스트 비활성화
        if (_languagetype.Equals(DataClass.LanguageType.Korean))
        {
            foreach (var engText in engTexts)
            {
                engText.SetActive(false);
            }
            
            foreach (var korText in korTexts)
            {
                korText.SetActive(true);
            }
        }
        // 영어 텍스트 활성화, 한국어 텍스트 비활성화
        else
        {
            foreach (var engText in engTexts)
            {
                engText.SetActive(true);
            }
            
            foreach (var korText in korTexts)
            {
                korText.SetActive(false);
            }
        }
    }

    // 일정한 움직임 처리를 위해 FixedUpdate 설정
    void FixedUpdate()
    {
        // 메뉴 기본 위치 설정
        menuPosition = new Vector3(cameraToFollow.transform.position.x,
                                   cameraToFollow.transform.position.y,
                                   cameraToFollow.transform.position.z + menuToCamDistance);

        // 시작 시 한 번만 메뉴 위치를 기본 위치로 설정
        if (isActivate == false)
        {
            // Start에서 처리할 경우 카메라 활성화 여부로 인해 처리가 안 됨
            isActivate = true;
            transform.position = menuPosition;
        }

        // todo. 테스트용(비 XR 환경)
        if (Input.GetKeyDown(KeyCode.K))
        {
            SetLanguage_Korean();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            SetLanguage_English();
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            CanvasExit();
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            ExitButtonTest();
        }

        TryFollow();
    }

    // 메뉴 이동 처리 준비
    private void TryFollow()
    {
        // 카메라 시선 방향 벡터
        Vector3 _gazeDirection = cameraToFollow.transform.localRotation * Vector3.forward * menuToCamDistance;
        // 카메라 위치에서 메뉴로 향하는 벡터
        Vector3 _cameraToMenu = transform.position - cameraToFollow.transform.position;
        // 시선 방향 벡터의 회전값
        Quaternion q = Quaternion.LookRotation(_gazeDirection);
        // 메뉴가 이동해야 할 동선 벡터
        Vector3 _moveDir = _gazeDirection - _cameraToMenu;

        // 카메라와 메뉴 사이의 거리 저장
        float betWeenDist = Vector3.Magnitude(_cameraToMenu);
        // 카메라 시선 벡터와 카메라 위치에서 메뉴 위치로 향하는 벡터 사이의 각도
        float betweenAngle = Vector3.Angle(_gazeDirection, _cameraToMenu);

        // 메뉴가 움직이지 않고있을 때 움직임 코루틴 시작
        if (isMenuMoving == false)
        {
            StartCoroutine(FollowCamera(_moveDir, q, betweenAngle));
        }
    }

    // Coroutine 사용한 부드러운 메뉴 이동 로직
    IEnumerator FollowCamera(Vector3 vec, Quaternion quaternion, float angle)
    {   
        // 메뉴가 이동해야 할 위치
        Vector3 _menuPos = transform.position + vec - Vector3.up * 0.1f;
        // while문 끝내기를 위한 임시 변수
        int count = 0;
        isMenuMoving = true;

        // 시선 각도가 임계 각을 벗어났을 때 로직 시작
        if (angle > moveAngle)
        {   
            // 현재 메뉴의 위치가 이동해야 할 위치와 다를 경우 이동 시작
            while(transform.position != _menuPos)
            {
                // while문 끝내기를 위한 임시 변수 증가
                count ++;

                // 메뉴 이동 및 회전 부드럽게 처리
                transform.position = Vector3.Lerp(transform.position, _menuPos, Time.deltaTime * moveSpeed);
                transform.rotation = Quaternion.Lerp(transform.rotation, quaternion, Time.deltaTime * moveSpeed);

                // 임계값 벗어났을 때 while문 종료
                if(count > moveCount)
                {
                    break;
                }
                yield return null;
            }
            // while문 종료 후 위치 및 각도 처리
            // Lerp사용할 경우 정확한 값으로 가지 않기 때문에 정확한 위치 및 각도 입력
            transform.position = _menuPos;
            transform.rotation = quaternion;
        }
        // 메뉴 이동 종료, 재이동 가능하도록 처리
        isMenuMoving = false;
    }

    public void TempSelected()
    {
        this.gameObject.SetActive(false);
    }

    // 설정 버튼 선택
    public void SettingsButtonSelected()
    {
        // 설정창 비활성화 시 설정창 활성화
        if(menuCanvas.activeSelf.Equals(false))
        {
            menuCanvas.SetActive(true);
        }
        // 설정창 활성화 시 설정창 비활성화
        else
        {
            menuCanvas.SetActive(false);
        }
    }

    // 언어 설정 : 한국어
    public void SetLanguage_Korean()
    {
        // 영어 텍스트 전체 비활성화
        foreach (var engText in engTexts)
        {
            engText.SetActive(false);
        }
        // 한국어 텍스트 전체 활성화
        foreach (var korText in korTexts)
        {
            korText.SetActive(true);
        }

        // PlayManager 언어설정 한국어로 설정
        playManager.SetLanguageKorean();
    }

    // 언어 설정 : 영어
    public void SetLanguage_English()
    {
        // 영어 텍스트 전체 활성화
        foreach (var engText in engTexts)
        {
            engText.SetActive(true);
        }
        // 한국어 텍스트 전체 비활성화
        foreach (var korText in korTexts)
        {
            korText.SetActive(false);
        }
        // PlayManager 언어설정 영어로 설정
        playManager.SetLanguageEnglish();
    }

    // 설정창 닫기 버튼 선택
    public void CanvasExit()
    {
        // 설정창 비활성화
        if (menuCanvas.activeSelf.Equals(true))
        {
            menuCanvas.SetActive(false);
        }
    }

    // 테스트용
    public void ExitButtonTest()
    {
        textExit.GetComponent<ModalWindowManager>().ModalWindowIn();
    }
}