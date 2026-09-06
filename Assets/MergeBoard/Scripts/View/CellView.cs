using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace MergeBoard
{
    /// <summary>한 칸의 아이템과 이름을 표시하고 uGUI 드래그 이벤트를 BoardView에 전달한다.</summary>
    public sealed class CellView : MonoBehaviour, IPointerDownHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private BoardView boardView;
        [SerializeField] private int index;
        [SerializeField] private ItemGraphic icon;
        [SerializeField] private TextMeshProUGUI label;
        public RectTransform Rect => (RectTransform)transform;
        public ItemStage Stage => icon.Stage;
        public ItemGraphic Icon => icon;
        private bool dragged;
        private bool hasRenderedStage;
        private ItemStage renderedStage;
        private bool itemVisible = true;

        /// <summary>장면 생성 시 소속 보드·칸 인덱스·표시 요소를 연결한다.</summary>
        public void Configure(BoardView board, int cellIndex, ItemGraphic item, TextMeshProUGUI nameLabel)
        {
            boardView = board; index = cellIndex; icon = item; label = nameLabel;
        }

        /// <summary>아이템 단계와 표시 이름을 갱신하고 드래그 중인 원본은 숨긴다.</summary>
        /// <param name="force">true면 이전 아이템 단계와 같아도 아이콘과 이름을 다시 표시한다.</param>
        public void Render(ItemStage stage, bool dragging, bool force = false)
        {
            if (force || !hasRenderedStage || renderedStage != stage)
            {
                icon.Stage = stage;
                label.text = BoardView.StageName(stage);
                renderedStage = stage;
                hasRenderedStage = true;
            }
            SetItemVisible(!dragging);
        }

        /// <summary>아이템과 이름의 표시만 변경한다.</summary>
        public void SetItemVisible(bool visible)
        {
            if (itemVisible == visible) return;
            icon.enabled = visible;
            label.enabled = visible;
            itemVisible = visible;
        }
        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) dragged = false;
        }
        /// <summary>드래그를 수행하지 않은 왼쪽 클릭만 보드에 전달한다.</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!dragged && eventData.button == PointerEventData.InputButton.Left) boardView.Click(index);
        }
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            dragged = true;
            eventData.eligibleForClick = false;
            boardView.BeginDrag(index, eventData);
        }
        public void OnDrag(PointerEventData eventData) => boardView.MoveDrag(eventData);
        public void OnEndDrag(PointerEventData eventData) => boardView.EndDrag(eventData);
    }
}
