using Core.Managers;
using DG.Tweening;
using Firebase.Firestore;
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class NyangNyangSnapResultUI : UIPopup
{
    [Header("사진 SO")]
    [SerializeField] private NyangNyangSnapPhotoAlbumSO _photoAlbumSO;

    [Header("결과/보상 패널")]
    [SerializeField] private GameObject _resultCollectionPanel;
    [SerializeField] private GameObject _rewardPanel;

    [Header("버튼")]
    [Tooltip("사진 선택 버튼")][SerializeField] private Button _selectPhotosButton;
    [Tooltip("사진 저장 버튼")][SerializeField] private Button _saveButton;
    [Tooltip("다시 시도 버튼")][SerializeField] private Button _retryButton;
    [Tooltip("메인화면 버튼")][SerializeField] private Button _mainButton;

    [Header("보상 텍스트")]
    [Tooltip("별 개수만큼 지급되는 Jewel 수량")]
    [SerializeField] private TMP_Text _rewardJewelText;

    [Header("점수 텍스트")]
    [SerializeField] private TMP_Text _poseNameText;
    [SerializeField] private TMP_Text _poseScoreText;
    [SerializeField] private TMP_Text _compositionScoreText;
    [SerializeField] private TMP_Text _backGroundScoreText;
    [SerializeField] private TMP_Text _reactionScoreText;
    [SerializeField] private TMP_Text _totalScoreText;

    [Header("결과 연출")]
    [Tooltip("사진 페이드 시간")]
    [SerializeField] private float _photoFadeDuration = 0.25f;

    [Tooltip("게이지 증가 시간")]
    [SerializeField] private float _gaugeFillDuration = 0.45f;

    [Tooltip("총점 카운트업 시간")]
    [SerializeField] private float _scoreCountDuration = 0.8f;

    [Tooltip("각 연출 사이 간격")]
    [SerializeField] private float _sequenceInterval = 0.15f;

    [Tooltip("별 하나가 차오르는 시간")]
    [SerializeField] private float _starFillDuration = 0.15f;

    private NyangNyangSnapResultUISprite _resultSprite;
    private ResultCollectionPanelSprite _collectionSprite;
    private RewardPanelSprite _rewardSprite;
    private NyangNyangSnapCaptureRecord _currentRecord;
    private IReadOnlyList<NyangNyangSnapCaptureRecord> _records;
    private Sequence _resultSequence;
    private NyangNyangSnapUI _snapUI;
    private bool _isSaving;
    private bool _isRewardGranted;

    public override void Init()
    {
        Bind<Button>(typeof(NyangNyangSnapResultButton));

        _retryButton = Get<Button>((int)NyangNyangSnapResultButton.RetryButton);
        _mainButton = Get<Button>((int)NyangNyangSnapResultButton.MainButton);
        _selectPhotosButton = Get<Button>((int)NyangNyangSnapResultButton.SelectPhotosButton);
        _saveButton = Get<Button>((int)NyangNyangSnapResultButton.SaveButton);

        _resultSprite = GetComponent<NyangNyangSnapResultUISprite>();
        _collectionSprite = GetComponent<ResultCollectionPanelSprite>();
        _rewardSprite = GetComponent<RewardPanelSprite>();

        if (_resultSprite != null)
            _resultSprite.Init();
        else
            DebugTool.Warning("[NyangNyangSnapResultUI] NyangNyangSnapResultUISprite가 없습니다.", DebugType.UI, this);

        if (_collectionSprite != null) _collectionSprite.Init();

        if (_rewardSprite != null) _rewardSprite.Init();

        InitPopups();
    }

    private void InitPopups()
    {
        AddSelectPhotosButton(_selectPhotosButton);
        AddSaveButton(_saveButton);
        AddRetryButton(_retryButton);
        AddMainButton(_mainButton);
    }

    private void OnDisable()
    {
        KillResultSequence();
    }

    private void OnDestroy()
    {
        if (_selectPhotosButton != null) _selectPhotosButton.onClick.RemoveAllListeners();
        if (_saveButton != null) _saveButton.onClick.RemoveAllListeners();
        if (_retryButton != null) _retryButton.onClick.RemoveAllListeners();
        if (_mainButton != null) _mainButton.onClick.RemoveAllListeners();
    }

    private void AddSelectPhotosButton(Button button)
    {
        if (button == null) return;

        button.onClick.AddListener(() =>
        {
            KillResultSequence();
            _resultCollectionPanel.SetActive(true);
            _collectionSprite.SetPhotoCollection(_records);
            button.gameObject.SetActive(false);

            DebugTool.Log(
                "[NyangNyangSnapResultUI] 사진 선택 버튼 클릭 - 결과창 사진 모음 활성화",
                DebugType.UI,
                this
            );
        });
    }

    private void AddSaveButton(Button button)
    {
        if (button == null) return;

        button.onClick.AddListener(SaveSelectedPhotos);
    }

    private async void SaveSelectedPhotos()
    {
        if (_isSaving) return;

        _isSaving = true;

        if (_saveButton != null)
            _saveButton.interactable = false;

        try
        {
            IReadOnlyList<NyangNyangSnapCaptureRecord> selectedRecords = _collectionSprite.SelectedRecords;

            if (selectedRecords != null && selectedRecords.Count > 0)
            {
                foreach (NyangNyangSnapCaptureRecord record in selectedRecords)
                {
                    if (record == null || record.CapturedSprite == null || record.ScoreResult == null) continue;

                    string photoId = $"NNSnap_{DateTime.Now:yyMMdd_HH.mm.ss.fff}";
                    string imageUrl = SavePhotoToFolder(record.CapturedSprite, photoId);

                    if (string.IsNullOrEmpty(imageUrl)) continue;

                    NyangNyangSnapSavedPhotoData photoData = new()
                    {
                        photoId = photoId,
                        imageUrl = imageUrl,
                        storagePath = imageUrl,
                        poseScore = record.ScoreResult.PoseScore,
                        compositionScore = record.ScoreResult.CompositionScore,
                        timingScore = record.ScoreResult.TimingScore,
                        backGroundScore = record.ScoreResult.BackgroundScore,
                        totalScore = record.ScoreResult.TotalScore,
                        starCount = record.ScoreResult.StarCount,
                        createdAt = Timestamp.GetCurrentTimestamp()
                    };

                    _photoAlbumSO.AddPhoto(photoData);
                }

                await _photoAlbumSO.UpdateDataAsync();
                MainUI.Instance?.SetPhotoAlert(true);

                DebugTool.Log(
                    "[NyangNyangSnapResultUI] 선택 사진 저장 완료",
                    DebugType.UI,
                    this);
            }

            bool rewardGranted = await GrantJewelRewardAsync();

            if (!rewardGranted)
            {
                DebugTool.Warning(
                    "[NyangNyangSnapResultUI] Jewel 보상 지급에 실패하여 보상 패널을 열지 않습니다.",
                    DebugType.UI,
                    this);
                return;
            }

            _resultCollectionPanel.SetActive(false);
            _rewardPanel.SetActive(true);

            DebugTool.Log(
                "[NyangNyangSnapResultUI] 사진 저장 및 Jewel 보상 지급 완료",
                DebugType.UI,
                this);
        }
        finally
        {
            _isSaving = false;

            if (_saveButton != null)
                _saveButton.interactable = true;
        }
    }

    private async System.Threading.Tasks.Task<bool> GrantJewelRewardAsync()
    {
        if (_isRewardGranted)
            return true;

        if (_currentRecord?.ScoreResult == null)
        {
            DebugTool.Warning(
                "[NyangNyangSnapResultUI] Jewel 보상 계산에 필요한 점수 결과가 없습니다.",
                DebugType.UI,
                this);
            return false;
        }

        int rewardJewelCount = _currentRecord.ScoreResult.RewardJewelCount;
        bool success = await PlayerResourceManager.Instance.AddJewelAsync(rewardJewelCount);

        if (!success)
            return false;

        _isRewardGranted = true;
        SetRewardJewelText(rewardJewelCount);

        DebugTool.Log(
            $"[NyangNyangSnapResultUI] Jewel 보상 지급 완료 / 별: {_currentRecord.ScoreResult.StarCount}, Jewel: {rewardJewelCount}",
            DebugType.UI,
            this);

        return true;
    }

    private void SetRewardJewelText(int rewardJewelCount)
    {
        if (_rewardJewelText == null && _rewardPanel != null)
        {
            TMP_Text[] texts = _rewardPanel.GetComponentsInChildren<TMP_Text>(true);
            _rewardJewelText = Array.Find(texts, text => text.name == "RewardJewelText");
        }

        if (_rewardJewelText == null)
        {
            DebugTool.Warning(
                "[NyangNyangSnapResultUI] RewardPanel 하위에 RewardJewelText가 없습니다.",
                DebugType.UI,
                this);
            return;
        }

        _rewardJewelText.text = $"x {rewardJewelCount}";
    }

    private string SavePhotoToFolder(Sprite sprite, string photoId)
    {
        if (sprite == null || sprite.texture == null) return string.Empty;

        string folder = "Assets/Test/SavedPhotos";

        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

        string path = $"{folder}/{photoId}.png";

        File.WriteAllBytes(path, sprite.texture.EncodeToPNG());

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif

        return path;
    }

    private void AddRetryButton(Button button)
    {
        if (button == null) return;

        button.onClick.AddListener(() =>
        {
            KillResultSequence();

            _snapUI.RetrySnap();
            gameObject.SetActive(false);

            DebugTool.Log("[NyangNyangSnapResultUI] Retry 클릭", DebugType.UI, this);
        });
    }

    private void AddMainButton(Button button)
    {
        if (button == null) return;

        button.onClick.AddListener(() =>
        {
            KillResultSequence();

            _snapUI.BackToMain();
            gameObject.SetActive(false);

            DebugTool.Log("[NyangNyangSnapResultUI] Main 클릭 - 메인으로 이동", DebugType.UI, this);
        });
    }

    public void SetResult(NyangNyangSnapCaptureRecord record, IReadOnlyList<NyangNyangSnapCaptureRecord> records)
    {
        _currentRecord = record;
        _records = records;

        if (_currentRecord == null)
        {
            DebugTool.Warning("[NyangNyangSnapResultUI] 전달받은 촬영 결과가 없습니다.", DebugType.UI, this);
            return;
        }

        if (_resultSprite == null)
        {
            _resultSprite = GetComponent<NyangNyangSnapResultUISprite>();

            if (_resultSprite != null)
                _resultSprite.Init();
        }

        if (_resultSprite == null)
        {
            DebugTool.Warning("[NyangNyangSnapResultUI] ResultUISprite가 없습니다.", DebugType.UI, this);
            return;
        }

        if (_currentRecord.CapturedSprite == null)
        {
            DebugTool.Warning("[NyangNyangSnapResultUI] BestRecord에 CapturedSprite가 없습니다.", DebugType.UI, this);
            return;
        }

        if (_currentRecord.ScoreResult == null)
        {
            DebugTool.Warning("[NyangNyangSnapResultUI] BestRecord에 ScoreResult가 없습니다.", DebugType.UI, this);
            return;
        }

        SetInitialResultView();
        _resultSprite.SetResultPhoto(_currentRecord.CapturedSprite);
        PlayResultSequence(_currentRecord.ScoreResult, _currentRecord.PoseData);

        DebugTool.Log(
            $"[NyangNyangSnapResultUI] 결과 표시 시작 / 최고 점수: {_currentRecord.TotalScore}",
            DebugType.UI,
            this
        );
    }

    private void SetInitialResultView()
    {
        KillResultSequence();

        _resultCollectionPanel.SetActive(false);
        _rewardPanel.SetActive(false);
        _selectPhotosButton.gameObject.SetActive(true);
        _isSaving = false;
        _isRewardGranted = false;

        if (_saveButton != null)
            _saveButton.interactable = true;

        SetRewardJewelText(_currentRecord?.ScoreResult?.RewardJewelCount ?? 0);

        if (_resultSprite != null)
            _resultSprite.ResetRuntimeImages();

        if (_totalScoreText != null) _totalScoreText.text = "0";
        if (_poseScoreText != null) _poseScoreText.text = "0";
        if (_compositionScoreText != null) _compositionScoreText.text = "0";
        if (_backGroundScoreText != null) _backGroundScoreText.text = "0";
        if (_reactionScoreText != null) _reactionScoreText.text = "0";
        if (_poseNameText != null) _poseNameText.text = string.Empty;
    }

    private void PlayResultSequence(NyangNyangSnapScoreResult scoreResult, NyangNyangSnapPoseData poseData)
    {
        KillResultSequence();

        _resultSequence = DOTween.Sequence();

        _resultSequence.Append(_resultSprite.CreateStarFillSequence(scoreResult.TotalScore, _starFillDuration));
        _resultSequence.AppendInterval(_sequenceInterval);

        _resultSequence.Append(_resultSprite.CreatePhotoFadeTween(1f, _photoFadeDuration));
        _resultSequence.AppendInterval(_sequenceInterval);

        _resultSequence.AppendCallback(() =>
        {
            if (_poseNameText != null)
                _poseNameText.text = poseData != null ? poseData.PoseName : "-";
        });

        _resultSequence.AppendInterval(_sequenceInterval);

        _resultSequence.Append(_resultSprite.CreatePoseGaugeTween(scoreResult.PoseGaugeValue, _gaugeFillDuration));
        _resultSequence.Join(CreateIntTextTween(_poseScoreText, 0, scoreResult.PoseScore, _gaugeFillDuration));

        _resultSequence.AppendInterval(_sequenceInterval);

        _resultSequence.Append(_resultSprite.CreateCompositionGaugeTween(scoreResult.CompositionGaugeValue, _gaugeFillDuration));
        _resultSequence.Join(CreateIntTextTween(_compositionScoreText, 0, scoreResult.CompositionScore, _gaugeFillDuration));

        _resultSequence.AppendInterval(_sequenceInterval);

        _resultSequence.Append(_resultSprite.CreateBackGroundGaugeTween(scoreResult.BackgroundGaugeValue, _gaugeFillDuration));
        _resultSequence.Join(CreateIntTextTween(_backGroundScoreText, 0, scoreResult.BackgroundScore, _gaugeFillDuration));

        _resultSequence.AppendInterval(_sequenceInterval);

        _resultSequence.Append(_resultSprite.CreateReactionGaugeTween(scoreResult.TimingGaugeValue, _gaugeFillDuration));
        _resultSequence.Join(CreateIntTextTween(_reactionScoreText, 0, scoreResult.TimingScore, _gaugeFillDuration));

        _resultSequence.AppendInterval(_sequenceInterval);

        _resultSequence.Append(CreateIntTextTween(_totalScoreText, 0, scoreResult.TotalScore, _scoreCountDuration));

        _resultSequence.OnComplete(() =>
        {
            DebugTool.Log(
                $"[NyangNyangSnapResultUI] 결과 연출 완료 / 총점: {scoreResult.TotalScore}",
                DebugType.UI,
                this
            );
        });
    }

    private Tween CreateIntTextTween(TMP_Text targetText, int startValue, int endValue, float duration)
    {
        if (targetText == null)
            return DOVirtual.DelayedCall(0f, () => { });

        return DOVirtual.Int(startValue, endValue, duration, value =>
        {
            targetText.text = value.ToString();
        }).SetEase(Ease.OutQuad);
    }

    private void KillResultSequence()
    {
        if (_resultSequence == null) return;

        _resultSequence.Kill();
        _resultSequence = null;
    }

    public void SetSnapUI(NyangNyangSnapUI snapUI)
    {
        _snapUI = snapUI;
    }
}

public enum NyangNyangSnapResultButton
{
    RetryButton,
    MainButton,
    SelectPhotosButton,
    SaveButton,
}