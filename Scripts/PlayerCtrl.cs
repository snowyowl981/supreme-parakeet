/********************************************************************************/
// 작성일 : 2021.05.27
// 작성자 : 최진우
// 설  명 : 플레이어 컨트롤러
/********************************************************************************/
// 수정일      | 종류 | 수정자 | 내용
// 2021.05.27 | ADD  | 최진우 | 신규 작성
// 2021.11.11 | ADD  | 최진우 | 주석 추가 작성
/********************************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PlayerCtrl : MonoBehaviourPun, IPunObservable
{
    /********************************************************************************/
    // 필요 컨트롤러 컴포넌트
    /********************************************************************************/
    public OVRInput.Controller leftController = OVRInput.Controller.LTouch;
    public OVRInput.Controller rightController = OVRInput.Controller.RTouch;

    /********************************************************************************/
    // 플레이어 이동 관련
    /********************************************************************************/
    [SerializeField]
    private Vector3 leftHandPos;                            // 왼손 컨트롤러 위치값
    [SerializeField]
    private Vector3 rightHandPos;                           // 오른손 컨트롤러 위치값

    // 플레이어 이동속도 관련 변수
    [SerializeField]
    private float moveSpeed;                                // 플레이어 이동속도
    private float leftSwingForce;                           // 왼손 스윙 값
    private float rightSwingForce;                          // 오른손 스윙 값

    // 이전 프레임 스윙 값
    private float leftLastSwing;                            // 1프레임 전의 왼손 스윙 값
    private float rightLastSwing;                           // 1프레임 전의 오른손 스윙 값

     // 왼손 위치 저장 변수
    private Vector3 leftLastPos;                            // 1프레임 전의 왼손 위치
    private Vector3 leftStartPos;                           // 왼손이 올라가다가 내려가는 지점
    private Vector3 leftEndPos;                             // 왼손이 내려갔다가 올라가는 지점
    private Vector3 leftSwingDownVec = Vector3.zero;        // 왼손 Start-End 스윙 방향

    // 오른손 위치 저장 변수
    private Vector3 rightLastPos;                           // 1프레임 전의 오른손 위치
    private Vector3 rightStartPos;                          // 오른손이 올라가다가 내려가는 지점
    private Vector3 rightEndPos;                            // 오른손이 내려갔다가 올라가는 지점
    private Vector3 rightSwingDownVec = Vector3.zero;       // 오른손 Start-End 스윙 방향

    // 딜레이 측정 변수
    private float leftStartTime = 0;
    private float leftDelayTime = 0;

    private float rightStartTime = 0;
    private float rightDelayTime = 0;

    // 움직임 상태 체크변수
    private bool isLeftMovingDown = false;                  // 왼손 아래로 움직이는지 체크
    private bool isRightMovingDown = false;                 // 오른손 아래로 움직이는지 체크
    public bool isPlayerMove = false;                       // 플레이어가 이동중인지 체크
    private bool isBreak = false;                           // 브레이크(후진) 이동중인지 체크

    // 컨트롤러 손에 쥐었는지 확인하는 상태변수
    public bool isLeftMove = false;                         // 왼손 컨트롤러 쥐고있는지 체크
    public bool isRightMove = false;                        // 오른손 컨트롤러 쥐고있는지 체크

    /********************************************************************************/
    // 레이캐스트 관련
    /********************************************************************************/
    private RaycastHit hit;                                 // 전방 레이캐스트 컴포넌트
    private RaycastHit backHit;                             // 후방 레이캐스트 컴포넌트
    public bool isBorder = false;                           // 전방 벽에 닿았는지 체크
    public bool isBackBorder = false;                       // 후방 벽에 닿았는지 체크

    /********************************************************************************/
    // 기타 필요 컴포넌트
    /********************************************************************************/
    private GameObject player;                              // 플레이어 게임오브젝트
    private Transform camTr;                                // OVR 카메라 Transform값
    private PhotonView pv;                                  // 포톤뷰

    void Start()
    {
        // 필요 컴포넌트 서치 및 초기화
        pv = GetComponentInParent<PhotonView>();
        player = transform.parent.gameObject;

        //내꺼 아니면 카메라, 오디오 리스너 없애기
        if (!pv.IsMine)
        {
            GetComponentInChildren<AudioListener>().enabled = false;
            Camera[] cams = GetComponentsInChildren<Camera>();
            foreach (var cam in cams)
            {
                cam.enabled = false;
            }
        }
        else
        {
            camTr = Camera.main.GetComponent<Transform>();
        }
        OVRManager.display.RecenterPose();                  // 시점 초기화
    }

    void Update()
    {
        if (pv.IsMine)
        {
            StartCoroutine(LeftControllerMoveCheck());      // 왼손 컨트롤러 움직임 계산함수
            StartCoroutine(RightControllerMoveCheck());     // 왼손 컨트롤러 움직임 계산함수
            TryBreak();                                     // 브레이크 시도
            StopToWall();                                   // Ray가 벽에 부딫혔는지
        }
        else
        {
            // 포톤이 내 것이 아닐 경우 보정
            if((player.transform.position - receivePos).sqrMagnitude > 10.0f * 10.0f)
            {
                player.transform.position = receivePos;
            }
            else
            {
                player.transform.position = Vector3.Lerp(player.transform.position, receivePos, Time.deltaTime * 10f);
            }
        }
    }

    // 왼손 컨트롤러 벡터 체크
    IEnumerator LeftControllerMoveCheck()
    {
        // 왼쪽 컨트롤러 움직이는지 확인
        isLeftMove = OVRInput.GetControllerPositionTracked(leftController);

        // 왼쪽 컨트롤러 위치좌표 확인
        leftHandPos = OVRInput.GetLocalControllerPosition(leftController);

        // 왼손 체크
        if (isLeftMovingDown == false)
        {
            // 최고점 찍고 내려가려는 상태
            if (leftLastPos.y > leftHandPos.y + 0.03f)
            {
                leftStartPos = leftLastPos;
                leftStartTime = Time.time;
                isLeftMovingDown = true;
            }
        }
        else
        {
            // 정지 혹은 올라가려는 상태
            if (Vector3.Distance(leftHandPos, leftLastPos) < 0.02f)
            {
                leftEndPos = leftHandPos;
                float _endTime = Time.time;
                leftDelayTime = _endTime - leftStartTime;

                leftSwingDownVec = leftEndPos - leftStartPos;
                isLeftMovingDown = false;

                // 벡터 거리, 속도 계산
                float _leftDist = Vector3.Magnitude(leftSwingDownVec);
                float _leftSpeed = _leftDist / leftDelayTime;
                leftSwingForce = _leftDist + _leftSpeed;

                // 움직임 코루틴 호출(그랩버튼 입력중일 시)
                if (isLeftMove == true && isPlayerMove == false)
                {
                    if (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch))
                    {
                        yield return StartCoroutine(MoveCheckCoroutine());
                    }
                }
            }
        }
        leftLastPos = leftHandPos;
        leftLastSwing = leftSwingForce;
    }

    // 오른손 컨트롤러 벡터 체크
    IEnumerator RightControllerMoveCheck()
    {
        // 오른쪽 컨트롤러 움직이는지 확인
        isRightMove = OVRInput.GetControllerPositionTracked(rightController);

        // 오른쪽 컨트롤러 위치 좌표 확인
        rightHandPos = OVRInput.GetLocalControllerPosition(rightController);

        // 오른손 체크
        if (isRightMovingDown == false)
        {
            // 최고점 찍고 내려가려는 상태
            if (rightLastPos.y > rightHandPos.y + 0.03f)
            {
                rightStartPos = rightLastPos;
                rightStartTime = Time.time;
                isRightMovingDown = true;
            }
        }
        else
        {
            // 정지 혹은 올라가려는 상태
            if (Vector3.Distance(rightHandPos, rightLastPos) < 0.03f)
            {
                rightEndPos = rightHandPos;
                float _rightEndTime = Time.time;
                rightDelayTime = _rightEndTime - rightStartTime;

                rightSwingDownVec = rightEndPos - rightStartPos;
                isRightMovingDown = false;

                // 벡터 거리, 속도 계산
                float _rightDist = Vector3.Magnitude(rightSwingDownVec);
                float _rightSpeed = _rightDist / rightDelayTime;
                rightSwingForce = _rightDist + _rightSpeed;

                // 움직임 체크 함수
                if (isRightMove == true && isPlayerMove == false)
                {
                    if (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch))
                    {
                        yield return StartCoroutine(MoveCheckCoroutine());
                    }
                }
            }
        }
        // 1프레임마다 좌표값 저장
        rightLastPos = rightHandPos;
        rightLastSwing = rightSwingForce;
    }

    // 움직임 상태 체크 및 조정
    IEnumerator MoveCheckCoroutine()
    {
        isPlayerMove = false;
        // 양 손 이전 스윙 임시저장
        float _rls = rightLastSwing;
        float _lls = leftLastSwing;
        yield return null;

        // 현재 스윙 임시저장
        float _lsf = leftSwingForce;
        float _rsf = rightSwingForce;
        float _applySwing = 0;

        // 예외처리
        if (_lsf > 11.0f)
            _lsf = 0;

        if(_rsf > 11.0f)
            _rsf = 0;

        // 이전 스윙과 현재 스윙을 비교해 속도 조절
        if (_lls == _lsf)
        {
            _applySwing = _rsf;
        }
        else if (_rls == _rsf)
        {
            _applySwing = _lsf;
        }
        else 
        {
            _applySwing = (_lsf + _rsf) * 0.7f;
        }

        // 벽을 감지하지 않았을때만 움직임
        if (isBorder == false)
            StartCoroutine(MoveCoroutine(_applySwing));
    }

    // 이동 처리
    IEnumerator MoveCoroutine(float _swingForce)
    {
        isPlayerMove = true;

        Vector3 _playerMovePos = player.transform.position + 
            (camTr.transform.forward * Time.deltaTime * moveSpeed * _swingForce);

        // 부드러운 이동 처리
        int count = 0;
        while (player.transform.position != _playerMovePos)
        {
            count++;

            player.transform.position = Vector3.Lerp(player.transform.position, _playerMovePos, 0.1f);

            if (count > 48)
            {
                break;
            }
            yield return null;
        }
        player.transform.position = Vector3.Lerp(player.transform.position, _playerMovePos, 0.1f);
        isPlayerMove = false;
    }

    // 브레이크 및 후진 시도
    private void TryBreak()
    {
        // 그랩버튼 당기고있을 경우 체크
        if (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, leftController) && OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, rightController))
        {
            // 양손 트리거버튼 체크
            if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, leftController) && OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, rightController))
            {
                if (isBreak == false && isBackBorder == false)
                {
                    // 전체 코루틴 정지 및 브레이크 코루틴 실행
                    StopAllCoroutines();
                    StartCoroutine(BreakCoroutine());
                }
            }
        }
    }

    // 레이캐스트를 발사해 벽에 닿는지 감지
    void StopToWall()
    {
        // 레이를 전방 생성해 벽 통과 방지
        if (Physics.Raycast(camTr.position, camTr.forward, out hit, 4) && hit.transform.tag == "BOUNDARY")
        {
            isBorder = true;
        }
        else
        {
            isBorder = false;
        }

        // 후진할때만 레이를 뒤로 생성해 벽 통과 방지
        if (Physics.Raycast(camTr.position, -camTr.forward, out backHit, 3) && backHit.transform.tag == "BOUNDARY")
        {
            isBackBorder = true;
        }
        else
        {
            isBackBorder = false;
        }
    }

    // 브레이크 & 후진
    IEnumerator BreakCoroutine()
    {
        isBreak = true;

        // 후진 위치 지정
        Vector3 backOffPos = player.transform.position - 
        (2 * camTr.transform.forward * Time.deltaTime * moveSpeed);

        int count = 0;
        while (player.transform.position != backOffPos)
        {
            count++;
            player.transform.position = Vector3.Lerp(player.transform.position, backOffPos, 0.2f);

            if(count > 45)
            {
                break;
            }

            yield return null;
        }
        player.transform.position = backOffPos;
        // 이동 중 후진했을 때 예외처리
        if  (isPlayerMove == true) isPlayerMove = false;
        isBreak = false;
    }

    // 네트워크를 통해서 수신받을 변수
    Vector3 receivePos = Vector3.zero;
    Quaternion receiveRot = Quaternion.identity;

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if(stream.IsWriting) // PhotonView.IsMine == true;
        {
            stream.SendNext(player.transform.position);   // 위치
            stream.SendNext(camTr.rotation);              // 회전값
        }
        else
        {
            // 두번 보내서 두번 받음
            receivePos = (Vector3)stream.ReceiveNext();
            receiveRot = (Quaternion)stream.ReceiveNext();
        }
    }
}
