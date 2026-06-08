using System;
using System.Collections.Generic;
using UI;
using UI.Base;
using UnityEngine.UI;

public class ResultCollectionPanelSprite : UIBase
{
    private UISpriteController[] _spriteController;

    public override void Init()
    {
        Bind<Image>(typeof(NyangNyangSnapResultCollectionImages));

        _spriteController = new UISpriteController[Enum.GetValues(typeof(NyangNyangSnapResultCollectionImages)).Length];

        for (int i = 0; i < Enum.GetValues(typeof(NyangNyangSnapResultCollectionImages)).Length; i++)
        {
            _spriteController[i] = new UISpriteController(GetImage(i));
        }

        SetSprites();
    }

    private void SetSprites()
    {
        SetSprite(NyangNyangSnapResultCollectionImages.ResultCollectionPanel, "NYS_Background");
        SetSprite(NyangNyangSnapResultCollectionImages.SaveButton, "Snap_Btn_Green");
    }

    private void SetSprite(NyangNyangSnapResultCollectionImages image, string key)
    {
        _spriteController[(int)image].ChangeSprite(key);
    }

    private void OnDestroy()
    {
        if (_spriteController == null) return;

        foreach (UISpriteController controller in _spriteController)
        {
            controller?.ReleaseSprite();
        }
    }

    public void SetPhotoCollection(IReadOnlyList<NyangNyangSnapCaptureRecord> records)
    {
        if (records == null) return;

        List<NyangNyangSnapCaptureRecord> sortedRecords = new(records);
        sortedRecords.Sort((a, b) => b.TotalScore.CompareTo(a.TotalScore));

        for (int i = 0; i < 10; i++)
        {
            Image image = GetImage((int)NyangNyangSnapResultCollectionImages.Image1 + i);

            if (image == null) continue;

            if (i >= sortedRecords.Count || sortedRecords[i].CapturedSprite == null)
            {
                image.gameObject.SetActive(false);
                continue;
            }

            image.gameObject.SetActive(true);
            image.sprite = sortedRecords[i].CapturedSprite;
        }
    }
}

public enum NyangNyangSnapResultCollectionImages
{
    ResultCollectionPanel,
    Image1,
    Image2,
    Image3,
    Image4,
    Image5,
    Image6,
    Image7,
    Image8,
    Image9,
    Image10,
    SaveButton,
}