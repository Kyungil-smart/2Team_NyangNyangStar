using Core.Managers;
using Data.ScriptableObjects.MergeBoard;
using System;
using System.Collections.Generic;
using TMPro;
using UI;
using UI.Base;
using UI.MergeBoard;
using UnityEngine;
using UnityEngine.UI;
using Util;

public class NyangNyangSnapSnackUI : UIPopup
{
    [Header("버튼")]
    [Tooltip("간식 버튼")]
    [SerializeField] private Button _snackButton;

    [Tooltip("뒤로 가기 패널")]
    [SerializeField] private Button _backPanel;

    [Tooltip("간식 패널 닫기")]
    [SerializeField] private Button _closeButton;

    [Header("데이터")]
    [Tooltip("아이템 이름과 AddressableKey를 가져올 ItemDatabase SO")]
    [SerializeField] private ItemDatabaseSo _itemDatabaseSO;

    [Tooltip("ItemID로 Toy/Snack/Food를 판별할 Tool SO")]
    [SerializeField] private NyangNyangSnapToolSO _toolSO;

    [Header("동적 생성")]
    [Tooltip("간식 버튼들이 생성될 부모 Content")]
    [SerializeField] private Transform _content;

    [Header("아이템 배치")]
    [SerializeField] private NyangNyangSnapPlacementController _placementController;

    private readonly List<Button> _createdButtons = new();
    private readonly List<UISpriteController> _spriteControllers = new();

    private bool _isInitialized;

    public override void Init()
    {
        Bind<Button>(typeof(NyangNyangSnapSnackButtons));

        _snackButton = Get<Button>((int)NyangNyangSnapSnackButtons.SnackButton);
        _backPanel = Get<Button>((int)NyangNyangSnapSnackButtons.BackPanel);
        _closeButton = Get<Button>((int)NyangNyangSnapSnackButtons.CloseButton);

        InitPopups();
        AutoAssignPlacementController();

        _isInitialized = true;

        LoadSnackButtons();
    }

    private void OnEnable()
    {
        if (!_isInitialized)
            return;

        AutoAssignPlacementController();
        LoadSnackButtons();
    }

    private void AutoAssignPlacementController()
    {
        if (_placementController != null && _placementController.gameObject.scene.IsValid())
            return;

        _placementController = FindFirstObjectByType<NyangNyangSnapPlacementController>();

        if (_placementController == null)
        {
            DebugTool.Warning(
                "[NyangNyangSnapSnackUI] 실행 중인 PlacementController를 찾지 못했습니다.",
                DebugType.UI,
                this
            );

            return;
        }

        DebugTool.Log(
            "[NyangNyangSnapSnackUI] 실행 중인 PlacementController 연결 완료",
            DebugType.UI,
            this
        );
    }

    private void InitPopups()
    {
        if (_backPanel != null)
        {
            _backPanel.onClick.RemoveAllListeners();
            _backPanel.onClick.AddListener(ClosePopup);
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveAllListeners();
            _closeButton.onClick.AddListener(ClosePopup);
        }
    }

    private void LoadSnackButtons()
    {
        if (!ValidateLoadReferences())
            return;

        if (_content == null)
            _content = _snackButton.transform.parent;

        ClearCreatedButtons();

        _snackButton.gameObject.SetActive(false);

        int displayedItemCount = 0;
        IReadOnlyList<ItemData> itemList = _itemDatabaseSO.Items;

        for (int i = 0; i < itemList.Count; i++)
        {
            ItemData itemData = itemList[i];

            if (itemData == null || !itemData.HasItem)
                continue;

            if (!_toolSO.TryGetToolDataByItemID(itemData.ItemID, out NyangNyangSnapToolData toolData))
                continue;

            bool isSnackOrFood =
                toolData.ItemToolType == NyangNyangSnapToolType.Snack ||
                toolData.ItemToolType == NyangNyangSnapToolType.Food;

            if (!isSnackOrFood)
                continue;

            int ownedCount = MergeBoardItemService.Instance.GetOwnedItemCount(itemData.ItemID);

            if (ownedCount <= 0)
                continue;

            NyangNyangSnapInventoryItem inventoryItem = new NyangNyangSnapInventoryItem(
                Array.Empty<int>(),
                itemData,
                ownedCount,
                toolData
            );

            CreateSnackButton(inventoryItem);

            displayedItemCount++;
        }

        DebugTool.Log(
            $"[NyangNyangSnapSnackUI] 보유 간식/음식 표시 완료 / 종류:{displayedItemCount}",
            DebugType.UI,
            this
        );
    }

    private bool ValidateLoadReferences()
    {
        if (MergeBoardItemService.Instance == null)
        {
            DebugTool.Warning(
                "[NyangNyangSnapSnackUI] MergeBoardItemService.Instance가 없습니다.",
                DebugType.UI,
                this
            );

            return false;
        }

        if (_itemDatabaseSO == null)
        {
            DebugTool.Warning(
                "[NyangNyangSnapSnackUI] ItemDatabaseSo가 연결되지 않았습니다.",
                DebugType.UI,
                this
            );

            return false;
        }

        if (_toolSO == null)
        {
            DebugTool.Warning(
                "[NyangNyangSnapSnackUI] NyangNyangSnapToolSO가 연결되지 않았습니다.",
                DebugType.UI,
                this
            );

            return false;
        }

        if (_snackButton == null)
        {
            DebugTool.Warning(
                "[NyangNyangSnapSnackUI] SnackButton 템플릿이 없습니다.",
                DebugType.UI,
                this
            );

            return false;
        }

        return true;
    }

    private void CreateSnackButton(NyangNyangSnapInventoryItem item)
    {
        Button createdButton = Instantiate(_snackButton, _content);

        createdButton.gameObject.SetActive(true);
        createdButton.name = $"SnackButton_{item.ItemID}";

        SetButtonSprite(createdButton, item);
        SetButtonCount(createdButton, item.Count);

        createdButton.onClick.RemoveAllListeners();
        createdButton.onClick.AddListener(() => SelectSnackItem(item, createdButton));

        _createdButtons.Add(createdButton);
    }

    private void SetButtonSprite(Button createdButton, NyangNyangSnapInventoryItem item)
    {
        Image itemImage = createdButton.GetComponent<Image>();

        if (itemImage == null)
        {
            DebugTool.Warning(
                $"[NyangNyangSnapSnackUI] 생성된 버튼에 Image가 없습니다. ItemID:{item.ItemID}",
                DebugType.UI,
                this
            );

            return;
        }

        UISpriteController spriteController = new UISpriteController(itemImage);

        _spriteControllers.Add(spriteController);

        if (!string.IsNullOrWhiteSpace(item.AddressableKey))
        {
            spriteController.ChangeSprite(item.AddressableKey);
            return;
        }

        DebugTool.Warning(
            $"[NyangNyangSnapSnackUI] AddressableKey가 비어있습니다. ItemID:{item.ItemID}, ItemName:{item.ItemName}",
            DebugType.UI,
            this
        );
    }

    private void SetButtonCount(Button createdButton, int count)
    {
        TMP_Text countText = createdButton.GetComponentInChildren<TMP_Text>(true);

        if (countText != null)
            countText.text = $"{count}";
    }

    private void SelectSnackItem(NyangNyangSnapInventoryItem item, Button selectedButton)
    {
        GameManager.Audio.PlaySfx("Main_SFX_Touch");

        AutoAssignPlacementController();

        if (_placementController == null)
        {
            DebugTool.Warning(
                "[NyangNyangSnapSnackUI] PlacementController가 없어 아이템을 선택할 수 없습니다.",
                DebugType.UI,
                this
            );

            return;
        }

        if (selectedButton == null)
        {
            DebugTool.Warning(
                $"[NyangNyangSnapSnackUI] 선택한 버튼이 없습니다. ItemID:{item.ItemID}",
                DebugType.UI,
                this
            );

            return;
        }

        Image itemImage = selectedButton.GetComponent<Image>();

        if (itemImage == null || itemImage.sprite == null)
        {
            DebugTool.Warning(
                $"[NyangNyangSnapSnackUI] 선택한 간식/음식 이미지가 없습니다. ItemID:{item.ItemID}, ItemName:{item.ItemName}",
                DebugType.UI,
                this
            );

            return;
        }

        _placementController.SelectItem(item.ItemID, item.ItemName, itemImage.sprite);

        DebugTool.Log(
            $"[NyangNyangSnapSnackUI] 간식/음식 선택 완료 / ItemID:{item.ItemID}, ItemName:{item.ItemName}, Count:{item.Count}, ToolType:{item.ToolType}",
            DebugType.UI,
            this
        );

        gameObject.SetActive(false);
    }

    private void ClearCreatedButtons()
    {
        for (int i = 0; i < _spriteControllers.Count; i++)
            _spriteControllers[i]?.Dispose();

        _spriteControllers.Clear();

        for (int i = 0; i < _createdButtons.Count; i++)
        {
            if (_createdButtons[i] != null)
                Destroy(_createdButtons[i].gameObject);
        }

        _createdButtons.Clear();
    }

    private void ClosePopup()
    {
        GameManager.Audio.PlaySfx("Main_SFX_Touch");

        gameObject.SetActive(false);
    }
}

public enum NyangNyangSnapSnackButtons
{
    SnackButton,
    BackPanel,
    CloseButton
}