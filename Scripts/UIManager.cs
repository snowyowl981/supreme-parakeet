/********************************************************************************/
// 작성일 : 2021.09.11
// 작성자 : 최진우
// 설  명 : UI 매니저
/********************************************************************************/
// 수정일      | 종류 | 수정자 | 내용
// 2021.09.11 | ADD  | 최진우 | 신규 작성
/********************************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Interaction;
using Leap.Unity.Animation;

public class UIManager : MonoBehaviour
{
    /********************************************************************************/
    // 오브젝트 종류 열거형
    // 선택한 오브젝트
    /********************************************************************************/
    // Magenta          : 컨트롤 패널
    // Red              : 시나리오 패널
    // Blue             : 미디어 패널
    // SelectedObject   : 선택 오브젝트
    /********************************************************************************/
    private enum Objects {Magenta, Red, Blue};
    private Objects selectedObject;

    /********************************************************************************/
    // 패널 Transform 컴포넌트
    /********************************************************************************/
    [Header("Hand Panel Transform")]
    [SerializeField]
    private Transform controlPanel;                             // 조작 패널 Transform
    [SerializeField]
    private Transform tempoaryPanel1;                           // 시나리오 패널 Transform
    [SerializeField]
    private Transform tempoaryPanel2;                           // 미디어 패널 Transform

    /********************************************************************************/
    // 패널 위치 저장 Tranform 컴포넌트
    /********************************************************************************/
    [Header("Hide & Visible Panel Transform")]
    [SerializeField]
    private Transform palmHidden;                               // 패널 위치 == 손바닥, 숨김
    [SerializeField]
    private Transform palmVisible;                              // 패널 위치 == 손바닥, 보임
    [SerializeField]
    private Transform AnchorVisible;                            // 패널 위치 == 앵커,   보임

    /********************************************************************************/
    // 오브젝트의 AnchorableBehaviour 컴포넌트
    /********************************************************************************/
    [Header("AnchorableBehaviour")]
    [SerializeField]
    private AnchorableBehaviour magenta;                        // 조작 패널 AnchorableBehaviour
    [SerializeField]
    private AnchorableBehaviour red;                            // 시나리오 패널 AnchorableBehaviour
    [SerializeField]
    private AnchorableBehaviour blue;                           // 미디어 패널 AnchorableBehaviour

    /********************************************************************************/
    // 오브젝트 분리 전 패널들의 원래 위치(사용자 손에 위치)
    /********************************************************************************/
    [SerializeField]
    private GameObject palmUIPivotAnchor;

    /********************************************************************************/
    // 오브젝트 분리 시 각 패널의 부모가 될 게임오브젝트(오브젝트 내부에 위치)
    /********************************************************************************/
    [Header("WorkstationBase")]
    [SerializeField]
    private GameObject magentaWorkstationBase;                  // 조작 패널
    [SerializeField]
    private GameObject redWorkstationBase;                      // 시나리오 패널
    [SerializeField]
    private GameObject blueWorkstationBase;                     // 미디어 패널

    /********************************************************************************/
    // 머티리얼 관련
    /********************************************************************************/
    [Header("Materials")]
    [SerializeField]
    private Material prismDefault;                              // 기본 머티리얼
    [SerializeField]
    private Material prismSelected;                             // 선택 시 머티리얼

    /********************************************************************************/
    // 기타 컴포넌트
    /********************************************************************************/
    private TransformTweenBehaviour transformTweenBehaviour;    // 애니메이션 처리 컴포넌트
    private AudioSource anchorSound;                            // 사운드 처리
    public bool isStaying = false;                              // 사운드 중복 제거용

    // 시작 시 기본 설정
    void Start()
    {
        // 비활성화되어있는 시나리오, 미디어 패널 활성화
        tempoaryPanel1.gameObject.SetActive(true);
        tempoaryPanel2.gameObject.SetActive(true);

        // 시나리오, 미디어 패널 안보이게끔 처리
        HideMenu(tempoaryPanel1);
        HideMenu(tempoaryPanel2);
        
        // 애니메이션 처리 및 사운드 컴포넌트 취득
        transformTweenBehaviour = GetComponentInChildren<TransformTweenBehaviour>();
        anchorSound = GetComponent<AudioSource>();

        // 기본 패널(조작 패널) 머티리얼 설정
        magenta.gameObject.GetComponentInChildren<MeshRenderer>().material = prismSelected;
    }

    // 조작 패널 오브젝트 선택
    public void MagentaObjectContacted()
    {
        if (magenta.isAttached == true)
        {
            selectedObject = Objects.Magenta;
            TryChange();
        }
    }

    // 시나리오 패널 오브젝트 선택
    public void RedObjectContacted()
    {
        if (red.isAttached == true)
        {
            selectedObject = Objects.Red;
            TryChange();
        }
    }

    // 미디어 패널 오브젝트 선택
    public void BlueObjectContacted()
    {
        if (blue.isAttached)
        {
            selectedObject = Objects.Blue;
            TryChange();
        }
    }

    // 오브젝트 선택 시 패널 변경 준비
    public void TryChange()
    {
        // 앵커에 결합된 패널 체크
        GameObject[] panels = GameObject.FindGameObjectsWithTag("UI");
        foreach(GameObject panel in panels)
        {
            if (panel.transform.parent == palmUIPivotAnchor.transform)
            {
                // 패널 숨김 처리
                HideMenu(panel.transform);
            }
        }

        // 결합되어있는 패널 머티리얼 설정
        GameObject[] dynamicUIs = GameObject.FindGameObjectsWithTag("DynamicUI");
        foreach(GameObject dynamicUI in dynamicUIs)
        {
            if (dynamicUI.GetComponent<AnchorableBehaviour>().isAttached)
            {
                // 패널 머티리얼 기본으로 설정
                dynamicUI.GetComponentInChildren<MeshRenderer>().material = prismDefault;
            }
        }

        SetHandMenu();
    }

    // 패널 크기 축소 및 조작 안되도록 처리
    private void HideMenu(Transform _panelTransform)
    {
        _panelTransform.localPosition = palmHidden.localPosition;
        _panelTransform.localRotation = palmHidden.localRotation;
        _panelTransform.localScale = palmHidden.localScale;
    }

    // 선택된 오브젝트 설정값 전달 및 머티리얼 변경
    private void SetHandMenu()
    {
        switch(selectedObject)
        {
            case Objects.Magenta:
            ChangeMenu(controlPanel, palmVisible);
            magenta.gameObject.GetComponentInChildren<MeshRenderer>().material = prismSelected;
            break;

            case Objects.Red:
            ChangeMenu(tempoaryPanel1, palmVisible);
            red.gameObject.GetComponentInChildren<MeshRenderer>().material = prismSelected;
            break;

            case Objects.Blue:
            ChangeMenu(tempoaryPanel2, palmVisible);
            blue.gameObject.GetComponentInChildren<MeshRenderer>().material = prismSelected;
            break;
        }
    }

    // 선택된 오브젝트에 연결된 패널 크기 변경
    private void ChangeMenu(Transform _applyTransform, Transform _targetTransform)
    {
        _applyTransform.localPosition = _targetTransform.localPosition;
        _applyTransform.localRotation = _targetTransform.localRotation;
        _applyTransform.localScale = _targetTransform.localScale;
    }
    
    /********************************************************************************/
    // 조작 패널 오브젝트 이벤트
    /********************************************************************************/
    // 오브젝트 앵커 결합
    public void AttachedToMagentaAnchor()
    {
        if (controlPanel.parent.gameObject == magentaWorkstationBase)
        {
            // 결합 시 조작 패널의 부모를 손바닥으로 설정
            controlPanel.SetParent(palmUIPivotAnchor.transform);
            // 조작 패널 확대(손바닥)
            ChangeMenu(controlPanel, palmVisible);
            // 애니메이션 처리
            transformTweenBehaviour.PlayForward();
            TryChange();
        }
    }
    // 오브젝트 앵커 분리
    public void DetachedToMagentaAnchor()
    {
        // 분리한 오브젝트 머티리얼 변경
        magenta.gameObject.GetComponentInChildren<MeshRenderer>().material = prismSelected;
        // 애니메이션 처리
        transformTweenBehaviour.PlayBackward();
        Invoke("SetParentToMagentaObject", 0.3f);
    }

    // 앵커 분리 시 조작 패널 위치 및 애니메이션 설정
    public void SetParentToMagentaObject()
    {
        // 조작 패널의 부모를 다이나믹 UI 오브젝트로 설정
        controlPanel.SetParent(magentaWorkstationBase.transform);
        // 조작 패널 확대 처리(앵커)
        ChangeMenu(controlPanel, AnchorVisible);
        // 애니메이션 처리
        transformTweenBehaviour.PlayForward();
        Invoke("AnchorChange", 0.3f);
    }

    /********************************************************************************/
    // 시나리오 패널 오브젝트 이벤트
    /********************************************************************************/
    // 오브젝트 앵커 결합
    public void AttachedToRedAnchor()
    {
        if (tempoaryPanel1.parent.gameObject == redWorkstationBase)
        {
            tempoaryPanel1.SetParent(palmUIPivotAnchor.transform);
            ChangeMenu(tempoaryPanel1, palmVisible);
            transformTweenBehaviour.PlayForward();
            TryChange();
        }
    }

    // 오브젝트 앵커 분리
    public void DetachedToRedAnchor()
    {
        red.gameObject.GetComponentInChildren<MeshRenderer>().material = prismSelected;
        transformTweenBehaviour.PlayBackward();
        Invoke("SetParentToRedObject", 0.3f);
    }

    // 앵커 분리 시 시나리오 패널 위치 및 애니메이션 설정
    public void SetParentToRedObject()
    {
        tempoaryPanel1.SetParent(redWorkstationBase.transform);
        ChangeMenu(tempoaryPanel1, AnchorVisible);
        transformTweenBehaviour.PlayForward();
        Invoke("AnchorChange", 0.3f);
    }

    /********************************************************************************/
    // 미디어 패널 오브젝트 이벤트
    /********************************************************************************/
    // 오브젝트 앵커 결합
    public void AttachedToBlueAnchor()
    {
        if (tempoaryPanel2.parent.gameObject == blueWorkstationBase)
        {
            tempoaryPanel2.SetParent(palmUIPivotAnchor.transform);
            ChangeMenu(tempoaryPanel2, palmVisible);
            transformTweenBehaviour.PlayForward();
            TryChange();
        }
    }

    // 오브젝트 앵커 분리
    public void DetachedToBlueAnchor()
    {
        blue.gameObject.GetComponentInChildren<MeshRenderer>().material = prismSelected;
        transformTweenBehaviour.PlayBackward();
        Invoke("SetParentToBlueObject", 0.3f);
    }

    // 앵커 분리 시 미디어 패널 위치 및 애니메이션 설정
    public void SetParentToBlueObject()
    {
        tempoaryPanel2.SetParent(blueWorkstationBase.transform);
        ChangeMenu(tempoaryPanel2, AnchorVisible);
        transformTweenBehaviour.PlayForward();
        Invoke("AnchorChange", 0.3f);
    }

    // 앵커 분리 시, 핸드메뉴 중 남은 패널이 존재하면 그 중 첫 번째 패널을 확대
    public void AnchorChange()
    {
        // 손바닥 위치 안의 패널 배열로 설정
        GameObject[] panels = GameObject.FindGameObjectsWithTag("UI");
        foreach(GameObject panel in panels)
        {
            if (panel.transform.parent == palmUIPivotAnchor.transform)
            {
                // 패널 숨김 처리
                HideMenu(panel.transform);
            }
        }

        // 손바닥 위치 안의 패널 개수가 0이 아닐 때 머티리얼 설정
        if (palmUIPivotAnchor.transform.childCount != 0)
        {
            if (panels[0].gameObject.name.Equals("Control Panel"))
            {
                magenta.gameObject.GetComponentInChildren<MeshRenderer>().material = prismSelected;
            }
            else if (panels[0].gameObject.name.Equals("Scenario Panel"))
            {
                red.gameObject.GetComponentInChildren<MeshRenderer>().material = prismSelected;
            }
            else if (panels[0].gameObject.name.Equals("Media Panel"))
            {
                blue.gameObject.GetComponentInChildren<MeshRenderer>().material = prismSelected;
            }
        }
    }

    // 조작 패널 분리 및 결합 시 양손 조작 여부 설정
    public void ControlPanelHover()
    {
        // 패널 손바닥 위치 시 왼손 조작 안되도록 설정
        InteractionButton[] buttons = controlPanel.GetComponentsInChildren<InteractionButton>();
        if (magenta.isAttached)
        {
            foreach(InteractionButton button in buttons)
            {
                button.ignoreHoverMode = IgnoreHoverMode.Left;
            }
        }
        // 패널 앵커 위치 시 왼손 조작 가능하도록 설정
        else
        {
            foreach(InteractionButton button in buttons)
            {
                button.ignoreHoverMode = IgnoreHoverMode.None;
            }
        }
    }

    // 시나리오 패널 분리 및 결합 시 양손 조작 여부 설정
    public void ScenarioPanelHover()
    {
        // 패널 손바닥 위치 시 왼손 조작 안되도록 설정
        InteractionButton[] buttons = tempoaryPanel1.GetComponentsInChildren<InteractionButton>();
        if (red.isAttached)
        {
            foreach(InteractionButton button in buttons)
            {
                button.ignoreHoverMode = IgnoreHoverMode.Left;
            }
        }
        // 패널 앵커 위치 시 왼손 조작 가능하도록 설정
        else
        {
            foreach(InteractionButton button in buttons)
            {
                button.ignoreHoverMode = IgnoreHoverMode.None;
            }
        }
    }

    // 시나리오 패널 분리 및 결합 시 양손 조작 여부 설정
    public void MediaPanelHover()
    {
        // 패널 손바닥 위치 시 왼손 조작 안되도록 설정
        InteractionButton[] buttons = tempoaryPanel2.GetComponentsInChildren<InteractionButton>();
        if (blue.isAttached)
        {
            foreach(InteractionButton button in buttons)
            {
                button.ignoreHoverMode = IgnoreHoverMode.Left;
            }
        }
        // 패널 앵커 위치 시 왼손 조작 가능하도록 설정
        else
        {
            foreach(InteractionButton button in buttons)
            {
                button.ignoreHoverMode = IgnoreHoverMode.None;
            }
        }
    }

    // 제스처 이벤트
    public void OnLeftFacingGrabbed()
    {
        // 분리된 다이나믹 UI 오브젝트를 찾아 배열로 설정
        GameObject[] dynamicObjects = GameObject.FindGameObjectsWithTag("DynamicUI");
        foreach (GameObject dynamicObject in dynamicObjects)
        {
            AnchorableBehaviour anchorableBehaviour = dynamicObject.GetComponent<AnchorableBehaviour>();
            if (!anchorableBehaviour.isAttached)
            {
            // 오브젝트가 앵커에 붙어있지 않을 때 자동으로 결합되도록 설정
                anchorableBehaviour.maxAnchorRange = 5.0f;
                anchorableBehaviour.TryAttach();
            }
            // 오브젝트가 앵커에 붙어있을 때 기본 설정
            if (anchorableBehaviour.isAttached)
            {
                anchorableBehaviour.maxAnchorRange = 0.3f;
            }
        }
    }

    // 오브젝트 터치 시 사운드 피드백
    public void AnchorContactSound()
    {
        if (isStaying == false)
        {
            AudioClip anchorClick = anchorSound.clip;
            anchorSound.PlayOneShot(anchorClick);
            isStaying = true;

            StartCoroutine("SoundDelay");
        }
    }

    // 사운드 연속해서 나는 예외처리 수정
    IEnumerator SoundDelay()
    {
        yield return new WaitForSeconds(0.7f);
        if (isStaying)
        {
            isStaying = !isStaying;
        }
    }
}