using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.MergeBoard
{
    public class BoardSystem : MonoBehaviour
    {
        [Header("보드 슬롯")]
        [SerializeField] private GameObject _slotRoot;
        [SerializeField] private GameObject _slotPrefab;
        [SerializeField] private GridLayoutGroup _grid;
        [SerializeField] private List<ItemSlot> _itemSlots;

        [Header("보드 크기")]
        [SerializeField] private int _width = 7;
        [SerializeField] private int _height = 9;

        [Header("슬롯 크기")]
        [SerializeField] private int _slotSize = 135;
        [SerializeField] private int _itemSpacing = 5;

        [Header("슬롯 간격")]
        [SerializeField] private int _slotSpacing = 5;

        private void Awake()
        {
            if (_slotRoot == null)
                _slotRoot = GameObject.Find("@Slot Root");

            if (_slotRoot == null)
            {
                DebugTool.Warning("@Slot Root를 찾을 수 없습니다.", DebugType.Board, this);
                return;
            }

            if (_grid == null)
                _grid = _slotRoot.GetComponent<GridLayoutGroup>();

            if (_grid == null)
                _grid = _slotRoot.AddComponent<GridLayoutGroup>();
        }

        private void Start()
        {
            Init();
            GenerateSlot();
        }

        private void Init()
        {
            if (_grid == null)
            {
                DebugTool.Warning("GridLayoutGroup이 없습니다.", DebugType.Board, this);
                return;
            }

            _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _grid.constraintCount = _width;
            _grid.cellSize = new Vector2(_slotSize, _slotSize);
            _grid.spacing = new Vector2(_slotSpacing, _slotSpacing);
        }
        
        public void MoveOrSwapItem(ItemSlot fromSlot, ItemSlot toSlot)
        {
            if (fromSlot == null || toSlot == null)
                return;

            if (fromSlot == toSlot)
                return;

            if (!fromSlot.HasItem)
                return;

            ItemData fromData = fromSlot.ItemData;
            ItemData toData = toSlot.ItemData;

            if (toSlot.HasItem)
            {
                fromSlot.SetItemData(toData);
                toSlot.SetItemData(fromData);
            }
            else
            {
                fromSlot.ClearItem();
                toSlot.SetItemData(fromData);
            }
        }

        private void GenerateSlot()
        {
            int slotAmount = _width * _height;
            int itemSize = _slotSize - _itemSpacing;

            for (int i = 1; i <= slotAmount; i++)
            {
                GameObject slot = Instantiate(_slotPrefab, _slotRoot.transform, false);
                slot.name = $"Slot_{i}";

                ItemSlot itemSlot = slot.GetComponent<ItemSlot>();

                if (itemSlot == null)
                {
                    DebugTool.Warning($"{slot.name}에 ItemSlot이 없습니다.", DebugType.Board, this);
                    continue;
                }
                
                _itemSlots.Add(itemSlot);

                // Color color = new (RandomColor(), RandomColor(), RandomColor(), 1f);
                
                Color color = GradationColor();
                ItemData data = new (i, null, 1, color);
                
                itemSlot.Init(this, i, data, itemSize);

                DebugTool.Log($"{i} 번째 슬롯 생성", DebugType.Board, this);
            }
        }

        private float RandomColor()
            => Random.Range(0.1f, 1f);

        [SerializeField] private float _rainbowSpeed = 0.5f;
        private float _hue;

        private Color GradationColor()
        {
            _hue = Mathf.Repeat(_hue + _rainbowSpeed * Time.deltaTime, 1f);
            return Color.HSVToRGB(_hue, 1f, 1f);
        }
    }
}