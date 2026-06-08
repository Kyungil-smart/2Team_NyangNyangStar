using Core.Managers;
using TMPro;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;
using Util;

public class NyangNyangSnapUI : UIPopup
{
    [Tooltip("시작 패널")][SerializeField] private GameObject _startPanel;
    [Tooltip("시작 버튼")][SerializeField] private GameObject _startButton;

    [Header("버튼")]
    [Tooltip("뒤로가기 버튼")][SerializeField] private Button _backButton;
    [Tooltip("사진 버튼")][SerializeField] private Button _photoButton;
    [Tooltip("세팅 버튼")][SerializeField] private Button _settingsButton;
    [Tooltip("간식 패널 버튼")][SerializeField] private Button _snackPanelButton;
    [Tooltip("장난감 패널 버튼")][SerializeField] private Button _toyPanelButton;

    [Header("사진촬영")]
    [SerializeField] private NyangNyangSnapPhotoFrameCapture _photoFrameCapture;

    [Tooltip("구도 계산 컴포넌트")]
    [SerializeField] private NyangNyangSnapCompositionCalculator _compositionCalculator;

    [Tooltip("촬영 기록 저장 컴포넌트")]
    [SerializeField] private NyangNyangSnapCaptureRecorder _captureRecorder;

    [Tooltip("포즈 데이터 SO")]
    [SerializeField] private NyangNyangSnapPoseSO _poseSO;

    [Tooltip("데모용 반응 점수 비율")]
    [Range(0f, 1f)]
    [SerializeField] private float _reactionRate = 0.6f;

    [Header("촬영 횟수 UI")]
    [Tooltip("남은 촬영 횟수 Text 이름")]
    [SerializeField] private string _captureCountTextName = "CaptureCountText";

    [Tooltip("남은 촬영 횟수 표시 TMP Text")]
    [SerializeField] private TMP_Text _captureCountText;

    [Header("데모 고양이 UI")]
    [Tooltip("PhotoFrame 안에 배치한 고양이 UI Image 오브젝트 이름")]
    [SerializeField] private string _catObjectName = "goods 1";

    [Header("결과 UI")]
    [Tooltip("결과 팝업 Addressables Key")]
    [SerializeField] private string _resultPopupKey = "NyangNyangSnapResultCanvas";

    private readonly NyangNyangSnapScoreCalculator _scoreCalculator = new();

    private GameObject _snapCatObject;
    private NyangNyangSnapSprite _sprite;
    private NyangNyangSnapStagePopupUI _stagePopup;
    private bool _isCapturing;

    public override void Init()
    {
        Bind<Button>(typeof(NyangNyangSnapButtons));

        _backButton = Get<Button>((int)NyangNyangSnapButtons.BackButton);
        _photoButton = Get<Button>((int)NyangNyangSnapButtons.PhotoButton);
        _settingsButton = Get<Button>((int)NyangNyangSnapButtons.SettingsButton);
        _snackPanelButton = Get<Button>((int)NyangNyangSnapButtons.SnackPanelButton);
        _toyPanelButton = Get<Button>((int)NyangNyangSnapButtons.ToyPanelButton);

        InitPopups();
        AutoAssignCaptureComponents();
        AutoAssignCaptureCountText();

        _sprite = GetComponent<NyangNyangSnapSprite>();
        _sprite.Init();

        // 냥냥스냅 진입 직후에는 고양이 이미지를 보여주지 않음
        SetSnapCatActive(false);
        UpdateCaptureCountText();
    }

    private void InitPopups()
    {
        //InitPopup(KeyContainer.Prefabs.NyangNyangSnapPopupUI, _photoButton);
        AddCloseNyangNyangSnapButton(_backButton);
        AddCapturePhotoButton(_photoButton);

        InitPopup(KeyContainer.Prefabs.SettingsPopupUI, _settingsButton);
        InitPopup(KeyContainer.Prefabs.NyangNyangSnapSnackPopupUI, _snackPanelButton);
        InitPopup(KeyContainer.Prefabs.NyangNyangSnapToyPopupUI, _toyPanelButton);

    }

    private void OnDisable()
    {
        // 냥냥스냅을 닫으면 촬영용 고양이 이미지도 숨김
        SetSnapCatActive(false);
        _isCapturing = false;

        //RemovePopupButton(_photoButton);
        //RemovePopupButton(_backButton);
        //RemovePopupButton(_settingsButton);
        //RemovePopupButton(_snackPanelButton);
        //RemovePopupButton(_toyPanelButton);
    }

    private void InitPopup(string key, Button button)
    {
        GameManager.UI.ShowPopupUI<UIPopup>(key, onLoaded => AddPopupButton(button, onLoaded), false);
    }

    private void AddPopupButton(Button button, UIPopup popup)
    {
        if (button == null) return;

        button.onClick.AddListener(() =>
        {
            popup.gameObject.SetActive(true);
            PlayPopupOpenAnimation(popup);
        });
    }

    private void RemovePopupButton(Button button)
    {
        if (button == null) return;

        button.onClick.RemoveAllListeners();
    }

    private void PlayPopupOpenAnimation(UIPopup popup)
    {
        if (popup == null) return;

        popup.PlayOpenAnimation();
    }

    private void AddCloseNyangNyangSnapButton(Button button)
    {
        if (button == null) return;

        button.onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
        });
    }

    private void AddCapturePhotoButton(Button button)
    {
        if (button == null) return;

        button.onClick.RemoveListener(OnClickPhotoButton);
        button.onClick.AddListener(OnClickPhotoButton);
    }

    private void OnClickPhotoButton()
    {
        if (_isCapturing)
        {
            DebugTool.Log("[NyangNyangSnapUI] 현재 사진 캡처 중입니다.", DebugType.UI, this);
            return;
        }

        AutoAssignCaptureComponents();

        if (_photoFrameCapture == null)
        {
            DebugTool.Warning("[NyangNyangSnapUI] PhotoFrameCapture가 없습니다.", DebugType.UI, this);
            return;
        }

        if (_compositionCalculator == null)
        {
            DebugTool.Warning("[NyangNyangSnapUI] CompositionCalculator가 없습니다.", DebugType.UI, this);
            return;
        }

        if (_captureRecorder == null)
        {
            DebugTool.Warning("[NyangNyangSnapUI] CaptureRecorder가 없습니다.", DebugType.UI, this);
            return;
        }

        if (_poseSO == null)
        {
            DebugTool.Warning("[NyangNyangSnapUI] PoseSO가 연결되지 않았습니다.", DebugType.UI, this);
            return;
        }

        if (_captureRecorder.IsCaptureComplete)
        {
            DebugTool.Log("[NyangNyangSnapUI] 이미 최대 촬영 횟수에 도달했습니다.", DebugType.UI, this);
            SetPhotoButtonInteractable(false);
            UpdateCaptureCountText();
            return;
        }

        _isCapturing = true;
        SetPhotoButtonInteractable(false);

        _photoFrameCapture.CapturePhoto(capturedSprite =>
        {
            _isCapturing = false;

            if (capturedSprite == null)
            {
                DebugTool.Warning("[NyangNyangSnapUI] 사진 캡처 실패", DebugType.UI, this);
                SetPhotoButtonInteractable(true);
                return;
            }

            if (_captureRecorder.IsCaptureComplete)
            {
                DebugTool.Log("[NyangNyangSnapUI] 최대 촬영 횟수 이후 들어온 캡처 결과는 무시합니다.", DebugType.UI, this);
                Destroy(capturedSprite.texture);
                Destroy(capturedSprite);

                SetPhotoButtonInteractable(false);
                UpdateCaptureCountText();
                return;
            }

            NyangNyangSnapPoseData randomPoseData = _poseSO.GetRandomPoseData();

            if (randomPoseData == null)
            {
                DebugTool.Warning("[NyangNyangSnapUI] 랜덤 포즈 데이터를 가져오지 못했습니다.", DebugType.UI, this);
                SetPhotoButtonInteractable(true);
                return;
            }

            float compositionRate = _compositionCalculator.CalculateCompositionRate();

            NyangNyangSnapScoreResult scoreResult = _scoreCalculator.Calculate(
                randomPoseData,
                compositionRate,
                _reactionRate
            );

            _captureRecorder.AddRecord(
                capturedSprite,
                randomPoseData,
                scoreResult
            );

            UpdateCaptureCountText();

            DebugTool.Log(
                $"[NyangNyangSnapUI] 촬영 완료 " +
                $"({_captureRecorder.CurrentCaptureCount}/{_captureRecorder.MaxCaptureCount}) / " +
                $"포즈: {randomPoseData.PoseName} / " +
                $"총점: {scoreResult.TotalScore}",
                DebugType.UI,
                this
            );

            if (_captureRecorder.IsCaptureComplete)
            {
                DebugTool.Log(
                    $"[NyangNyangSnapUI] 10장 촬영 완료 / 최고 점수: {_captureRecorder.BestRecord.TotalScore}",
                    DebugType.UI,
                    this
                );

                SetPhotoButtonInteractable(false);
                StopSnapCat();
                OpenResultUI(_captureRecorder.BestRecord);
                return;
            }

            SetPhotoButtonInteractable(true);
        });
    }
    private void OpenResultUI(NyangNyangSnapCaptureRecord bestRecord)
    {
        if (bestRecord == null)
        {
            DebugTool.Warning("[NyangNyangSnapUI] 결과로 넘길 BestRecord가 없습니다.", DebugType.UI, this);
            return;
        }

        GameManager.UI.ShowPopupUI<NyangNyangSnapResultUI>(
            KeyContainer.Prefabs.NyangNyangSnapResultPopupUI,
            resultUI =>
            {
                if (resultUI == null)
                {
                    DebugTool.Warning("[NyangNyangSnapUI] ResultUI 로드 실패", DebugType.UI, this);
                    return;
                }

                resultUI.SetSnapUI(this);
                resultUI.gameObject.SetActive(true);
                resultUI.SetResult(bestRecord, _captureRecorder.Records);
                resultUI.PlayOpenAnimation();

                DebugTool.Log(
                    $"[NyangNyangSnapUI] 결과 UI 열기 완료 / 최고 점수: {bestRecord.TotalScore}",
                    DebugType.UI,
                    this
                );

                // 결과창으로 넘어갔으므로 냥냥스냅 Canvas 비활성화
                gameObject.SetActive(false);
            }
        );
    }
    private void AutoAssignCaptureComponents()
    {
        if (_photoFrameCapture == null)
            _photoFrameCapture = GetComponent<NyangNyangSnapPhotoFrameCapture>();

        if (_compositionCalculator == null)
            _compositionCalculator = GetComponent<NyangNyangSnapCompositionCalculator>();

        if (_captureRecorder == null)
            _captureRecorder = GetComponent<NyangNyangSnapCaptureRecorder>();
    }

    private void UpdateCaptureCountText()
    {
        AutoAssignCaptureComponents();
        AutoAssignCaptureCountText();

        if (_captureCountText == null)
        {
            DebugTool.Warning("[NyangNyangSnapUI] CaptureCountText가 없습니다.", DebugType.UI, this);
            return;
        }

        if (_captureRecorder == null)
        {
            DebugTool.Warning("[NyangNyangSnapUI] CaptureRecorder가 없습니다.", DebugType.UI, this);
            return;
        }

        int remainingCount = Mathf.Max(
            0,
            _captureRecorder.MaxCaptureCount - _captureRecorder.CurrentCaptureCount
        );

        _captureCountText.text = remainingCount.ToString();

        DebugTool.Log(
            $"[NyangNyangSnapUI] 남은 촬영 횟수 갱신: {remainingCount}",
            DebugType.UI,
            this
        );
    }

    private void AutoAssignCaptureCountText()
    {
        if (_captureCountText != null) return;

        Transform target = FindTransformByNameInCanvas(_captureCountTextName);

        if (target == null)
        {
            DebugTool.Warning($"[NyangNyangSnapUI] 촬영 횟수 Text를 찾지 못했습니다. 이름: {_captureCountTextName}", DebugType.UI, this);
            return;
        }

        _captureCountText = target.GetComponent<TMP_Text>();

        if (_captureCountText == null)
        {
            DebugTool.Warning($"[NyangNyangSnapUI] TMP_Text 컴포넌트가 없습니다. 이름: {_captureCountTextName}", DebugType.UI, this);
            return;
        }

        DebugTool.Log($"[NyangNyangSnapUI] 촬영 횟수 Text 자동 연결 완료: {_captureCountText.name}", DebugType.UI, this);
    }

    private void SetPhotoButtonInteractable(bool isInteractable)
    {
        if (_photoButton == null) return;

        _photoButton.interactable = isInteractable;
    }

    private void SetSnapCatActive(bool isActive)
    {
        AutoAssignSnapCatObject();

        if (_snapCatObject == null)
        {
            DebugTool.Warning($"[NyangNyangSnapUI] 고양이 오브젝트를 찾지 못했습니다. 이름: {_catObjectName}", DebugType.UI, this);
            return;
        }

        _snapCatObject.SetActive(isActive);

        DebugTool.Log(
            $"[NyangNyangSnapUI] 촬영용 고양이 오브젝트 활성화 상태 변경: {isActive}",
            DebugType.UI,
            this
        );
    }

    private void AutoAssignSnapCatObject()
    {
        if (_snapCatObject != null) return;

        Transform catTransform = FindTransformByNameInCanvas(_catObjectName);

        if (catTransform == null)
        {
            DebugTool.Warning($"[NyangNyangSnapUI] 고양이 오브젝트 자동 연결 실패. 이름: {_catObjectName}", DebugType.UI, this);
            return;
        }

        _snapCatObject = catTransform.gameObject;

        DebugTool.Log(
            $"[NyangNyangSnapUI] 고양이 오브젝트 자동 연결 완료: {_snapCatObject.name}",
            DebugType.UI,
            this
        );
    }

    private Transform FindTransformByNameInCanvas(string objectName)
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        Transform searchRoot = canvas != null ? canvas.transform : transform;

        Transform[] children = searchRoot.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child.name == objectName)
                return child;
        }

        return null;
    }

    public void OpenPopup(int stage)
    {
        gameObject.SetActive(true);

        _sprite.SetBackground(stage);
        _startPanel.SetActive(true);
        _startButton.SetActive(true);

        // OpenPopup 시점에는 아직 촬영 시작 전이므로 고양이 이미지는 숨김
        SetSnapCatActive(false);

        AutoAssignCaptureComponents();

        if (_captureRecorder != null)
            _captureRecorder.ClearRecords();

        _isCapturing = false;
        SetPhotoButtonInteractable(false);
        UpdateCaptureCountText();
    }

    public void RetrySnap()
    {
        gameObject.SetActive(true);

        _startPanel.SetActive(true);
        _startButton.SetActive(true);

        SetSnapCatActive(false);

        AutoAssignCaptureComponents();

        if (_captureRecorder != null)
            _captureRecorder.ClearRecords();

        _isCapturing = false;
        SetPhotoButtonInteractable(false);
        UpdateCaptureCountText();
    }

    public void SetStagePopup(NyangNyangSnapStagePopupUI stagePopup)
    {
        _stagePopup = stagePopup;
    }

    public void BackToMain()
    {
        _stagePopup.gameObject.SetActive(false);

        gameObject.SetActive(false);
    }

    public void StartSnapCat()
    {
        SetSnapCatActive(true);

        AutoAssignCaptureComponents();

        if (_captureRecorder != null)
            _captureRecorder.ClearRecords();

        _isCapturing = false;
        SetPhotoButtonInteractable(true);
        UpdateCaptureCountText();

        DebugTool.Log("[NyangNyangSnapUI] 촬영 시작 - 고양이 이미지 활성화", DebugType.UI, this);
    }

    public void StopSnapCat()
    {
        SetSnapCatActive(false);
        _isCapturing = false;
        SetPhotoButtonInteractable(false);

        DebugTool.Log("[NyangNyangSnapUI] 촬영 종료 - 고양이 이미지 비활성화", DebugType.UI, this);
    }
}

public enum NyangNyangSnapButtons
{
    BackButton,
    PhotoButton,
    SettingsButton,
    SnackPanelButton,
    ToyPanelButton
}