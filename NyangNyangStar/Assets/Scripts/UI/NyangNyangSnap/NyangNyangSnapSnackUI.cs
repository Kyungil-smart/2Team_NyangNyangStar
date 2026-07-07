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

public class NyangNyangSnapSnackUI : UIPopup
{
    [Header("버튼")]
    [Tooltip("간식/음식 버튼")]
    [SerializeField] private Button _snackButton;

    [Tooltip("뒤로 가기 패널")]
    [SerializeField] private Button _backPanel;

    [Tooltip("간식/음식 패널 닫기")]
    [SerializeField] private Button _closeButton;


    [Header("데이터")]
    [Tooltip("아이템 이름과 AddressableKey를 가져올 ItemDatabase SO")]
    [SerializeField] private ItemDatabaseSo _itemDatabaseSO;

    [Tooltip("ItemID로 간식/음식/간식/음식을 판별할 Tool SO")]
    [SerializeField] private NyangNyangSnapToolSO _toolSO;

    [Header("동적 생성")]
    [Tooltip("간식/음식 버튼들이 생성될 부모 Content")]
    [SerializeField] private Transform _content;

    [Header("빈 목록 안내")]
    [Tooltip("보유 간식이 없을 때 표시할 TextMeshPro 텍스트")]
    [SerializeField] private TMP_Text _emptyMessageText;

    [Tooltip("빈 목록일 때 표시할 문구")]
    [SerializeField] private string _emptyMessageTextString = "간식이 없습니다.";

    [Header("아이템 배치")]
    [SerializeField] private NyangNyangSnapPlacementController _placementController;

    [Header("선택 연출")]
    [Tooltip("선택된 간식/음식 버튼 크기 배율")]
    [SerializeField] private float _selectedScale = 1.1f;

    [Tooltip("선택 크기 변경 시간")]
    [SerializeField] private float _selectTweenDuration = 0.15f;

    [Tooltip("선택 크기 변경 Ease")]
    [SerializeField] private Ease _selectEase = Ease.OutBack;

    [Header("길게 눌러 배치")]
    [Tooltip("간식/음식 배치 드래그가 시작되는 누르기 시간")]
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

    private NyangNyangSnapSnackUISprite _nyangNyangSnapSnackUISprite;
    public override void Init()
    {
        Bind<Button>(typeof(NyangNyangSnapSnackButtons));

        _snackButton = Get<Button>((int)NyangNyangSnapSnackButtons.SnackButton);
        _backPanel = Get<Button>((int)NyangNyangSnapSnackButtons.BackPanel);
        _closeButton = Get<Button>((int)NyangNyangSnapSnackButtons.CloseButton);

        _nyangNyangSnapSnackUISprite = GetComponent<NyangNyangSnapSnackUISprite>();
        _nyangNyangSnapSnackUISprite.Init();

        AutoAssignPlacementController();
        RegisterPlacementEvents();
        InitPopups();

        _isInitialized = true;
    }

    private void OnEnable()
    {
        if (!_isInitialized)
            return;

        AutoAssignPlacementController();
        RegisterPlacementEvents();
    }

    private void OnDisable()
    {
        StopLoadButtonsCoroutine();
        CancelHold();

        _isPointerPressed = false;
        _isPlacementDragging = false;

        ResetSelectedButton(false);

        if (_placementController != null)
            _placementController.OnSnackDragCompleted -= OnSnackDragCompleted;
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

    private void RegisterPlacementEvents()
    {
        if (_placementController == null)
            return;

        _placementController.OnSnackDragCompleted -= OnSnackDragCompleted;
        _placementController.OnSnackDragCompleted += OnSnackDragCompleted;
    }

    private void OnSnackDragCompleted(int itemID)
    {
        _isPointerPressed = false;
        _isPlacementDragging = false;

        CancelHold();
        ResetSelectedButton(true);

        DebugTool.Log(
            $"[NyangNyangSnapSnackUI] 고양이 도착으로 간식 사용 완료 / ItemID:{itemID}",
            DebugType.UI,
            this
        );

        gameObject.SetActive(false);
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

        LoadSnackButtons();
        return true;
    }

    private void RequestLoadSnackButtons()
    {
        StopLoadButtonsCoroutine();

        if (!isActiveAndEnabled)
            return;

        _loadButtonsCoroutine = StartCoroutine(LoadSnackButtonsWhenReady());
    }

    private IEnumerator LoadSnackButtonsWhenReady()
    {
        if (!ValidateLoadReferences())
        {
            SetEmptyMessageActive(true);
            _loadButtonsCoroutine = null;
            yield break;
        }

        DebugTool.Log(
            "[NyangNyangSnapSnackUI] 머지보드 최신 데이터 로드 시작",
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
                "[NyangNyangSnapSnackUI] 머지보드 최신 데이터 로드에 실패했습니다.",
                DebugType.UI,
                this
            );

            SetEmptyMessageActive(true);
            _loadButtonsCoroutine = null;
            yield break;
        }

        LoadSnackButtons();
        _loadButtonsCoroutine = null;
    }

    private bool HasOwnedSnackItem()
    {
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

        SetEmptyMessageActive(displayedItemCount <= 0);

        DebugTool.Log(
            $"[NyangNyangSnapSnackUI] 보유 간식/음식 표시 완료 / 종류:{displayedItemCount}",
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
        RegisterSnackButtonEvents(createdButton, item);

        _createdButtons.Add(createdButton);
    }

    private Image GetSnackItemImage(Button button)
    {
        if (button == null)
            return null;

        Transform imageTransform = button.transform.Find("SnackImage");

        if (imageTransform == null)
        {
            DebugTool.Warning(
                "[NyangNyangSnapSnackUI] SnackButton 자식에서 SnackImage를 찾지 못했습니다.",
                DebugType.UI,
                this
            );

            return null;
        }

        Image itemImage = imageTransform.GetComponent<Image>();

        if (itemImage == null)
        {
            DebugTool.Warning(
                "[NyangNyangSnapSnackUI] SnackImage에 Image 컴포넌트가 없습니다.",
                DebugType.UI,
                this
            );

            return null;
        }

        return itemImage;
    }

    private void SetButtonSprite(Button createdButton, NyangNyangSnapInventoryItem item)
    {
        Image itemImage = GetSnackItemImage(createdButton);

        if (itemImage == null)
        {
            DebugTool.Warning(
                $"[NyangNyangSnapSnackUI] 생성된 버튼의 SnackImage를 찾지 못했습니다. ItemID:{item.ItemID}",
                DebugType.UI,
                this
            );

            return;
        }

        itemImage.preserveAspect = true;

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

    private void RegisterSnackButtonEvents(Button createdButton, NyangNyangSnapInventoryItem item)
    {
        createdButton.onClick.RemoveAllListeners();
        createdButton.onClick.AddListener(() => SelectSnackItem(item, createdButton));

        EventTrigger eventTrigger = createdButton.GetComponent<EventTrigger>();

        if (eventTrigger == null)
            eventTrigger = createdButton.gameObject.AddComponent<EventTrigger>();

        eventTrigger.triggers.Clear();

        AddEventTrigger(
            eventTrigger,
            EventTriggerType.PointerDown,
            eventData => OnSnackPointerDown(item, createdButton, eventData)
        );

        AddEventTrigger(
            eventTrigger,
            EventTriggerType.Drag,
            OnSnackDrag
        );

        AddEventTrigger(
            eventTrigger,
            EventTriggerType.PointerUp,
            OnSnackPointerUp
        );

        AddEventTrigger(
            eventTrigger,
            EventTriggerType.PointerExit,
            OnSnackPointerExit
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

    private void SelectSnackItem(NyangNyangSnapInventoryItem item, Button selectedButton)
    {
        AutoAssignPlacementController();

        if (_placementController == null || selectedButton == null)
            return;

        if (_isPlacementDragging)
            return;

        if (_selectedButton == selectedButton && _selectedItemID == item.ItemID)
            return;

        Image itemImage = GetSnackItemImage(selectedButton);

        if (itemImage == null || itemImage.sprite == null)
        {
            DebugTool.Warning(
                $"[NyangNyangSnapSnackUI] 선택한 간식/음식 이미지가 없습니다. ItemID:{item.ItemID}",
                DebugType.UI,
                this
            );

            return;
        }

        bool selected = _placementController.SelectSnack(item.ItemID, item.ItemName, itemImage.sprite);

        if (!selected)
            return;

        GameManager.Audio.PlaySfx("Main_SFX_Touch");

        SetSelectedButton(selectedButton, item.ItemID);

        DebugTool.Log(
            $"[NyangNyangSnapSnackUI] 간식/음식 선택 완료 / ItemID:{item.ItemID}, ItemName:{item.ItemName}",
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

    private void OnSnackPointerDown(NyangNyangSnapInventoryItem item, Button button, PointerEventData eventData)
    {
        CancelHold();

        _isPointerPressed = true;
        _isPlacementDragging = false;

        _pressedItem = item;
        _pressedButton = button;
        _currentPointerEvent = eventData;

        _holdCoroutine = StartCoroutine(BeginPlacementAfterHold());
    }

    private void OnSnackDrag(PointerEventData eventData)
    {
        _currentPointerEvent = eventData;

        if (_isPlacementDragging)
        {
            _placementController?.UpdateSnackDrag(eventData.position);
            return;
        }
    }

    private void OnSnackPointerUp(PointerEventData eventData)
    {
        _isPointerPressed = false;

        if (_isPlacementDragging)
        {
            _placementController?.CancelSnackDrag();
            _isPlacementDragging = false;

            CancelHold();

            DebugTool.Log(
                "[NyangNyangSnapSnackUI] 고양이 도착 전에 터치를 해제하여 간식 사용을 취소했습니다.",
                DebugType.UI,
                this
            );

            return;
        }

        CancelHold();
    }

    private void OnSnackPointerExit(PointerEventData eventData)
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

        bool started = _placementController.BeginSnackDrag(_currentPointerEvent.position);

        if (!started)
            yield break;

        _isPlacementDragging = true;

        DebugTool.Log(
            $"[NyangNyangSnapSnackUI] 길게 눌러 간식/음식 배치 시작 / ItemID:{_pressedItem.ItemID}",
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

        _placementController?.CancelSnackDrag();
        _placementController?.CancelSelection();

        gameObject.SetActive(false);
    }
}

public enum NyangNyangSnapSnackButtons
{
    SnackButton,
    BackPanel,
    CloseButton
}