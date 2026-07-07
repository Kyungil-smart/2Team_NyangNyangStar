using Core.Managers;
using Data.ScriptableObjects.MergeBoard;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UI;
using UI.Base;
using UI.MergeBoard;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Util;

public class NyangNyangSnapToyUI : UIPopup
{
    [Header("버튼")]
    [Tooltip("장난감 버튼")]
    [SerializeField] private Button _toyButton;

    [Tooltip("뒤로 가기 패널")]
    [SerializeField] private Button _backPanel;

    [Tooltip("장난감 패널 닫기")]
    [SerializeField] private Button _closeButton;


    [Header("데이터")]
    [Tooltip("아이템 이름과 AddressableKey를 가져올 ItemDatabase SO")]
    [SerializeField] private ItemDatabaseSo _itemDatabaseSO;

    [Tooltip("ItemID로 장난감/간식/음식을 판별할 Tool SO")]
    [SerializeField] private NyangNyangSnapToolSO _toolSO;

    [Header("동적 생성")]
    [Tooltip("장난감 버튼들이 생성될 부모 Content")]
    [SerializeField] private Transform _content;

    [Header("빈 목록 안내")]
    [Tooltip("보유 장난감이 없을 때 표시할 TextMeshPro 텍스트")]
    [SerializeField] private TMP_Text _emptyMessageText;

    [Tooltip("빈 목록일 때 표시할 문구")]
    [SerializeField] private string _emptyMessageTextString = "장난감이 없습니다.";

    [Header("아이템 배치")]
    [SerializeField] private NyangNyangSnapPlacementController _placementController;

    [Header("선택 연출")]
    [Tooltip("선택된 장난감 버튼 크기 배율")]
    [SerializeField] private float _selectedScale = 1.1f;

    [Tooltip("선택 크기 변경 시간")]
    [SerializeField] private float _selectTweenDuration = 0.15f;

    [Tooltip("선택 크기 변경 Ease")]
    [SerializeField] private Ease _selectEase = Ease.OutBack;

    [Header("길게 눌러 배치")]
    [Tooltip("장난감 배치 드래그가 시작되는 누르기 시간")]
    [Min(0.05f)]
    [SerializeField] private float _placementHoldDuration = 0.25f;

    private readonly List<Button> _createdButtons = new();
    private readonly List<UISpriteController> _spriteControllers = new();

    private Button _selectedButton;
    private int _selectedItemID = -1;
    private Vector3 _selectedButtonOriginalScale = Vector3.one;

    private Coroutine _holdCoroutine;
    private Coroutine _loadButtonsCoroutine;

    private NyangNyangSnapInventoryItem _pressedItem;
    private Button _pressedButton;
    private PointerEventData _currentPointerEvent;

    private bool _isInitialized;
    private bool _isPointerPressed;
    private bool _isPlacementDragging;


    private NyangNyangSnapToyUISprite _nyangNyangSnapToyUISprite;


    public override void Init()
    {
        Bind<Button>(typeof(NyangNyangSnapToyButtons));

        _toyButton = Get<Button>((int)NyangNyangSnapToyButtons.ToyButton);
        _backPanel = Get<Button>((int)NyangNyangSnapToyButtons.BackPanel);
        _closeButton = Get<Button>((int)NyangNyangSnapToyButtons.CloseButton);

        _nyangNyangSnapToyUISprite = GetComponent<NyangNyangSnapToyUISprite>();
        _nyangNyangSnapToyUISprite.Init();

        AutoAssignPlacementController();
        InitPopups();

        _isInitialized = true;

    }

    private void OnEnable()
    {
        if (!_isInitialized)
            return;

        AutoAssignPlacementController();
    }

    private void OnDisable()
    {
        StopLoadButtonsCoroutine();
        CancelHold();

        _isPointerPressed = false;
        _isPlacementDragging = false;

        ResetSelectedButton(false);
    }

    private void AutoAssignPlacementController()
    {
        if (_placementController != null && _placementController.gameObject.scene.IsValid())
            return;

        _placementController = FindFirstObjectByType<NyangNyangSnapPlacementController>();

        if (_placementController == null)
        {
            DebugTool.Warning(
                "[NyangNyangSnapToyUI] 실행 중인 PlacementController를 찾지 못했습니다.",
                DebugType.UI,
                this
            );

            return;
        }

        DebugTool.Log(
            "[NyangNyangSnapToyUI] 실행 중인 PlacementController 연결 완료",
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

    // 냥냥스냅 메인 UI에서 머지보드 최신 로드가 끝난 뒤 호출합니다.
    // 비활성 상태에서도 버튼을 미리 만들어 패널이 켜질 때 생성 과정이 보이지 않게 합니다.
    public bool RefreshFromLoadedInventory()
    {
        StopLoadButtonsCoroutine();

        if (!ValidateLoadReferences())
        {
            SetEmptyMessageActive(true);
            return false;
        }

        LoadToyButtons();
        return true;
    }

    private void RequestLoadToyButtons()
    {
        StopLoadButtonsCoroutine();

        if (!isActiveAndEnabled)
            return;

        _loadButtonsCoroutine = StartCoroutine(LoadToyButtonsWhenReady());
    }

    private IEnumerator LoadToyButtonsWhenReady()
    {
        if (!ValidateLoadReferences())
        {
            SetEmptyMessageActive(true);
            _loadButtonsCoroutine = null;
            yield break;
        }

        DebugTool.Log(
            "[NyangNyangSnapToyUI] 머지보드 최신 데이터 로드 시작",
            DebugType.UI,
            this
        );

        Task<bool> reloadTask = MergeBoardItemService.Instance.ReloadInventoryFromServerAsync();

        while (isActiveAndEnabled && !reloadTask.IsCompleted)
            yield return null;

        if (!isActiveAndEnabled)
        {
            _loadButtonsCoroutine = null;
            yield break;
        }

        if (reloadTask.IsFaulted)
        {
            Debug.LogException(reloadTask.Exception, this);
            _loadButtonsCoroutine = null;
            yield break;
        }

        if (!reloadTask.Result)
        {
            DebugTool.Warning(
                "[NyangNyangSnapToyUI] 머지보드 최신 데이터 로드에 실패했습니다.",
                DebugType.UI,
                this
            );

            SetEmptyMessageActive(true);
            _loadButtonsCoroutine = null;
            yield break;
        }

        LoadToyButtons();
        _loadButtonsCoroutine = null;
    }

    private bool HasOwnedToyItem()
    {
        IReadOnlyList<ItemData> itemList = _itemDatabaseSO.Items;

        for (int i = 0; i < itemList.Count; i++)
        {
            ItemData itemData = itemList[i];

            if (itemData == null || !itemData.HasItem)
                continue;

            if (!_toolSO.TryGetToolDataByItemID(itemData.ItemID, out NyangNyangSnapToolData toolData))
                continue;

            if (toolData.ItemToolType != NyangNyangSnapToolType.Toy)
                continue;

            if (MergeBoardItemService.Instance.GetOwnedItemCount(itemData.ItemID) > 0)
                return true;
        }

        return false;
    }

    private void StopLoadButtonsCoroutine()
    {
        if (_loadButtonsCoroutine == null)
            return;

        StopCoroutine(_loadButtonsCoroutine);
        _loadButtonsCoroutine = null;
    }

    private void LoadToyButtons()
    {
        if (!ValidateLoadReferences())
            return;

        if (_content == null)
            _content = _toyButton.transform.parent;

        ClearCreatedButtons();

        _toyButton.gameObject.SetActive(false);

        int displayedItemCount = 0;
        IReadOnlyList<ItemData> itemList = _itemDatabaseSO.Items;

        for (int i = 0; i < itemList.Count; i++)
        {
            ItemData itemData = itemList[i];

            if (itemData == null || !itemData.HasItem)
                continue;

            if (!_toolSO.TryGetToolDataByItemID(itemData.ItemID, out NyangNyangSnapToolData toolData))
                continue;

            if (toolData.ItemToolType != NyangNyangSnapToolType.Toy)
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

            CreateToyButton(inventoryItem);

            displayedItemCount++;
        }

        SetEmptyMessageActive(displayedItemCount <= 0);

        DebugTool.Log(
            $"[NyangNyangSnapToyUI] 보유 장난감 표시 완료 / 종류:{displayedItemCount}",
            DebugType.UI,
            this
        );
    }

    private void SetEmptyMessageActive(bool isActive)
    {
        if (_emptyMessageText == null)
            return;

        _emptyMessageText.text = _emptyMessageTextString;
        _emptyMessageText.gameObject.SetActive(isActive);
    }

    private bool ValidateLoadReferences()
    {
        if (MergeBoardItemService.Instance == null)
        {
            DebugTool.Warning(
                "[NyangNyangSnapToyUI] MergeBoardItemService.Instance가 없습니다.",
                DebugType.UI,
                this
            );

            return false;
        }

        if (_itemDatabaseSO == null)
        {
            DebugTool.Warning(
                "[NyangNyangSnapToyUI] ItemDatabaseSo가 연결되지 않았습니다.",
                DebugType.UI,
                this
            );

            return false;
        }

        if (_toolSO == null)
        {
            DebugTool.Warning(
                "[NyangNyangSnapToyUI] NyangNyangSnapToolSO가 연결되지 않았습니다.",
                DebugType.UI,
                this
            );

            return false;
        }

        if (_toyButton == null)
        {
            DebugTool.Warning(
                "[NyangNyangSnapToyUI] ToyButton 템플릿이 없습니다.",
                DebugType.UI,
                this
            );

            return false;
        }

        return true;
    }

    private void CreateToyButton(NyangNyangSnapInventoryItem item)
    {
        Button createdButton = Instantiate(_toyButton, _content);

        createdButton.gameObject.SetActive(true);
        createdButton.name = $"ToyButton_{item.ItemID}";

        SetButtonSprite(createdButton, item);
        SetButtonCount(createdButton, item.Count);
        RegisterToyButtonEvents(createdButton, item);

        _createdButtons.Add(createdButton);
    }

    private void SetButtonSprite(Button createdButton, NyangNyangSnapInventoryItem item)
    {
        Image itemImage = createdButton.GetComponent<Image>();

        if (itemImage == null)
        {
            DebugTool.Warning(
                $"[NyangNyangSnapToyUI] 생성된 버튼에 Image가 없습니다. ItemID:{item.ItemID}",
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
            $"[NyangNyangSnapToyUI] AddressableKey가 비어있습니다. ItemID:{item.ItemID}, ItemName:{item.ItemName}",
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

    private void RegisterToyButtonEvents(Button createdButton, NyangNyangSnapInventoryItem item)
    {
        createdButton.onClick.RemoveAllListeners();
        createdButton.onClick.AddListener(() => SelectToyItem(item, createdButton));

        EventTrigger eventTrigger = createdButton.GetComponent<EventTrigger>();

        if (eventTrigger == null)
            eventTrigger = createdButton.gameObject.AddComponent<EventTrigger>();

        eventTrigger.triggers.Clear();

        AddEventTrigger(
            eventTrigger,
            EventTriggerType.PointerDown,
            eventData => OnToyPointerDown(item, createdButton, eventData)
        );

        AddEventTrigger(
            eventTrigger,
            EventTriggerType.Drag,
            OnToyDrag
        );

        AddEventTrigger(
            eventTrigger,
            EventTriggerType.PointerUp,
            OnToyPointerUp
        );

        AddEventTrigger(
            eventTrigger,
            EventTriggerType.PointerExit,
            OnToyPointerExit
        );
    }

    private void AddEventTrigger(EventTrigger eventTrigger, EventTriggerType eventType, Action<PointerEventData> callback)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry
        {
            eventID = eventType
        };

        entry.callback.AddListener(baseEventData =>
        {
            if (baseEventData is PointerEventData pointerEventData)
                callback?.Invoke(pointerEventData);
        });

        eventTrigger.triggers.Add(entry);
    }

    private void SelectToyItem(NyangNyangSnapInventoryItem item, Button selectedButton)
    {
        AutoAssignPlacementController();

        if (_placementController == null || selectedButton == null)
            return;

        if (_isPlacementDragging)
            return;

        if (_selectedButton == selectedButton && _selectedItemID == item.ItemID)
            return;

        Image itemImage = selectedButton.GetComponent<Image>();

        if (itemImage == null || itemImage.sprite == null)
        {
            DebugTool.Warning(
                $"[NyangNyangSnapToyUI] 선택한 장난감 이미지가 없습니다. ItemID:{item.ItemID}",
                DebugType.UI,
                this
            );

            return;
        }

        bool selected = _placementController.SelectToy(item.ItemID, item.ItemName, itemImage.sprite);

        if (!selected)
            return;

        GameManager.Audio.PlaySfx("Main_SFX_Touch");

        SetSelectedButton(selectedButton, item.ItemID);

        DebugTool.Log(
            $"[NyangNyangSnapToyUI] 장난감 선택 완료 / ItemID:{item.ItemID}, ItemName:{item.ItemName}",
            DebugType.UI,
            this
        );
    }

    private void SetSelectedButton(Button selectedButton, int itemID)
    {
        if (_selectedButton == selectedButton && _selectedItemID == itemID)
            return;

        ResetSelectedButton(true);

        _selectedButton = selectedButton;
        _selectedItemID = itemID;
        _selectedButtonOriginalScale = selectedButton.transform.localScale;

        selectedButton.transform.DOKill();

        selectedButton.transform
            .DOScale(_selectedButtonOriginalScale * _selectedScale, _selectTweenDuration)
            .SetEase(_selectEase);
    }

    private void ResetSelectedButton(bool useTween)
    {
        if (_selectedButton == null)
            return;

        _selectedButton.transform.DOKill();

        if (useTween)
        {
            _selectedButton.transform
                .DOScale(_selectedButtonOriginalScale, _selectTweenDuration)
                .SetEase(Ease.OutSine);
        }
        else
        {
            _selectedButton.transform.localScale = _selectedButtonOriginalScale;
        }

        _selectedButton = null;
        _selectedItemID = -1;
    }

    private void OnToyPointerDown(NyangNyangSnapInventoryItem item, Button button, PointerEventData eventData)
    {
        CancelHold();

        _isPointerPressed = true;
        _isPlacementDragging = false;

        _pressedItem = item;
        _pressedButton = button;
        _currentPointerEvent = eventData;

        _holdCoroutine = StartCoroutine(BeginPlacementAfterHold());
    }

    private void OnToyDrag(PointerEventData eventData)
    {
        _currentPointerEvent = eventData;

        if (_isPlacementDragging)
        {
            _placementController?.UpdateToyDrag(eventData.position);
            return;
        }
    }

    private void OnToyPointerUp(PointerEventData eventData)
    {
        _isPointerPressed = false;

        if (_isPlacementDragging)
        {
            bool placed = _placementController != null &&
                          _placementController.EndToyDrag(eventData.position);

            _isPlacementDragging = false;

            CancelHold();

            if (placed)
            {
                ResetSelectedButton(true);
                gameObject.SetActive(false);
            }

            return;
        }

        CancelHold();
    }

    private void OnToyPointerExit(PointerEventData eventData)
    {
        if (_isPlacementDragging)
            return;

        CancelHoldCoroutineOnly();
    }

    private IEnumerator BeginPlacementAfterHold()
    {
        yield return new WaitForSecondsRealtime(_placementHoldDuration);

        _holdCoroutine = null;

        if (!_isPointerPressed)
            yield break;

        if (_pressedButton == null || _pressedItem == null)
            yield break;

        if (_selectedButton != _pressedButton || _selectedItemID != _pressedItem.ItemID)
            yield break;

        AutoAssignPlacementController();

        if (_placementController == null || _currentPointerEvent == null)
            yield break;

        bool started = _placementController.BeginToyDrag(_currentPointerEvent.position);

        if (!started)
            yield break;

        _isPlacementDragging = true;

        DebugTool.Log(
            $"[NyangNyangSnapToyUI] 길게 눌러 장난감 배치 시작 / ItemID:{_pressedItem.ItemID}",
            DebugType.UI,
            this
        );
    }

    private void CancelHoldCoroutineOnly()
    {
        if (_holdCoroutine == null)
            return;

        StopCoroutine(_holdCoroutine);
        _holdCoroutine = null;
    }

    private void CancelHold()
    {
        CancelHoldCoroutineOnly();

        _pressedItem = null;
        _pressedButton = null;
        _currentPointerEvent = null;
    }

    private void ClearCreatedButtons()
    {
        CancelHold();
        ResetSelectedButton(false);

        for (int i = 0; i < _spriteControllers.Count; i++)
            _spriteControllers[i]?.Dispose();

        _spriteControllers.Clear();

        for (int i = 0; i < _createdButtons.Count; i++)
        {
            if (_createdButtons[i] == null)
                continue;

            _createdButtons[i].transform.DOKill();

            Destroy(_createdButtons[i].gameObject);
        }

        _createdButtons.Clear();
    }

    private void ClosePopup()
    {
        GameManager.Audio.PlaySfx("Main_SFX_Touch");

        CancelHold();

        _isPointerPressed = false;
        _isPlacementDragging = false;

        ResetSelectedButton(true);

        _placementController?.CancelSelection();

        gameObject.SetActive(false);
    }
}

public enum NyangNyangSnapToyButtons
{
    ToyButton,
    BackPanel,
    CloseButton
}