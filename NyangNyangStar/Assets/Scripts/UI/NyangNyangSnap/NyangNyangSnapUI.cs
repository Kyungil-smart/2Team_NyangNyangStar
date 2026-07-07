using Core.Managers;
using Data.LibrarySystem;
using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UI;
using UI.Base;
using UI.MergeBoard;
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

    private UIPopup _snackPopup;
    private UIPopup _toyPopup;

    [Header("사진촬영")]
    [SerializeField] private NyangNyangSnapPhotoFrameCapture _photoFrameCapture;

    [Tooltip("구도 계산 컴포넌트")]
    [SerializeField] private NyangNyangSnapCompositionCalculator _compositionCalculator;

    [Tooltip("촬영 기록 저장 컴포넌트")]
    [SerializeField] private NyangNyangSnapCaptureRecorder _captureRecorder;

    [Tooltip("포즈 데이터 SO")]
    [SerializeField] private NyangNyangSnapPoseSO _poseSO;

    [Tooltip("아이템 배치 컨트롤러")]
    [SerializeField] private NyangNyangSnapPlacementController _placementController;

    [Tooltip("고양이 랜덤 스폰 및 이동 컨트롤러")]
    [SerializeField] private NyangNyangSnapCatController _catController;

    [Tooltip("도구별 감지 범위를 가져올 Tool SO")]
    [SerializeField] private NyangNyangSnapToolSO _toolSO;

    [Tooltip("ToolSO의 ItemRange에 곱할 UI 거리 배율")]
    [Min(0.1f)]
    [SerializeField] private float _itemRangeScale = 10f;

    [Header("촬영 횟수 UI")]
    [Tooltip("남은 촬영 횟수 Text 이름")]
    [SerializeField] private string _captureCountTextName = "CaptureCountText";

    [Tooltip("남은 촬영 횟수 표시 TMP Text")]
    [SerializeField] private TMP_Text _captureCountText;

    [Header("데모 고양이 UI")]
    [Tooltip("PhotoFrame 안에 배치한 고양이 UI Image 오브젝트 이름")]
    [SerializeField] private string _catObjectName = "";

    private readonly NyangNyangSnapScoreCalculator _scoreCalculator = new();
    private readonly NyangNyangSnapTimingScoreCalculator _timingScoreCalculator = new();

    private GameObject _snapCatObject;
    private NyangNyangSnapSprite _sprite;
    private NyangNyangSnapStagePopupUI _stagePopup;
    private bool _isCapturing;

    [SerializeField] private NyangNyangSnapCatSpriteAnimator _catSpriteAnimator;
    [SerializeField] private DOTweenAnimation _photoButtonBlinkAnimation;

    public override void Init()
    {
        Bind<Button>(typeof(NyangNyangSnapButtons));

        _backButton = Get<Button>((int)NyangNyangSnapButtons.BackButton);
        _photoButton = Get<Button>((int)NyangNyangSnapButtons.PhotoButton);
        _settingsButton = Get<Button>((int)NyangNyangSnapButtons.SettingsButton);
        _snackPanelButton = Get<Button>((int)NyangNyangSnapButtons.SnackPanelButton);
        _toyPanelButton = Get<Button>((int)NyangNyangSnapButtons.ToyPanelButton);

        InitPopups();
        ResolveRuntimeDataSources();

        AutoAssignCaptureComponents();
        AutoAssignPlacementController();
        AutoAssignCatController();
        AutoAssignCaptureCountText();

        RegisterPlacementEvent();

        _sprite = GetComponent<NyangNyangSnapSprite>();
        _sprite.Init();

        // 냥냥스냅 진입 직후에는 고양이 이미지를 보여주지 않음
        SetSnapCatActive(false);
        UpdateCaptureCountText();
    }
    private void OnEnable()
    {
        if (_catSpriteAnimator == null)
            return;

        _catSpriteAnimator.OnFrameChanged += OnCatFrameChanged;
        _catSpriteAnimator.OnAnimationCompleted += OnCatAnimationCompleted;
    }

    private void InitPopups()
    {
        AddCloseNyangNyangSnapButton(_backButton);
        AddCapturePhotoButton(_photoButton);

        InitPopup(KeyContainer.Prefabs.SettingsPopupUI, _settingsButton);

        GameManager.UI.ShowPopupUI<UIPopup>(
            KeyContainer.Prefabs.NyangNyangSnapSnackPopupUI,
            onLoaded =>
            {
                _snackPopup = onLoaded;
                AddPopupButton(_snackPanelButton, onLoaded);
            },
            false
        );

        GameManager.UI.ShowPopupUI<UIPopup>(
            KeyContainer.Prefabs.NyangNyangSnapToyPopupUI,
            onLoaded =>
            {
                _toyPopup = onLoaded;
                AddPopupButton(_toyPanelButton, onLoaded);
            },
            false
        );
    }

    private void OnDisable()
    {
        if (_catSpriteAnimator != null)
        {
            _catSpriteAnimator.OnFrameChanged -= OnCatFrameChanged;
            _catSpriteAnimator.OnAnimationCompleted -= OnCatAnimationCompleted;
        }

        StopPhotoButtonBlink();

        if (_catController != null)
        {
            _catController.OnDestinationReached -= OnCatDestinationReached;
            _catController.StopInteraction();
        }

        SetSnapCatActive(false);
        ClearPlacedItem();
        _isCapturing = false;

        if (_placementController != null)
        {
            _placementController.OnItemPlaced -= OnPlacedItem;

            _placementController.OnToyAlertStarted -= OnToyAlertStarted;
            _placementController.OnToyAlertEnded -= OnToyAlertEnded;

            _placementController.OnSnackDragStarted -= OnSnackDragStarted;
            _placementController.OnSnackDragUpdated -= OnSnackDragUpdated;
            _placementController.OnSnackDragCanceled -= OnSnackDragCanceled;
            _placementController.OnPreviewMoved -= OnPreviewMoved;
        }
    }
    private void OnCatFrameChanged(CatAnimationType animationType, int frameIndex, int frameCount)
    {
        if (_catSpriteAnimator.IsOptimalCut)
        {
            _photoButtonBlinkAnimation?.DORestart();
            return;
        }

        StopPhotoButtonBlink();
    }
    private void OnCatAnimationCompleted(CatAnimationType animationType)
    {
        StopPhotoButtonBlink();
    }
    private void StopPhotoButtonBlink()
    {
        if (_photoButtonBlinkAnimation == null)
            return;

        _photoButtonBlinkAnimation.DOPause();
        _photoButtonBlinkAnimation.DORewind();
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
            GameManager.Audio.PlaySfx("Main_SFX_Touch");
            popup.gameObject.SetActive(true);
            PlayPopupOpenAnimation(popup);
        });
    }

    private void PlayPopupOpenAnimation(UIPopup popup)
    {
        if (popup == null) return;

        popup.PlayOpenAnimation();
    }

    private void AddCloseNyangNyangSnapButton(Button button)
    {
        if (button == null)
            return;

        button.onClick.AddListener(() =>
        {
            GameManager.Audio.PlaySfx("Main_SFX_Touch");

            // 간식 패널이 열려 있으면 비활성화
            if (_snackPopup != null && _snackPopup.gameObject.activeSelf)
            {
                _snackPopup.gameObject.SetActive(false);
            }

            // 장난감 패널이 열려 있으면 비활성화
            if (_toyPopup != null && _toyPopup.gameObject.activeSelf)
            {
                _toyPopup.gameObject.SetActive(false);
            }

            gameObject.SetActive(false);
        });
    }

    private void AddCapturePhotoButton(Button button)
    {
        if (button == null) return;


        button.onClick.RemoveListener(OnClickPhotoButton);
        button.onClick.AddListener(() =>
        {
            GameManager.Audio.PlaySfx("Main_SFX_Touch");
            OnClickPhotoButton();
        });
    }

    private void OnClickPhotoButton()
    {
        ResolveRuntimeDataSources();

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

        if (_captureRecorder.IsCaptureComplete)
        {
            DebugTool.Log("[NyangNyangSnapUI] 이미 최대 촬영 횟수에 도달했습니다.", DebugType.UI, this);
            SetPhotoButtonInteractable(false);
            UpdateCaptureCountText();
            return;
        }
        NyangNyangSnapPoseData poseData = GetPoseByPlacedItemOrNull();

        // 캡처 완료 시점이 아니라 촬영 버튼을 누른 순간의 점수를 저장합니다.
        int timingScore = _timingScoreCalculator.Calculate(_catSpriteAnimator);

        // 마커를 숨기기 전에 현재 위치로 구도 점수를 먼저 계산합니다.
        float compositionRate =
            _compositionCalculator.CalculateCompositionRate();

        _isCapturing = true;
        SetPhotoButtonInteractable(false);

        // 촬영 전 범위 이미지의 활성 상태를 저장합니다.
        AutoAssignPlacementController();

        bool wasRangeImageVisible =
            _placementController != null &&
            _placementController.IsRangeImageVisible;

        // RenderTexture 촬영 결과에 Center와 아이템 범위 이미지가
        // 나오지 않도록 촬영 중에만 숨깁니다.
        _compositionCalculator.SetTargetImageVisible(false);

        if (_placementController != null)
            _placementController.SetRangeImageVisible(false);

        _photoFrameCapture.CapturePhoto(capturedSprite =>
        {
            // 캡처 성공 여부와 관계없이 Center와 범위 이미지를
            // 촬영 전 활성 상태로 복구합니다.
            _compositionCalculator.SetTargetImageVisible(true);

            if (_placementController != null)
            {
                _placementController.SetRangeImageVisible(
                    wasRangeImageVisible
                );
            }

            _isCapturing = false;

            if (capturedSprite == null)
            {
                DebugTool.Warning(
                    "[NyangNyangSnapUI] 사진 캡처 실패",
                    DebugType.UI,
                    this
                );

                SetPhotoButtonInteractable(true);
                return;
            }

            if (_captureRecorder.IsCaptureComplete)
            {
                DebugTool.Log(
                    "[NyangNyangSnapUI] 최대 촬영 횟수 이후 들어온 캡처 결과는 무시합니다.",
                    DebugType.UI,
                    this
                );

                Destroy(capturedSprite.texture);
                Destroy(capturedSprite);

                SetPhotoButtonInteractable(false);
                UpdateCaptureCountText();
                return;
            }

            int backgroundScore =
                _sprite.CurrentBackgroundData != null
                    ? _sprite.CurrentBackgroundData.Score
                    : 0;

            NyangNyangSnapScoreResult scoreResult = _scoreCalculator.Calculate(
                poseData,
                compositionRate,
                backgroundScore,
                timingScore
            );

            _captureRecorder.AddRecord(
                capturedSprite,
                poseData,
                scoreResult
            );

            UpdateCaptureCountText();
            // 촬영 1회 완료 후 배치된 아이템은 제거합니다.
            // 아이템이 없는 상태로 촬영한 경우에는 제거할 대상이 없습니다.
            if (_placementController != null && _placementController.HasPlacedItem)
            {
                ClearPlacedItem();

                DebugTool.Log(
                    "[NyangNyangSnapUI] 촬영 1회 완료로 배치 아이템 제거",
                    DebugType.UI,
                    this
                );
            }

            string poseName = poseData != null ? poseData.PoseName : "아이템 없음";

            DebugTool.Log(
                $"[NyangNyangSnapUI] 촬영 완료 " +
                $"({_captureRecorder.CurrentCaptureCount}/{_captureRecorder.MaxCaptureCount}) / " +
                $"포즈: {poseName} / " +
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

    private void AutoAssignPlacementController()
    {
        if (_placementController != null)
            return;

        _placementController = GetComponent<NyangNyangSnapPlacementController>();

        if (_placementController == null)
        {
            _placementController = FindFirstObjectByType<NyangNyangSnapPlacementController>();
        }

        if (_placementController == null)
        {
            DebugTool.Warning("[NyangNyangSnapUI] PlacementController를 찾지 못했습니다.", DebugType.UI, this);
            return;
        }

        DebugTool.Log("[NyangNyangSnapUI] PlacementController 자동 연결 완료", DebugType.UI, this);
    }
    private void AutoAssignCatController()
    {
        if (_catController != null)
            return;

        AutoAssignSnapCatObject();

        if (_snapCatObject != null)
        {
            _catController = _snapCatObject.GetComponent<NyangNyangSnapCatController>();
        }

        if (_catController == null)
        {
            _catController = GetComponentInChildren<NyangNyangSnapCatController>(true);
        }

        if (_catController == null)
        {
            DebugTool.Warning(
                "[NyangNyangSnapUI] NyangNyangSnapCatController를 찾지 못했습니다.",
                DebugType.UI,
                this
            );
            return;
        }

        DebugTool.Log(
            "[NyangNyangSnapUI] CatController 자동 연결 완료",
            DebugType.UI,
            this
        );
    }
    private void ClearPlacedItem()
    {
        AutoAssignPlacementController();

        if (_placementController == null)
            return;

        _placementController.ClearPlacedItem();
        _placementController.CancelSelection();

        DebugTool.Log("[NyangNyangSnapUI] 배치 아이템 초기화 완료", DebugType.UI, this);
    }

    private NyangNyangSnapPoseData GetPoseByPlacedItemOrNull()
    {
        ResolveRuntimeDataSources();
        AutoAssignPlacementController();

        if (_placementController == null)
        {
            DebugTool.Log(
                "[NyangNyangSnapUI] PlacementController가 없어 아이템 없는 촬영으로 처리합니다.",
                DebugType.UI,
                this
            );

            return null;
        }

        if (!_placementController.HasPlacedItem)
        {
            DebugTool.Log(
                "[NyangNyangSnapUI] 배치 아이템 없음 - 포즈 점수 0점 촬영",
                DebugType.UI,
                this
            );

            return null;
        }

        if (_poseSO == null)
        {
            DebugTool.Warning(
                "[NyangNyangSnapUI] PoseSO가 연결되지 않았습니다. 포즈 점수는 0점 처리됩니다.",
                DebugType.UI,
                this
            );

            return null;
        }

        int itemID = _placementController.SelectedItemID;

        List<NyangNyangSnapPoseData> poseList = _poseSO.GetPoseDataByTool(itemID);

        if (poseList == null || poseList.Count == 0)
        {
            DebugTool.Warning(
                $"[NyangNyangSnapUI] 배치된 아이템에 연결된 포즈가 없습니다. 포즈 점수는 0점 처리됩니다. ItemID:{itemID}",
                DebugType.UI,
                this
            );

            return null;
        }

        NyangNyangSnapPoseData poseData = poseList[0];

        DebugTool.Log(
            $"[NyangNyangSnapUI] 배치 아이템 기준 포즈 선택 완료 / ItemID:{itemID}, Pose:{poseData.PoseName}",
            DebugType.UI,
            this
        );

        return poseData;
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

                if (_snackPopup != null)
                {
                    _snackPopup.gameObject.SetActive(false);
                }
                if (_toyPopup != null)
                {
                    _toyPopup.gameObject.SetActive(false);
                }

                // 결과창으로 넘어갔으므로 냥냥스냅 Canvas 비활성화
                gameObject.SetActive(false);

            }
        );
    }
    private void AutoAssignCaptureComponents()
    {
        if (_photoFrameCapture == null)
        {
            _photoFrameCapture = GetComponent<NyangNyangSnapPhotoFrameCapture>();

        }

        if (_compositionCalculator == null)
        {
            _compositionCalculator = GetComponent<NyangNyangSnapCompositionCalculator>();

        }

        if (_captureRecorder == null)
        {
            _captureRecorder = GetComponent<NyangNyangSnapCaptureRecorder>();

        }
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

    private void RegisterPlacementEvent()
    {
        AutoAssignPlacementController();

        if (_placementController == null)
            return;

        _placementController.OnItemPlaced -= OnPlacedItem;
        _placementController.OnItemPlaced += OnPlacedItem;

        _placementController.OnToyAlertStarted -= OnToyAlertStarted;
        _placementController.OnToyAlertStarted += OnToyAlertStarted;

        _placementController.OnToyAlertEnded -= OnToyAlertEnded;
        _placementController.OnToyAlertEnded += OnToyAlertEnded;

        _placementController.OnSnackDragStarted -= OnSnackDragStarted;
        _placementController.OnSnackDragStarted += OnSnackDragStarted;
        _placementController.OnSnackDragUpdated -= OnSnackDragUpdated;
        _placementController.OnSnackDragUpdated += OnSnackDragUpdated;

        _placementController.OnSnackDragCanceled -= OnSnackDragCanceled;
        _placementController.OnSnackDragCanceled += OnSnackDragCanceled;

        _placementController.OnPreviewMoved -= OnPreviewMoved;
        _placementController.OnPreviewMoved += OnPreviewMoved;

        AutoAssignCatController();

        if (_catController != null)
        {
            _catController.OnDestinationReached -= OnCatDestinationReached;
            _catController.OnDestinationReached += OnCatDestinationReached;
        }

        DebugTool.Log("[NyangNyangSnapUI] 아이템 배치 및 간식 드래그 이벤트 연결 완료", DebugType.UI, this);
    }
    private void OnToyAlertStarted()
    {
        AutoAssignCatController();

        if (_catController == null)
            return;

        _catController.BeginAlert();
    }
    private void OnToyAlertEnded()
    {
        if (_catController == null)
            return;

        _catController.EndAlert();
    }
    private async void OnPlacedItem(int itemID)
    {
        if (itemID <= 0)
        {
            DebugTool.Warning(
                $"[NyangNyangSnapUI] 소비할 ItemID가 잘못되었습니다. " +
                $"ItemID:{itemID}",
                DebugType.UI,
                this
            );

            ClearPlacedItem();
            return;
        }

        if (MergeBoardItemService.Instance == null)
        {
            DebugTool.Warning(
                $"[NyangNyangSnapUI] MergeBoardItemService.Instance가 없습니다. " +
                $"ItemID:{itemID}",
                DebugType.UI,
                this
            );

            ClearPlacedItem();
            return;
        }

        bool consumed =
            await MergeBoardItemService.Instance
                .ConsumeItemByIdAsync(itemID, 1);

        if (!consumed)
        {
            DebugTool.Warning(
                $"[NyangNyangSnapUI] 배치 아이템 소비 실패 / " +
                $"ItemID:{itemID}",
                DebugType.UI,
                this
            );

            ClearPlacedItem();
            return;
        }

        DebugTool.Log(
            $"[NyangNyangSnapUI] 배치 아이템 소비 완료 / " +
            $"ItemID:{itemID}",
            DebugType.UI,
            this
        );
        if (!_placementController.LastPlacementWasSnack)
            TryMoveCatToPlacedItem(itemID);
    }
    private void OnPreviewMoved(RectTransform previewRectTransform)
    {
        AutoAssignCatController();

        if (_catController == null)
            return;

        _catController.LookAtTarget(
            previewRectTransform
        );
    }
    private void OnSnackDragStarted(int itemID)
    {
        AutoAssignCatController();

        if (_catController == null)
            return;

        _catController.BeginAlert();

        DebugTool.Log(
            $"[NyangNyangSnapUI] 간식 드래그 시작으로 ALERT 상태 전환 / ItemID:{itemID}",
            DebugType.UI,
            this
        );
    }

    private void OnSnackDragUpdated(int itemID, RectTransform snackRectTransform, float itemRange)
    {
        AutoAssignCatController();
        AutoAssignPlacementController();

        if (_catController == null || _placementController == null)
            return;

        if (_catController.IsMoving)
            return;

        bool startedMove = _catController.TryMoveToTool(
            itemID,
            snackRectTransform,
            itemRange
        );

        if (!startedMove)
            return;

        _placementController.LockSnackDrag();

        DebugTool.Log(
            $"[NyangNyangSnapUI] 고양이가 간식 효과 범위에 들어와 이동 시작 / ItemID:{itemID}",
            DebugType.UI,
            this
        );
    }

    private void OnCatDestinationReached(int itemID)
    {
        if (_placementController == null || !_placementController.IsSnackWaitingForCat)
            return;

        _catController.EnterEating(itemID);

        bool completed = _placementController.CompleteSnackDrag(itemID);

        DebugTool.Log(
            $"[NyangNyangSnapUI] 고양이 도착 후 간식 사용 완료 처리 / ItemID:{itemID}, Complete:{completed}",
            DebugType.UI,
            this
        );
    }

    private void OnSnackDragCanceled()
    {
        if (_catController != null && !_catController.IsEating)
            _catController.StopInteraction();
    }
    private void TryMoveCatToPlacedItem(int itemID)
    {
        ResolveRuntimeDataSources();
        AutoAssignCatController();
        AutoAssignPlacementController();

        if (_catController == null)
        {
            DebugTool.Warning(
                "[NyangNyangSnapUI] CatController가 없습니다.",
                DebugType.UI,
                this
            );
            return;
        }

        if (_placementController == null ||
            _placementController.PlacedItemRectTransform == null)
        {
            DebugTool.Warning(
                "[NyangNyangSnapUI] 배치된 도구 위치를 찾지 못했습니다.",
                DebugType.UI,
                this
            );
            return;
        }

        if (_toolSO == null)
        {
            DebugTool.Warning(
                "[NyangNyangSnapUI] ToolSO가 연결되지 않았습니다.",
                DebugType.UI,
                this
            );
            return;
        }

        if (!_toolSO.TryGetToolDataByItemID(
                itemID,
                out NyangNyangSnapToolData toolData))
        {
            DebugTool.Warning(
                $"[NyangNyangSnapUI] ToolSO에서 도구 데이터를 찾지 못했습니다. " +
                $"ItemID:{itemID}",
                DebugType.UI,
                this
            );
            return;
        }

        float itemRange =
            toolData.ItemRange * _itemRangeScale;

        bool startedMove = _catController.TryMoveToTool(
            itemID,
            _placementController.PlacedItemRectTransform,
            itemRange
        );

        DebugTool.Log(
            $"[NyangNyangSnapUI] 고양이 도구 이동 검사 완료 / " +
            $"ItemID:{itemID}, " +
            $"Range:{itemRange:F1}, " +
            $"Move:{startedMove}",
            DebugType.UI,
            this
        );
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
        if (_snapCatObject != null)
            return;

        Transform catTransform =
            FindTransformByNameInCanvas(_catObjectName);

        if (catTransform == null)
        {
            DebugTool.Warning(
                $"[NyangNyangSnapUI] 고양이 오브젝트 자동 연결 실패. " +
                $"이름: {_catObjectName}",
                DebugType.UI,
                this
            );
            return;
        }

        _snapCatObject = catTransform.gameObject;

        DebugTool.Log(
            $"[NyangNyangSnapUI] 고양이 오브젝트 자동 연결 완료: " +
            $"{_snapCatObject.name}",
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

        RegisterPlacementEvent();
        ResolveRuntimeDataSources();

        _sprite.SetBackground(stage);
        _startPanel.SetActive(true);
        _startButton.SetActive(true);

        // OpenPopup 시점에는 아직 촬영 시작 전이므로 고양이 이미지는 숨김
        SetSnapCatActive(false);

        ClearPlacedItem();

        AutoAssignCaptureComponents();

        if (_compositionCalculator != null)
        {
            _compositionCalculator.SetTargetVisible(false);
        }

        if (_captureRecorder != null)
        {
            _captureRecorder.ClearRecords();
        }

        _isCapturing = false;
        SetPhotoButtonInteractable(false);
        UpdateCaptureCountText();
    }

    public void RetrySnap()
    {
        gameObject.SetActive(true);

        RegisterPlacementEvent();
        ResolveRuntimeDataSources();

        _startPanel.SetActive(true);
        _startButton.SetActive(true);

        SetSnapCatActive(false);

        ClearPlacedItem();

        AutoAssignCaptureComponents();

        if (_compositionCalculator != null)
        {
            _compositionCalculator.SetTargetVisible(false);
        }

        if (_captureRecorder != null)
        {
            _captureRecorder.ClearRecords();

        }

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

        ClearPlacedItem();

        gameObject.SetActive(false);
    }

    public void StartSnapCat()
    {
        RegisterPlacementEvent();
        ResolveRuntimeDataSources();

        SetSnapCatActive(true);

        AutoAssignCatController();

        if (_catController != null)
        {
            _catController.SetRandomPosition();

        }
        AutoAssignCaptureComponents();

        if (_compositionCalculator != null)
        {
            _compositionCalculator.RandomizeTargetPoint();
        }

        if (_captureRecorder != null)
        {
            _captureRecorder.ClearRecords();

        }

        _isCapturing = false;
        SetPhotoButtonInteractable(true);
        UpdateCaptureCountText();

        DebugTool.Log(
            "[NyangNyangSnapUI] 촬영 시작 - 고양이 이미지 활성화",
            DebugType.UI,
            this
        );
    }

    public void StopSnapCat()
    {
        if (_catController != null)
        {
            _catController.StopInteraction();
        }

        if (_compositionCalculator != null)
        {
            _compositionCalculator.SetTargetVisible(false);
        }

        SetSnapCatActive(false);
        _isCapturing = false;
        SetPhotoButtonInteractable(false);

        DebugTool.Log(
            "[NyangNyangSnapUI] 촬영 종료 - 고양이 이미지 비활성화",
            DebugType.UI,
            this
        );
    }

    private void ResolveRuntimeDataSources()
    {
        if (LocalDataAccess.Instance == null || LocalDataAccess.Instance.Game == null)
            return;

        if (LocalDataAccess.Instance.Game.TryGetNyangNyangSnapPoseSO(out NyangNyangSnapPoseSO loadedPoseSO) &&
            loadedPoseSO != null)
        {
            _poseSO = loadedPoseSO;
        }

        if (LocalDataAccess.Instance.Game.TryGetNyangNyangSnapToolSO(out NyangNyangSnapToolSO loadedToolSO) &&
            loadedToolSO != null)
        {
            _toolSO = loadedToolSO;
        }
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
