using System;
using System.Collections.Generic;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class ResultCollectionPanelSprite : UIBase
{
    private const int PhotoCount = 10;
    private UISpriteController[] _spriteController;

    [SerializeField] private Color _circleColor;

    private Image[] _photoImages = new Image[PhotoCount];
    private Image[] _checkMarks = new Image[PhotoCount];

    private readonly List<NyangNyangSnapCaptureRecord> _sortedRecords = new();
    private readonly List<NyangNyangSnapCaptureRecord> _selectedRecords = new();
    public IReadOnlyList<NyangNyangSnapCaptureRecord> SelectedRecords => _selectedRecords;

    public override void Init()
    {
        Bind<Image>(typeof(NyangNyangSnapResultCollectionImages));

        _spriteController = new UISpriteController[Enum.GetValues(typeof(NyangNyangSnapResultCollectionImages)).Length];

        for (int i = 0; i < Enum.GetValues(typeof(NyangNyangSnapResultCollectionImages)).Length; i++)
        {
            _spriteController[i] = new UISpriteController(GetImage(i));
        }

        GetImages();
        SetSprites();
        AddImageButtons();
    }

    private void SetSprites()
    {
        SetSprite(NyangNyangSnapResultCollectionImages.ResultCollectionPanel, "NYS_Background");
        SetSprite(NyangNyangSnapResultCollectionImages.SaveButton, "Snap_Btn_Green");
        SetSprite(NyangNyangSnapResultCollectionImages.CatImage, "NQ_Portrait_Moongchi");
        SetSprite(NyangNyangSnapResultCollectionImages.CharacterImage, "NQ_Portrait_Main");

        for (int i = 0; i < PhotoCount; i++)
        {
            SetSprite(NyangNyangSnapResultCollectionImages.Circle1 + i, "Shape_Circle", _circleColor);
            SetSprite(NyangNyangSnapResultCollectionImages.Frame1 + i, "Snap_Btn_Unchecked");
            SetSprite(NyangNyangSnapResultCollectionImages.CheckMark1 + i, "Icon_Check");
            SetCheckMark(i, false);
        }
    }

    private void SetSprite(NyangNyangSnapResultCollectionImages image, string key)
    {
        _spriteController[(int)image].ChangeSprite(key);
    }

    private void SetSprite(NyangNyangSnapResultCollectionImages image, string key, Color color)
    {
        _spriteController[(int)image].ChangeColor(color);
        _spriteController[(int)image].ChangeSprite(key);
    }

    private void SetCheckMark(int index, bool active)
    {
        _checkMarks[index].gameObject.SetActive(active);
    }

    private void GetImages()
    {
        for (int i = 0; i < PhotoCount; i++)
        {
            _photoImages[i] = GetImage((int)NyangNyangSnapResultCollectionImages.Image1 + i);
            _checkMarks[i] = GetImage((int)NyangNyangSnapResultCollectionImages.CheckMark1 + i);
        }
    }

    private void AddImageButtons()
    {
        for (int i = 0; i < PhotoCount; i++)
        {
            int index = i;

            if (_photoImages[i] == null) continue;

            Button button = _photoImages[i].GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => ToggleSelectPhoto(index));
        }
    }

    public void SetPhotoCollection(IReadOnlyList<NyangNyangSnapCaptureRecord> records)
    {
        _sortedRecords.Clear();
        _selectedRecords.Clear();

        for (int i = 0; i < PhotoCount; i++)
        {
            SetCheckMark(i, false);
        }

        if (records == null) return;

        _sortedRecords.AddRange(records);
        _sortedRecords.Sort((a, b) => b.TotalScore.CompareTo(a.TotalScore));

        for (int i = 0; i < PhotoCount; i++)
        {
            Image image = _photoImages[i];

            if (image == null) continue;

            if (i >= _sortedRecords.Count || _sortedRecords[i].CapturedSprite == null)
            {
                image.gameObject.SetActive(false);
                SetCheckMark(i, false);
                continue;
            }

            image.gameObject.SetActive(true);
            image.sprite = _sortedRecords[i].CapturedSprite;
        }
    }

    public void ToggleSelectPhoto(int index)
    {
        if (index < 0 || index >= _sortedRecords.Count) return;

        NyangNyangSnapCaptureRecord record = _sortedRecords[index];

        if (!_selectedRecords.Contains(record))
        {
            _selectedRecords.Add(record);
            SetCheckMark(index, true);
            DebugTool.Log($"{index + 1}번 사진 선택. 점수 : {record.TotalScore}", DebugType.UI, this);
        }
        else
        {
            _selectedRecords.Remove(record);
            SetCheckMark(index, false);
            DebugTool.Log($"{index + 1}번 사진 선택 해제. 점수 : {record.TotalScore}", DebugType.UI, this);
        }
    }

    private void OnDestroy()
    {
        if (_spriteController == null) return;

        foreach (UISpriteController controller in _spriteController)
        {
            controller?.ReleaseSprite();
        }
    }
}

public enum NyangNyangSnapResultCollectionImages
{
    ResultCollectionPanel,
    Image1, Image2, Image3, Image4, Image5,
    Image6, Image7, Image8, Image9, Image10,
    Circle1, Circle2, Circle3, Circle4, Circle5,
    Circle6, Circle7, Circle8, Circle9, Circle10,
    Frame1, Frame2, Frame3, Frame4, Frame5,
    Frame6, Frame7, Frame8, Frame9, Frame10,
    CheckMark1, CheckMark2, CheckMark3, CheckMark4, CheckMark5,
    CheckMark6, CheckMark7, CheckMark8, CheckMark9, CheckMark10,
    SaveButton,
    CatImage,
    CharacterImage
}