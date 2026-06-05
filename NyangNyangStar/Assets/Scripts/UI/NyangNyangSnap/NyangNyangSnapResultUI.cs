using DG.Tweening;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;
using Util;

public class NyangNyangSnapResultUI : UIPopup
{
    [Header("버튼")]
    [Tooltip("다시 시도 버튼")][SerializeField] private Button _retryButton;
    [Tooltip("SNS 버튼")][SerializeField] private Button _snsButton;

    [Header("점수 텍스트")]
    [Tooltip("총점 Text 이름")]
    [SerializeField] private string _totalScoreTextName = "TotalScoreText";
    [SerializeField] private TMP_Text _totalScoreText;

    [Tooltip("포즈 점수 Text 이름")]
    [SerializeField] private string _poseScoreTextName = "PoseScoreText";
    [SerializeField] private TMP_Text _poseScoreText;

    [Tooltip("구도 점수 Text 이름")]
    [SerializeField] private string _compositionScoreTextName = "CompositionScoreText";
    [SerializeField] private TMP_Text _compositionScoreText;

    [Tooltip("반응 점수 Text 이름")]
    [SerializeField] private string _reactionScoreTextName = "ReactionScoreText";
    [SerializeField] private TMP_Text _reactionScoreText;

    [Tooltip("포즈 이름 Text 이름")]
    [SerializeField] private string _poseNameTextName = "PoseNameText";
    [SerializeField] private TMP_Text _poseNameText;

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

    private NyangNyangSnapResultUISprite _sprite;
    private NyangNyangSnapCaptureRecord _currentRecord;
    private Sequence _resultSequence;

    public override void Init()
    {
        Bind<Button>(typeof(NyangNyangSnapResultButton));

        _retryButton = Get<Button>((int)NyangNyangSnapResultButton.RetryButton);
        _snsButton = Get<Button>((int)NyangNyangSnapResultButton.SnsButton);

        _sprite = GetComponent<NyangNyangSnapResultUISprite>();

        if (_sprite != null)
            _sprite.Init();
        else
            DebugTool.Warning("[NyangNyangSnapResultUI] NyangNyangSnapResultUISprite가 없습니다.", DebugType.UI, this);

        AutoAssignTexts();
        InitPopups();
    }

    private void InitPopups()
    {
        AddRetryButton(_retryButton);
        AddSnsButton(_snsButton);
    }

    private void OnDisable()
    {
        KillResultSequence();
    }

    private void AddRetryButton(Button button)
    {
        if (button == null) return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            KillResultSequence();

            gameObject.SetActive(false);

            DebugTool.Log(
                "[NyangNyangSnapResultUI] Retry 클릭 - 결과창 비활성화",
                DebugType.UI,
                this
            );
        });
    }

    private void AddSnsButton(Button button)
    {
        if (button == null) return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            DebugTool.Log("[NyangNyangSnapResultUI] SNS 버튼 클릭 - 추후 업로드 기능 연결", DebugType.UI, this);
        });
    }

    public void SetResult(NyangNyangSnapCaptureRecord record)
    {
        _currentRecord = record;

        if (_currentRecord == null)
        {
            DebugTool.Warning("[NyangNyangSnapResultUI] 전달받은 촬영 결과가 없습니다.", DebugType.UI, this);
            return;
        }

        if (_sprite == null)
        {
            _sprite = GetComponent<NyangNyangSnapResultUISprite>();

            if (_sprite != null)
                _sprite.Init();
        }

        AutoAssignTexts();

        if (_sprite == null)
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
        _sprite.SetResultPhoto(_currentRecord.CapturedSprite);
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

        if (_sprite != null)
            _sprite.ResetRuntimeImages();

        if (_totalScoreText != null) _totalScoreText.text = "0";
        if (_poseScoreText != null) _poseScoreText.text = "0";
        if (_compositionScoreText != null) _compositionScoreText.text = "0";
        if (_reactionScoreText != null) _reactionScoreText.text = "0";
        if (_poseNameText != null) _poseNameText.text = string.Empty;
    }

    private void PlayResultSequence(NyangNyangSnapScoreResult scoreResult, NyangNyangSnapPoseData poseData)
    {
        KillResultSequence();

        _resultSequence = DOTween.Sequence();

        _resultSequence.Append(_sprite.CreateStarFillSequence(scoreResult.TotalScore, _starFillDuration));
        _resultSequence.AppendInterval(_sequenceInterval);

        _resultSequence.Append(_sprite.CreatePhotoFadeTween(1f, _photoFadeDuration));
        _resultSequence.AppendInterval(_sequenceInterval);

        _resultSequence.AppendCallback(() =>
        {
            if (_poseNameText != null)
                _poseNameText.text = poseData != null ? poseData.PoseName : "-";
        });

        _resultSequence.AppendInterval(_sequenceInterval);

        _resultSequence.Append(_sprite.CreatePoseGaugeTween(scoreResult.PoseGaugeValue, _gaugeFillDuration));
        _resultSequence.Join(CreateIntTextTween(_poseScoreText, 0, scoreResult.PoseScore, _gaugeFillDuration));

        _resultSequence.AppendInterval(_sequenceInterval);

        _resultSequence.Append(_sprite.CreateCompositionGaugeTween(scoreResult.CompositionGaugeValue, _gaugeFillDuration));
        _resultSequence.Join(CreateIntTextTween(_compositionScoreText, 0, scoreResult.CompositionScore, _gaugeFillDuration));

        _resultSequence.AppendInterval(_sequenceInterval);

        _resultSequence.Append(_sprite.CreateReactionGaugeTween(scoreResult.ReactionGaugeValue, _gaugeFillDuration));
        _resultSequence.Join(CreateIntTextTween(_reactionScoreText, 0, scoreResult.ReactionScore, _gaugeFillDuration));

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

    private void AutoAssignTexts()
    {
        if (_totalScoreText == null)
            _totalScoreText = FindTMPTextByNameInCanvas(_totalScoreTextName);

        if (_poseScoreText == null)
            _poseScoreText = FindTMPTextByNameInCanvas(_poseScoreTextName);

        if (_compositionScoreText == null)
            _compositionScoreText = FindTMPTextByNameInCanvas(_compositionScoreTextName);

        if (_reactionScoreText == null)
            _reactionScoreText = FindTMPTextByNameInCanvas(_reactionScoreTextName);

        if (_poseNameText == null)
            _poseNameText = FindTMPTextByNameInCanvas(_poseNameTextName);

        LogTextAutoAssignResult(_totalScoreText, _totalScoreTextName);
        LogTextAutoAssignResult(_poseScoreText, _poseScoreTextName);
        LogTextAutoAssignResult(_compositionScoreText, _compositionScoreTextName);
        LogTextAutoAssignResult(_reactionScoreText, _reactionScoreTextName);
        LogTextAutoAssignResult(_poseNameText, _poseNameTextName);
    }

    private void LogTextAutoAssignResult(TMP_Text targetText, string targetName)
    {
        if (targetText != null)
        {
            DebugTool.Log(
                $"[NyangNyangSnapResultUI] Text 자동 연결 완료: {targetName}",
                DebugType.UI,
                this
            );
            return;
        }

        DebugTool.Warning(
            $"[NyangNyangSnapResultUI] Text 자동 연결 실패. 이름: {targetName}",
            DebugType.UI,
            this
        );
    }

    private TMP_Text FindTMPTextByNameInCanvas(string objectName)
    {
        Transform target = FindTransformByNameInCanvas(objectName);

        if (target == null)
            return null;

        return target.GetComponent<TMP_Text>();
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
}

public enum NyangNyangSnapResultButton
{
    RetryButton,
    SnsButton
}